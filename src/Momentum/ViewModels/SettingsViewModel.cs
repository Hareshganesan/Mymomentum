using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class SettingsViewModel : PageViewModel
    {
        public SettingsViewModel()
        {
            SyncCommand = new RelayCommand(async () => { await Shell.SyncLeetCodeAsync(false); RaiseAll(); });
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
            OpenFolderCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo(StorageService.UserDirectory) { UseShellExecute = true }));
            ResetCommand = new RelayCommand(Reset);
            ConnectLeetCodeCommand = new RelayCommand(ConnectLeetCode);
            RecoveryCodeCommand = new RelayCommand(CreateRecoveryCode);
            OpenSolvedPageCommand = new RelayCommand(() => Process.Start(new ProcessStartInfo("https://leetcode.com/api/problems/all/") { UseShellExecute = true }));
            ImportSolvedFileCommand = new RelayCommand(ImportSolvedFile);
            PasteSolvedCommand = new RelayCommand(() =>
            {
                var text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
                ApplyImport(LeetCodeService.ImportSolvedText(text), "You can clear your clipboard now.");
            });
            DisconnectLeetCodeCommand = new RelayCommand(() => { LeetCodeSession = string.Empty; RaiseAll(); Shell.ShowToast("LeetCode disconnected. Public sync still works."); });
            DarkCommand = new RelayCommand(() => { Shell.ApplyTheme(true); OnPropertyChanged(nameof(IsDark)); });
            LightCommand = new RelayCommand(() => { Shell.ApplyTheme(false); OnPropertyChanged(nameof(IsDark)); });
        }

        private static Profile P => AppStore.Instance.State.Profile;

        public ICommand SyncCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ConnectLeetCodeCommand { get; }
        public ICommand RecoveryCodeCommand { get; }
        public string RecoveryText => AccountService.HasRecoveryCode(Username) ? "Replace recovery code" : "Create recovery code";
        public ICommand OpenSolvedPageCommand { get; }
        public ICommand ImportSolvedFileCommand { get; }
        public ICommand PasteSolvedCommand { get; }
        public ICommand DisconnectLeetCodeCommand { get; }
        public ICommand DarkCommand { get; }
        public ICommand LightCommand { get; }

        private void Changed() { Store.ScheduleSave(); Shell.RefreshChrome(); }

        public string Name { get => P.Name; set { P.Name = value; Changed(); OnPropertyChanged(nameof(Initial)); } }
        public DateTime? TargetDate { get => P.TargetDate; set { P.TargetDate = value; Changed(); } }
        public int DailyMinutes { get => P.DailyMinutes; set { P.DailyMinutes = value; OnPropertyChanged(); Changed(); } }
        public string Language { get => P.Language; set { P.Language = value; Changed(); } }
        public string TargetCompanies { get => P.TargetCompanies; set { P.TargetCompanies = value; Changed(); } }
        public string LeetCodeUsername { get => P.LeetCodeUsername; set { P.LeetCodeUsername = value.Trim(); Changed(); } }
        public string LeetCodeSession
        {
            get => MailSyncService.UnprotectOrPlain(P.LeetCodeSession);
            set { P.LeetCodeSession = MailSyncService.Protect(value.Trim()); Changed(); OnPropertyChanged(nameof(IsLeetCodeConnected)); OnPropertyChanged(nameof(ConnectStatus)); }
        }
        public bool IsLeetCodeConnected => !string.IsNullOrEmpty(P.LeetCodeSession);
        public string ConnectStatus => IsLeetCodeConnected
            ? "Connected. Your full solve history is imported on each sync."
            : "Optional. Sign in once to import your full solve history, not just the last 20.";
        public event Action? SessionChanged;
        public bool IsDark => P.DarkTheme;
        public string Username => System.IO.Path.GetFileName(StorageService.UserDirectory);
        public string Initial => string.IsNullOrWhiteSpace(P.Name) ? Username.Substring(0, Math.Min(1, Username.Length)).ToUpperInvariant() : P.Name.Trim()[..1].ToUpperInvariant();
        public IReadOnlyList<string> Languages { get; } = new[] { "C++", "Java", "Python", "JavaScript", "C#", "Go" };
        public string DataPath => StorageService.DataFile;
        public string SyncStatus => Store.State.LeetCode is { } lc
            ? $"Last sync {lc.SyncedAt:d MMM, HH:mm} · {lc.Total} solved on LeetCode · {Store.Problems.Count(p => p.IsSolved)} sheet problems marked solved"
            : "Not synced yet.";

        public override void OnShown() => RaiseAll();

        private async void CreateRecoveryCode()
        {
            if (AccountService.HasRecoveryCode(Username) &&
                !await Shell.ConfirmAsync("Replace recovery code?", "Your old recovery code will stop working.", "Replace")) return;
            var code = AccountService.NewRecoveryCode(Username);
            Clipboard.SetText(code);
            OnPropertyChanged(nameof(RecoveryText));
            await Shell.ConfirmAsync("Your recovery code", $"{code}\n\nCopied to your clipboard. Keep it somewhere safe, like a password manager or on paper. " +
                "It lets you reset your password if you forget it. It won't be shown again.", "I've saved it");
        }

        private async void ConnectLeetCode()
        {
            var win = new Views.LeetCodeLoginWindow { Owner = Application.Current.MainWindow };
            if (win.ShowDialog() != true || string.IsNullOrEmpty(win.Session)) return;
            LeetCodeSession = win.Session;
            if (!string.IsNullOrEmpty(win.Username)) LeetCodeUsername = win.Username;
            SessionChanged?.Invoke();
            RaiseAll();
            Shell.ShowToast($"Connected to LeetCode as {win.Username}. Importing your history...");
            await Shell.SyncLeetCodeAsync(false);
            RaiseAll();
        }

        private void ImportSolvedFile()
        {
            var dlg = new OpenFileDialog { Title = "Choose the LeetCode problem list you saved", Filter = "Saved page (*.json;*.htm;*.html;*.txt)|*.json;*.htm;*.html;*.txt|All files|*.*" };
            if (dlg.ShowDialog() != true) return;
            ApplyImport(LeetCodeService.ImportSolvedFile(dlg.FileName), "You can delete the file now.");
        }

        private void ApplyImport(LeetCodeResult result, string after)
        {
            if (!result.Success) { Shell.ShowToast(result.Error); return; }
            int added = Store.ApplyLeetCodeSolved(result);
            Shell.ShowToast(added > 0
                ? $"Imported {result.Solved.Count} solves: {added} sheet problem{(added == 1 ? "" : "s")} marked solved and queued for review. {after}"
                : $"Imported {result.Solved.Count} solves. Your sheet was already up to date. {after}");
            RaiseAll();
        }

        private void Export()
        {
            var dlg = new SaveFileDialog { FileName = $"momentum-backup-{DateTime.Today:yyyy-MM-dd}.json", Filter = "Momentum backup (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;
            StorageService.Export(Store.State, dlg.FileName);
            Shell.ShowToast("Backup saved.");
        }

        private async void Import()
        {
            var dlg = new OpenFileDialog { Filter = "Momentum backup (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;
            if (!await Shell.ConfirmAsync("Are you sure?", "Replace all current progress with this backup?", "Continue", true)) return;
            try
            {
                Store.Replace(StorageService.Import(dlg.FileName));
                Shell.ShowToast("Backup restored.");
                RaiseAll();
            }
            catch (Exception ex)
            {
                Shell.ShowToast($"Couldn't import: {ex.Message}");
            }
        }

        private async void Reset()
        {
            if (!await Shell.ConfirmAsync("Reset all progress?", "Problems, cards, mocks, applications and stories will be cleared. Your profile and LeetCode link are kept. Export a backup first if unsure.", "Reset", true)) return;
            Store.Replace(new AppState { Profile = P });
            Shell.ShowToast("Progress reset.");
            RaiseAll();
        }
    }
}
