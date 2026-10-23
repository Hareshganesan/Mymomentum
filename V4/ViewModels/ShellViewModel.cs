using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Momentum.Core;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public abstract class PageViewModel : ObservableObject
    {
        protected static AppStore Store => AppStore.Instance;
        protected static ShellViewModel Shell => ShellViewModel.Current;

        /// <summary>Called every time the page becomes visible; recompute anything derived from the store here.</summary>
        public virtual void OnShown() { }
    }

    public sealed class NavItem : ObservableObject
    {
        private int _badge;

        public NavItem(string key, string label, string glyph, Func<PageViewModel> factory)
        {
            Key = key;
            Label = label;
            Glyph = glyph;
            Factory = factory;
        }

        public string Key { get; }
        public string Label { get; }
        public string Glyph { get; }
        public Func<PageViewModel> Factory { get; }
        public int Badge { get => _badge; set => Set(ref _badge, value); }
    }

    public sealed class ShellViewModel : ObservableObject
    {
        public static ShellViewModel Current { get; private set; } = null!;

        private readonly Dictionary<string, PageViewModel> _pages = new();
        private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(3.2) };
        private NavItem? _selectedNav;
        private PageViewModel? _currentPage;
        private string _toast = string.Empty;
        private bool _isToastVisible;
        private double _readiness;
        private string _readinessLevel = string.Empty;
        private int _streak;
        private string _syncText = string.Empty;
        private bool _isSyncing;

        public ShellViewModel()
        {
            Current = this;
            Nav = new ObservableCollection<NavItem>
            {
                new("today", "Today", "", () => new TodayViewModel()),
                new("problems", "Problems", "", () => new ProblemsViewModel()),
                new("review", "Review", "", () => new ReviewViewModel()),
                new("corecs", "Core CS", "", () => new CoreCsViewModel()),
                new("mock", "Mock interview", "", () => new MockViewModel()),
                new("pipeline", "Applications", "", () => new PipelineViewModel()),
                new("stories", "HR & stories", "", () => new StoriesViewModel()),
                new("insights", "Insights", "", () => new InsightsViewModel()),
                new("settings", "Settings", "", () => new SettingsViewModel()),
            };

            _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); IsToastVisible = false; };
            NavigateCommand = new RelayCommand<string>(key => Navigate(key));
            ToggleThemeCommand = new RelayCommand(() => ApplyTheme(!Store.State.Profile.DarkTheme));
            SyncCommand = new RelayCommand(async () => await SyncLeetCodeAsync(false), () => !IsSyncing);

            Store.Changed += RefreshChrome;
            RefreshChrome();

            SignOutCommand = new RelayCommand(() => SignOutRequested?.Invoke());
            Navigate("today");
        }

        private static AppStore Store => AppStore.Instance;

        public ObservableCollection<NavItem> Nav { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SyncCommand { get; }
        public ICommand SignOutCommand { get; }

        /// <summary>The window handles the actual sign-out (save, forget the remembered account, show login).</summary>
        public event Action? SignOutRequested;

        public string AccountName => Store.State.Profile.Name;

        public void Detach() => Store.Changed -= RefreshChrome;

        public NavItem? SelectedNav
        {
            get => _selectedNav;
            set
            {
                if (value == null || value == _selectedNav) return;
                Navigate(value.Key);
            }
        }

        public PageViewModel? CurrentPage { get => _currentPage; private set => Set(ref _currentPage, value); }

        public T Navigate<T>(string key) where T : PageViewModel => (T)Navigate(key);

        public PageViewModel Navigate(string key)
        {
            var nav = Nav.First(n => n.Key == key);
            if (!_pages.TryGetValue(key, out var page))
            {
                page = nav.Factory();
                _pages[key] = page;
            }
            _selectedNav = nav;
            OnPropertyChanged(nameof(SelectedNav));
            page.OnShown();
            CurrentPage = page;
            return page;
        }

        // ---------- Chrome: badges, readiness, streak ----------

        public double Readiness { get => _readiness; private set => Set(ref _readiness, value); }
        public string ReadinessLevel { get => _readinessLevel; private set => Set(ref _readinessLevel, value); }
        public int Streak { get => _streak; private set { Set(ref _streak, value); OnPropertyChanged(nameof(StreakText)); } }
        public string StreakText => Streak == 1 ? "1 day streak" : $"{Streak} day streak";
        public string SyncText { get => _syncText; private set => Set(ref _syncText, value); }
        public bool IsSyncing { get => _isSyncing; private set { Set(ref _isSyncing, value); CommandManager.InvalidateRequerySuggested(); } }

        public void RefreshChrome()
        {
            var r = ReadinessService.Compute(Store);
            Readiness = r.Score;
            ReadinessLevel = r.Level;
            Streak = Store.CurrentStreak();
            Nav.First(n => n.Key == "review").Badge = Store.DueProblems.Count() + Store.DueCards.Count();
            var lc = Store.State.LeetCode;
            SyncText = string.IsNullOrWhiteSpace(Store.State.Profile.LeetCodeUsername)
                ? "LeetCode not linked"
                : lc == null ? "Not synced yet" : $"Synced {Ago(lc.SyncedAt)}";
        }

        private static string Ago(DateTime t)
        {
            var span = DateTime.Now - t;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }

        // ---------- LeetCode sync ----------

        public async Task SyncLeetCodeAsync(bool quiet)
        {
            var profile = Store.State.Profile;
            if (string.IsNullOrWhiteSpace(profile.LeetCodeUsername))
            {
                if (!quiet) { ShowToast("Add your LeetCode username in Settings to sync."); Navigate("settings"); }
                return;
            }
            if (IsSyncing) return;
            IsSyncing = true;
            SyncText = "Syncing…";
            try
            {
                var result = await LeetCodeService.SyncAsync(profile.LeetCodeUsername, profile.LeetCodeSession);
                if (!result.Success)
                {
                    if (!quiet) ShowToast(result.Error);
                    return;
                }
                int added = Store.ApplyLeetCodeSolved(result);
                if (!quiet || added > 0)
                {
                    ShowToast(added > 0
                        ? $"Synced: {added} sheet problem{(added == 1 ? "" : "s")} marked solved and queued for review."
                        : $"Synced LeetCode — {result.Snapshot?.Total} solved overall, sheet already up to date.");
                }
                CurrentPage?.OnShown();
            }
            finally
            {
                IsSyncing = false;
                RefreshChrome();
            }
        }

        // ---------- Inbox sync ----------

        private bool _isMailSyncing;
        public bool IsMailSyncing { get => _isMailSyncing; private set { Set(ref _isMailSyncing, value); CommandManager.InvalidateRequerySuggested(); } }
        public string MailStatus { get; private set; } = string.Empty;

        /// <summary>Reads job emails and updates Applications. Quiet mode (startup) only speaks up when something changed.</summary>
        public async Task SyncMailAsync(bool quiet)
        {
            var p = Store.State.Profile;
            if (IsMailSyncing || !MailSyncService.IsConnected(p)) return;
            IsMailSyncing = true;
            try
            {
                MailStatus = "Connecting to your inbox…"; OnPropertyChanged(nameof(MailStatus));
                var progress = new Progress<string>(m => { MailStatus = m; OnPropertyChanged(nameof(MailStatus)); });
                var r = await MailSyncService.SyncAsync(Store.State, progress);
                MailStatus = r.Success
                    ? $"Checked {r.Scanned} emails · {r.JobEmails} job updates · {p.MailLastSync:d MMM, HH:mm}"
                    : r.Error;
                OnPropertyChanged(nameof(MailStatus));
                if (r.Success) Store.NotifyChanged();
                if (!r.Success && !quiet) ShowToast(r.Error);
                else if (r.Created + r.Updated > 0)
                    ShowToast($"Inbox: {string.Join(", ", r.Changes.Take(4))}{(r.Changes.Count > 4 ? $" +{r.Changes.Count - 4} more" : "")}");
                else if (!quiet) ShowToast("Inbox checked — no new job emails.");
                CurrentPage?.OnShown();
            }
            finally { IsMailSyncing = false; }
        }

        // ---------- In-app confirm dialog ----------

        public sealed class ConfirmRequest
        {
            public string Title { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
            public string ConfirmText { get; init; } = "Confirm";
            public bool IsDanger { get; init; }
            public bool IsNormal => !IsDanger;
            internal TaskCompletionSource<bool> Tcs { get; } = new();
        }

        private ConfirmRequest? _confirm;
        public ConfirmRequest? Confirm { get => _confirm; private set { Set(ref _confirm, value); OnPropertyChanged(nameof(HasConfirm)); } }
        public bool HasConfirm => Confirm != null;
        public ICommand ConfirmYesCommand => new RelayCommand(() => Answer(true));
        public ICommand ConfirmNoCommand => new RelayCommand(() => Answer(false));

        /// <summary>Themed replacement for MessageBox: resolves true when the user confirms.</summary>
        public Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", bool danger = false)
        {
            Confirm?.Tcs.TrySetResult(false);
            var req = new ConfirmRequest { Title = title, Message = message, ConfirmText = confirmText, IsDanger = danger };
            Confirm = req;
            return req.Tcs.Task;
        }

        public void Answer(bool yes)
        {
            var req = Confirm;
            Confirm = null;
            req?.Tcs.TrySetResult(yes);
        }

        // ---------- Toast ----------

        public string Toast { get => _toast; private set => Set(ref _toast, value); }
        public bool IsToastVisible { get => _isToastVisible; private set => Set(ref _isToastVisible, value); }

        public void ShowToast(string message)
        {
            Toast = message;
            IsToastVisible = true;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        // ---------- Theme ----------

        public void ApplyTheme(bool dark)
        {
            Store.State.Profile.DarkTheme = dark;
            App.ApplyTheme(dark);
            Store.ScheduleSave();
            // Re-create the current view so code-drawn controls pick up the new brushes.
            // Swapping back synchronously would let WPF keep the old visual tree, so wait for a layout pass first.
            var page = CurrentPage;
            CurrentPage = null;
            Application.Current.Dispatcher.BeginInvoke(() => CurrentPage = page, DispatcherPriority.Background);
        }
    }
}
