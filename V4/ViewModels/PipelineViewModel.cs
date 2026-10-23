using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    /// <summary>Editable wrapper around a <see cref="JobApplication"/>; edits write straight through and autosave.</summary>
    public sealed class ApplicationCard : ObservableObject
    {
        public ApplicationCard(JobApplication model) => Model = model;

        public JobApplication Model { get; }

        private void Changed(string? name = null)
        {
            OnPropertyChanged(name);
            OnPropertyChanged(nameof(EventText));
            OnPropertyChanged(nameof(IsSoon));
            OnPropertyChanged(nameof(HasEvent));
            AppStore.Instance.ScheduleSave();
        }

        public string Company { get => Model.Company; set { Model.Company = value; Changed(); OnPropertyChanged(nameof(PrepText)); } }
        public string Role { get => Model.Role; set { Model.Role = value; Changed(); } }
        public string Ctc { get => Model.Ctc; set { Model.Ctc = value; Changed(); } }
        public string NextEventTitle { get => Model.NextEventTitle; set { Model.NextEventTitle = value; Changed(); } }
        public DateTime? NextEventDate { get => Model.NextEventDate; set { Model.NextEventDate = value; Changed(); } }
        public string Url { get => Model.Url; set { Model.Url = value; Changed(); } }
        public string Notes { get => Model.Notes; set { Model.Notes = value; Changed(); } }
        public Stage Stage { get => Model.Stage; set { Model.MoveTo(value); Changed(); OnPropertyChanged(nameof(IsRejected)); OnPropertyChanged(nameof(ReachedStage)); } }
        public bool IsRejected => Model.Stage == Stage.Rejected;
        public Stage? ReachedStage { get => Model.ReachedStage; set { Model.ReachedStage = value; Changed(); OnPropertyChanged(nameof(Track)); OnPropertyChanged(nameof(AgeText)); } }
        public static IReadOnlyList<Stage> ReachedChoices { get; } = new[] { Stage.Applied, Stage.OA, Stage.Interview, Stage.HR, Stage.Offer };

        public bool HasEvent => Model.NextEventDate.HasValue && Model.NextEventDate.Value.Date >= DateTime.Today;
        public bool IsSoon => HasEvent && (Model.NextEventDate!.Value.Date - DateTime.Today).Days <= 3;
        public string EventText
        {
            get
            {
                if (!Model.NextEventDate.HasValue) return string.Empty;
                var d = (Model.NextEventDate.Value.Date - DateTime.Today).Days;
                var title = string.IsNullOrWhiteSpace(Model.NextEventTitle) ? "Next round" : Model.NextEventTitle;
                return d < 0 ? $"{title} · {Model.NextEventDate:d MMM} (past)" : d == 0 ? $"{title} · today" : d == 1 ? $"{title} · tomorrow" : $"{title} · in {d} days";
            }
        }

        public string PrepText
        {
            get
            {
                var tagged = AppStore.Instance.SheetProblems.Where(p => p.Def.Companies.Contains(Model.Company.Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
                return tagged.Count == 0 ? "No sheet problems tagged with this company yet." : $"{tagged.Count(p => p.IsSolved)} of {tagged.Count} tagged sheet problems solved";
            }
        }

        /// <summary>Applied → OA → Interview → HR → Offer, with the date each step was first reached.</summary>
        public List<TrackNode> Track
        {
            get
            {
                var steps = new[] { (Stage.Applied, "Applied"), (Stage.OA, "Test"), (Stage.Interview, "Interview"), (Stage.HR, "HR"), (Stage.Offer, "Offer") };
                var current = Model.ProgressStage;
                bool rejected = Model.Stage == Stage.Rejected;
                return steps.Select((st, i) =>
                {
                    DateTime? when = st.Item1 == Stage.Applied ? AppliedOn : Model.Emails.Where(e => e.Stage == st.Item1).Select(e => (DateTime?)e.Date).Min();
                    var state = current == st.Item1 ? 2 : current > st.Item1 ? 1 : 0;
                    return new TrackNode(st.Item2, state > 0 && when.HasValue ? when.Value.ToString("d MMM") : string.Empty, state, i < steps.Length - 1, state == 1, rejected && state == 2);
                }).ToList();
            }
        }
        public string QuietText
        {
            get
            {
                var d = (int)(DateTime.Now - LastContact).TotalDays;
                return d == 0 ? "today" : d == 1 ? "1 day" : d < 14 ? $"{d} days" : $"{d / 7} weeks";
            }
        }

        public bool IsCampus => Model.Source == "Campus";
        public string Initial => string.IsNullOrWhiteSpace(Model.Company) ? "?" : Model.Company.Trim()[..1].ToUpperInvariant();
        public DateTime AppliedOn => Model.Emails.Count > 0 ? Model.Emails.Min(e => e.Date) : Model.CreatedAt;
        public DateTime LastContact => Model.Emails.Count > 0 ? Model.Emails.Max(e => e.Date) : Model.CreatedAt;
        private int SilentDays => (int)(DateTime.Now - LastContact).TotalDays;
        public bool IsSilent => Model.Stage is Stage.Applied or Stage.OA or Stage.Interview or Stage.HR && SilentDays >= MailSyncService.StaleDays;
        public string AgeText
        {
            get
            {
                var applied = $"Applied {AppliedOn:d MMM}";
                if (Model.Stage == Stage.Rejected) return $"Reached {StageName(Model.ProgressStage)} · {applied}";
                if (Model.Emails.Count > 1) applied += $" · last reply {Ago(LastContact)}";
                return IsSilent ? $"{applied} · no reply in {SilentDays / 7} wk" : applied;
            }
        }
        private static string StageName(Stage s) => s switch
        {
            Stage.OA => "online test", Stage.Interview => "interview", Stage.HR => "HR / final", Stage.Offer => "offer", _ => "applied",
        };
        private static string Ago(DateTime t) { var d = (int)(DateTime.Now - t).TotalDays; return d == 0 ? "today" : d == 1 ? "yesterday" : d < 14 ? $"{d}d ago" : $"{d / 7}w ago"; }

        public IEnumerable<MailEvent> Emails => Model.Emails.OrderByDescending(e => e.Date).Take(12);
        public bool HasEmails => Model.Emails.Count > 0;
        public string EmailBadge => Model.Emails.Count == 0 ? string.Empty : $"{Model.Emails.Count} email{(Model.Emails.Count == 1 ? "" : "s")}";

        public void Refresh() => RaiseAll();
    }

    /// <summary>State: 0 = not reached, 1 = passed, 2 = current.</summary>
    public sealed record TrackNode(string Label, string Date, int State, bool HasNext, bool LineDone, bool EndedHere = false)
    {
        public bool IsCurrent => State == 2 && !EndedHere;
        public bool IsRejectedHere => EndedHere;
        public bool IsPassed => State == 1;
    }

    /// <summary>All roles at one company: shown as a single row, or a dropdown when there are several.</summary>
    public sealed class CompanyGroup : ObservableObject
    {
        private bool _isExpanded;
        public CompanyGroup(List<ApplicationCard> roles, bool expanded) { Roles = roles; _isExpanded = expanded; }
        public List<ApplicationCard> Roles { get; }
        public ApplicationCard Lead => Roles[0];
        public bool IsMulti => Roles.Count > 1;
        public bool IsSingle => !IsMulti;
        public string Company => Lead.Company;
        public string Initial => Lead.Initial;
        public string Key => Company.Trim().ToLowerInvariant();
        public string Summary => $"{Roles.Count} roles · furthest: {Roles.Max(r => r.Model.ProgressStage) switch { Stage.OA => "online test", Stage.Interview => "interview", Stage.HR => "HR / final", Stage.Offer => "offer", _ => "applied" }}";
        public bool IsExpanded { get => _isExpanded; set { Set(ref _isExpanded, value); OnPropertyChanged(nameof(Chevron)); } }
        public string Chevron => IsExpanded ? "\uE70E" : "\uE70D";
    }

    public sealed class JourneySection
    {
        public string Title { get; init; } = string.Empty;
        public string Hint { get; init; } = string.Empty;
        public string Accent { get; init; } = "TextMuted";
        public List<ApplicationCard> Items { get; init; } = new();
        public List<CompanyGroup> Groups { get; set; } = new();
        public int Count => Groups.Count;
    }

    public sealed record FunnelStep(string Name, int Count, int Total)
    {
        public double Ratio => Total == 0 ? 0 : (double)Count / Total;
    }

    public sealed class StageColumn
    {
        public Stage Stage { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Hint { get; init; } = string.Empty;
        public ObservableCollection<ApplicationCard> Items { get; } = new();
    }

    public sealed class PipelineViewModel : PageViewModel
    {
        private ApplicationCard? _selected;
        private bool _showArchived;

        public PipelineViewModel()
        {
            Columns = new List<StageColumn>
            {
                new() { Stage = Stage.Wishlist, Name = "Wishlist", Hint = "Planning to apply" },
                new() { Stage = Stage.Applied, Name = "Applied", Hint = "Waiting to hear back" },
                new() { Stage = Stage.OA, Name = "Online test", Hint = "OA / coding round" },
                new() { Stage = Stage.Interview, Name = "Interviews", Hint = "Technical rounds" },
                new() { Stage = Stage.HR, Name = "HR / final", Hint = "Managerial & HR" },
                new() { Stage = Stage.Offer, Name = "Offer", Hint = "Congratulations" },
            };

            AddCommand = new RelayCommand(Add);
            SelectCommand = new RelayCommand<ApplicationCard>(c => Selected = c);
            CloseCommand = new RelayCommand(() => { Selected = null; OnShown(); });
            NextCommand = new RelayCommand<ApplicationCard>(c => Move(c, +1));
            PrevCommand = new RelayCommand<ApplicationCard>(c => Move(c, -1));
            ArchiveCommand = new RelayCommand(() => { if (Selected == null) return; Selected.Stage = Stage.Rejected; Selected = null; OnShown(); Shell.ShowToast("Archived. Every rejection is interview practice."); });
            DeleteCommand = new RelayCommand(Delete);
            PracticeCommand = new RelayCommand(() => { if (Selected != null) Shell.Navigate<ProblemsViewModel>("problems").ShowCompany(Selected.Company); });
            MockCommand = new RelayCommand(() =>
            {
                if (Selected == null) return;
                var mock = Shell.Navigate<MockViewModel>("mock");
                mock.Company = Selected.Company;
                mock.Source = "Company";
            });
            OpenUrlCommand = new RelayCommand(() => Links.Open(Selected?.Url));
            ToggleArchivedCommand = new RelayCommand(() => ShowArchived = !ShowArchived);
            RestoreCommand = new RelayCommand<ApplicationCard>(c => { c.Model.RestoredAt = DateTime.Now; c.Model.Stage = c.Model.ReachedStage ?? Stage.Applied; Store.NotifyChanged(); OnShown(); Shell.ShowToast($"{c.Company} restored — the 45-day clock starts again."); });
        }

        // ---------- Inbox sync ----------
        private bool _showMailSetup;
        private string _mailAddress = AppStore.Instance.State.Profile.MailAddress;
        public string MailAddress { get => _mailAddress; set => Set(ref _mailAddress, value); }
        public bool IsMailConnected => MailSyncService.IsConnected(Store.State.Profile);
        public string GoogleClientId { get => Store.State.Profile.GoogleClientId; set { Store.State.Profile.GoogleClientId = value.Trim(); Store.ScheduleSave(); } }
        public bool HasGoogleSecret => !string.IsNullOrEmpty(Store.State.Profile.GoogleClientSecret);
        public bool ShowMailSetup { get => _showMailSetup; set => Set(ref _showMailSetup, value); }
        public string MailStatus => Shell.IsMailSyncing ? Shell.MailStatus : !IsMailConnected ? "Connect your inbox and job emails update this board automatically."
            : string.IsNullOrEmpty(Shell.MailStatus) ? $"Connected: {(string.IsNullOrEmpty(Store.State.Profile.GoogleToken) ? Store.State.Profile.MailAddress : Store.State.Profile.GoogleEmail + " (Google, read-only)")}" + (Store.State.Profile.MailLastSync is { } t ? $" · last checked {t:d MMM, HH:mm}" : string.Empty)
            : Shell.MailStatus;
        public ICommand ToggleMailSetupCommand => new RelayCommand(() => ShowMailSetup = !ShowMailSetup);
        public ICommand SyncMailCommand => new RelayCommand(async () => { await Shell.SyncMailAsync(false); OnShown(); }, () => IsMailConnected && !Shell.IsMailSyncing);
        public ICommand DisconnectMailCommand => new RelayCommand(() =>
        {
            var p = Store.State.Profile;
            if (!string.IsNullOrEmpty(p.GoogleToken)) _ = GoogleMailService.RevokeAsync(MailSyncService.Unprotect(p.GoogleToken));
            p.MailSecret = string.Empty; p.GoogleToken = string.Empty; p.GoogleEmail = string.Empty; p.MailLastSync = null;
            Store.ScheduleSave(); OnShown(); Shell.ShowToast("Inbox disconnected. The app password was removed from this PC.");
        });

        /// <summary>Runs the Google consent flow in the browser; stores the refresh token encrypted, then syncs.</summary>
        public async void SignInWithGoogle(string clientSecret)
        {
            var p = Store.State.Profile;
            if (!string.IsNullOrWhiteSpace(clientSecret)) p.GoogleClientSecret = MailSyncService.Protect(clientSecret.Trim());
            var secret = MailSyncService.Unprotect(p.GoogleClientSecret);
            if (string.IsNullOrWhiteSpace(p.GoogleClientId) || secret.Length == 0) { Shell.ShowToast("Paste your Google Client ID and secret first."); return; }
            Shell.ShowToast("Finish signing in in your browser...");
            var (token, email, error) = await GoogleMailService.SignInAsync(p.GoogleClientId, secret);
            if (token == null) { Shell.ShowToast(error); return; }
            p.GoogleToken = MailSyncService.Protect(token);
            p.GoogleEmail = email ?? string.Empty;
            p.MailLastSync = null;
            Store.ScheduleSave();
            ShowMailSetup = false;
            OnShown();
            await Shell.SyncMailAsync(false);
            OnShown();
        }

        /// <summary>Saves the mailbox (password encrypted with Windows DPAPI) and runs a first sync.</summary>
        public async void ConnectMail(string appPassword)
        {
            if (string.IsNullOrWhiteSpace(MailAddress) || string.IsNullOrWhiteSpace(appPassword)) { Shell.ShowToast("Enter your email and app password."); return; }
            var p = Store.State.Profile;
            p.MailAddress = MailAddress.Trim();
            p.MailHost = MailSyncService.GuessHost(p.MailAddress);
            p.MailSecret = MailSyncService.Protect(appPassword.Replace(" ", ""));
            p.MailLastSync = null;
            Store.ScheduleSave();
            ShowMailSetup = false;
            OnPropertyChanged(nameof(IsMailConnected));
            Shell.ShowToast("Connected — reading the last 120 days of job emails…");
            await Shell.SyncMailAsync(false);
            OnShown();
        }

        public List<StageColumn> Columns { get; }
        public ObservableCollection<ApplicationCard> Archived { get; } = new();
        public IReadOnlyList<Stage> StageChoices { get; } = Enum.GetValues<Stage>();

        private string _search = string.Empty;
        private string _filter = "all";
        public string Search { get => _search; set { if (Set(ref _search, value)) OnShown(); } }
        public string Filter { get => _filter; set { if (Set(ref _filter, value)) OnShown(); } }
        public int TotalApplied { get; private set; }
        public string ResponseRate { get; private set; } = "–";
        public int SilentCount { get; private set; }
        public List<FunnelStep> Funnel { get; private set; } = new();
        public List<JourneySection> Sections { get; private set; } = new();
        private readonly HashSet<string> _expandedCompanies = new();
        public ICommand ToggleGroupCommand => new RelayCommand<CompanyGroup>(g =>
        {
            g.IsExpanded = !g.IsExpanded;
            if (g.IsExpanded) _expandedCompanies.Add(g.Key); else _expandedCompanies.Remove(g.Key);
        });
        public List<JobApplication> MergeTargets { get; private set; } = new();

        public ICommand SetFilterCommand => new RelayCommand<string>(f => Filter = Filter == f ? "all" : f);
        public ICommand OpenGmailCommand => new RelayCommand(() =>
        {
            if (Selected != null) Links.Open("https://mail.google.com/mail/u/0/#search/" + Uri.EscapeDataString($"\"{Selected.Company}\""));
        });

        /// <summary>Folds the selected card into another one (e.g. "Lntedu" into "L&T EduTech"): emails and notes move, the stage keeps the furthest.</summary>
        public ICommand MergeIntoCommand => new RelayCommand<JobApplication>(target =>
        {
            if (Selected == null || target == null || target == Selected.Model) return;
            var src = Selected.Model;
            target.Emails.AddRange(src.Emails);
            if (src.Stage != Stage.Rejected && (src.Stage > target.Stage || target.Stage == Stage.Rejected)) target.Stage = src.Stage;
            if (!string.IsNullOrWhiteSpace(src.Notes)) target.Notes = (target.Notes + "\n" + src.Notes).Trim();
            if (string.IsNullOrWhiteSpace(target.Role)) target.Role = src.Role;
            if (target.CreatedAt > src.CreatedAt) target.CreatedAt = src.CreatedAt;
            Store.State.Applications.Remove(src);
            Store.NotifyChanged();
            Selected = new ApplicationCard(target);
            OnShown();
            Shell.ShowToast($"Merged {src.Company} into {target.Company}.");
        });

        public string SyncLabel => Shell.IsMailSyncing ? "Syncing…" : "Sync inbox";
        private bool _shellHooked;
        private void HookShell()
        {
            if (_shellHooked) return;
            _shellHooked = true;
            Shell.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ShellViewModel.MailStatus) or nameof(ShellViewModel.IsMailSyncing))
                { OnPropertyChanged(nameof(MailStatus)); OnPropertyChanged(nameof(SyncLabel)); }
            };
        }

        public ICommand AddCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PrevCommand { get; }
        public ICommand ArchiveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PracticeCommand { get; }
        public ICommand MockCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand ToggleArchivedCommand { get; }
        public ICommand RestoreCommand { get; }

        public ApplicationCard? Selected { get => _selected; set { Set(ref _selected, value); OnPropertyChanged(nameof(HasSelection)); MergeTargets = Store.State.Applications.Where(a => a != value?.Model).OrderBy(a => a.Company).ToList(); OnPropertyChanged(nameof(MergeTargets)); } }
        public bool HasSelection => Selected != null;
        public bool ShowArchived { get => _showArchived; set => Set(ref _showArchived, value); }

        public int ActiveCount { get; private set; }
        public int UpcomingCount { get; private set; }
        public int OfferCount { get; private set; }
        public bool IsEmpty { get; private set; }

        public override void OnShown()
        {
            HookShell();
            var aged = new MailSyncResult();
            MailSyncService.AgeOut(Store.State, aged);
            if (aged.Updated > 0) Store.ScheduleSave();
            var apps = Store.State.Applications;
            bool Visible(ApplicationCard c) =>
                (string.IsNullOrWhiteSpace(Search) || c.Company.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase)) &&
                (Filter switch { "followup" => c.IsSilent, "recent" => (DateTime.Now - c.LastContact).TotalDays <= 7, _ => true });
            foreach (var col in Columns)
            {
                col.Items.Clear();
                foreach (var a in apps.Where(a => a.Stage == col.Stage))
                {
                    var card = Selected?.Model == a ? Selected : new ApplicationCard(a);
                    if (Visible(card)) col.Items.Add(card);
                }
                // Most recent contact first, so active threads are on top.
                var sorted = col.Items.OrderByDescending(c => c.LastContact).ToList();
                col.Items.Clear();
                foreach (var c in sorted) col.Items.Add(c);
            }

            // Journeys: grouped by what needs doing, not by stage.
            var visible = Columns.SelectMany(c => c.Items).ToList();
            Sections = new List<JourneySection>
            {
                new() { Title = "Needs a nudge", Hint = "No reply in 3+ weeks — follow up or let it go", Accent = "Warning", Items = visible.Where(c => c.IsSilent).OrderBy(c => c.LastContact).ToList() },
                new() { Title = "In motion", Hint = "Recently active", Accent = "Accent", Items = visible.Where(c => !c.IsSilent && c.Stage is Stage.Applied or Stage.OA or Stage.Interview or Stage.HR).OrderByDescending(c => c.Stage).ThenByDescending(c => c.LastContact).ToList() },
                new() { Title = "Offers", Hint = "Congratulations", Accent = "Success", Items = visible.Where(c => c.Stage == Stage.Offer).ToList() },
                new() { Title = "Wishlist", Hint = "Planning to apply", Accent = "TextMuted", Items = visible.Where(c => c.Stage == Stage.Wishlist).ToList() },
                new() { Title = "Closed", Hint = "Rejected or no response — showing how far each one got", Accent = "Danger",
                        Items = apps.Where(a => a.Stage == Stage.Rejected).Select(a => Selected?.Model == a ? Selected : new ApplicationCard(a)).Where(Visible)
                                    .OrderByDescending(c => c.Model.ProgressStage).ThenByDescending(c => c.LastContact).ToList() },
            }.Where(s => s.Items.Count > 0).ToList();
            foreach (var sec in Sections)
                sec.Groups = sec.Items.GroupBy(c => c.Company.Trim().ToLowerInvariant())
                    .Select(g => new CompanyGroup(g.OrderByDescending(c => c.Model.ProgressStage).ThenByDescending(c => c.LastContact).ToList(), _expandedCompanies.Contains(g.Key)))
                    .ToList();
            OnPropertyChanged(nameof(Sections));

            // Insights: how the search is going, not just where cards sit.
            var tracked = apps.Where(a => a.Stage != Stage.Wishlist).ToList();
            TotalApplied = tracked.Count;
            var heardBack = tracked.Count(a => a.Stage is Stage.OA or Stage.Interview or Stage.HR or Stage.Offer || a.Emails.Any(e => e.Stage == Stage.Rejected));
            ResponseRate = TotalApplied == 0 ? "–" : $"{100.0 * heardBack / TotalApplied:0}%";
            SilentCount = apps.Select(a => new ApplicationCard(a)).Count(c => c.IsSilent);
            Funnel = new[] { Stage.Applied, Stage.OA, Stage.Interview, Stage.HR, Stage.Offer }
                .Select(st => new FunnelStep(Columns.First(c => c.Stage == st).Name, tracked.Count(a => a.ProgressStage >= st), TotalApplied))
                .ToList();
            OnPropertyChanged(nameof(TotalApplied)); OnPropertyChanged(nameof(ResponseRate)); OnPropertyChanged(nameof(SilentCount)); OnPropertyChanged(nameof(Funnel));
            MergeTargets = apps.Where(a => a != Selected?.Model).OrderBy(a => a.Company).ToList();
            OnPropertyChanged(nameof(MergeTargets));
            Archived.Clear();
            foreach (var a in apps.Where(a => a.Stage == Stage.Rejected)) Archived.Add(new ApplicationCard(a));

            ActiveCount = apps.Count(a => a.Stage is not (Stage.Rejected or Stage.Offer or Stage.Wishlist));
            UpcomingCount = PlanService.UpcomingEvents(Store.State, 7).Count();
            OfferCount = apps.Count(a => a.Stage == Stage.Offer);
            IsEmpty = apps.Count == 0;
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(UpcomingCount));
            OnPropertyChanged(nameof(OfferCount));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsMailConnected));
            OnPropertyChanged(nameof(MailStatus));
            Selected?.Refresh();
        }

        private void Add()
        {
            var app = new JobApplication { Company = "New company", Stage = Stage.Applied };
            Store.State.Applications.Add(app);
            Store.NotifyChanged();
            var card = new ApplicationCard(app);
            Selected = card;
            OnShown();
        }

        private void Move(ApplicationCard card, int delta)
        {
            var next = (int)card.Stage + delta;
            if (next < (int)Stage.Wishlist || next > (int)Stage.Offer) return;
            card.Stage = (Stage)next;
            if (card.Stage == Stage.Offer) Shell.ShowToast($"Offer from {card.Company}! Well earned.");
            Store.NotifyChanged();
            OnShown();
        }

        private async void Delete()
        {
            if (Selected == null) return;
            if (!await Shell.ConfirmAsync("Are you sure?", $"Delete {Selected.Company}? This can't be undone.", "Continue", true)) return;
            Store.State.Applications.Remove(Selected.Model);
            Selected = null;
            Store.NotifyChanged();
            OnShown();
        }
    }
}
