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
            DarkCommand = new RelayCommand(() => { Shell.ApplyTheme(true); OnPropertyChanged(nameof(IsDark)); });
            LightCommand = new RelayCommand(() => { Shell.ApplyTheme(false); OnPropertyChanged(nameof(IsDark)); });
        }

        private static Profile P => AppStore.Instance.State.Profile;

        public ICommand SyncCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand DarkCommand { get; }
        public ICommand LightCommand { get; }

        private void Changed() { Store.ScheduleSave(); Shell.RefreshChrome(); }

        public string Name { get => P.Name; set { P.Name = value; Changed(); OnPropertyChanged(nameof(Initial)); } }
        public DateTime? TargetDate { get => P.TargetDate; set { P.TargetDate = value; Changed(); } }
        public int DailyMinutes { get => P.DailyMinutes; set { P.DailyMinutes = value; OnPropertyChanged(); Changed(); } }
        public string Language { get => P.Language; set { P.Language = value; Changed(); } }
        public string TargetCompanies { get => P.TargetCompanies; set { P.TargetCompanies = value; Changed(); } }
        public string LeetCodeUsername { get => P.LeetCodeUsername; set { P.LeetCodeUsername = value.Trim(); Changed(); } }
        public string LeetCodeSession { get => P.LeetCodeSession; set { P.LeetCodeSession = value.Trim(); Changed(); } }
        public bool IsDark => P.DarkTheme;
        public string Username => System.IO.Path.GetFileName(StorageService.UserDirectory);
        public string Initial => string.IsNullOrWhiteSpace(P.Name) ? Username.Substring(0, Math.Min(1, Username.Length)).ToUpperInvariant() : P.Name.Trim()[..1].ToUpperInvariant();
        public IReadOnlyList<string> Languages { get; } = new[] { "C++", "Java", "Python", "JavaScript", "C#", "Go" };
        public string DataPath => StorageService.DataFile;
        public string SyncStatus => Store.State.LeetCode is { } lc
            ? $"Last sync {lc.SyncedAt:d MMM, HH:mm} · {lc.Total} solved on LeetCode · {Store.Problems.Count(p => p.IsSolved)} sheet problems marked solved"
            : "Not synced yet.";

        public override void OnShown() => RaiseAll();

        private void Export()
        {
            var dlg = new SaveFileDialog { FileName = $"momentum-backup-{DateTime.Today:yyyy-MM-dd}.json", Filter = "Momentum backup (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;
            StorageService.Export(Store.State, dlg.FileName);
            Shell.ShowToast("Backup saved.");
        }

        private void Import()
        {
            var dlg = new OpenFileDialog { Filter = "Momentum backup (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;
            if (MessageBox.Show("Replace all current progress with this backup?", "Momentum", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            try
            {
                Store.Replace(StorageService.Import(dlg.FileName));
                Shell.ShowToast("Backup restored.");
                RaiseAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Couldn't import: {ex.Message}", "Momentum");
            }
        }

        private void Reset()
        {
            if (MessageBox.Show("Reset ALL progress (problems, cards, mocks, applications, stories)? Your profile and LeetCode link are kept. Export a backup first if unsure.",
                    "Momentum", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            Store.Replace(new AppState { Profile = P });
            Shell.ShowToast("Progress reset.");
            RaiseAll();
        }
    }
}
