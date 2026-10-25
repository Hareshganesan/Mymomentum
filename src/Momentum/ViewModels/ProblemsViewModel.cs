using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class PatternRow : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int Index { get; init; }
        public int Solved { get; set; }
        public int Total { get; set; }
        public int Due { get; set; }
        public double Progress => Total == 0 ? 0 : (double)Solved / Total;
        public string CountText => $"{Solved}/{Total}";
        public bool IsAll => Key.Length == 0;
        public void Refresh() => RaiseAll();
    }

    public sealed class ProblemsViewModel : PageViewModel
    {
        private PatternRow? _selectedPattern;
        private ProblemItem? _selectedProblem;
        private string _search = string.Empty;
        private string _difficulty = "All";
        private string _statusFilter = "All";
        private string _company = AnyCompany;
        private bool _showHint;
        private bool _isAdding;
        private string _newUrl = string.Empty;
        private string _newTitle = string.Empty;
        private Difficulty _newDifficulty = Difficulty.Medium;
        private string _newPattern = "arrays";
        private string _newCompanies = string.Empty;
        private const string AnyCompany = "Any company";

        public ProblemsViewModel()
        {
            Patterns.Add(new PatternRow { Key = string.Empty, Name = "All problems", Index = -1 });
            int i = 0;
            foreach (var p in ProblemCatalog.Patterns) Patterns.Add(new PatternRow { Key = p.Key, Name = p.Name, Index = ++i });

            Companies = new[] { AnyCompany }
                .Concat(ProblemCatalog.Problems.SelectMany(p => p.Companies)
                    .GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key))
                .ToList();

            _selectedPattern = Patterns[0];

            OpenCommand = new RelayCommand(() => { if (SelectedProblem == null) return; Links.Open(SelectedProblem.Url); if (SelectedProblem.IsTodo) SetStatus(ProblemStatus.Attempted); });
            AttemptedCommand = new RelayCommand(() => SetStatus(ProblemStatus.Attempted));
            ResetCommand = new RelayCommand(() => SetStatus(ProblemStatus.Todo));
            GradeAgainCommand = new RelayCommand(() => MarkSolved(Grade.Again));
            GradeHardCommand = new RelayCommand(() => MarkSolved(Grade.Hard));
            GradeGoodCommand = new RelayCommand(() => MarkSolved(Grade.Good));
            GradeEasyCommand = new RelayCommand(() => MarkSolved(Grade.Easy));
            ToggleStarCommand = new RelayCommand(() => { if (SelectedProblem != null) SelectedProblem.Starred = !SelectedProblem.Starred; });
            ShowHintCommand = new RelayCommand(() => ShowHint = true);
            CloseDetailCommand = new RelayCommand(() => SelectedProblem = null);
            DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedProblem?.IsCustom == true);
            StartAddCommand = new RelayCommand(() => { IsAdding = true; SelectedProblem = null; });
            CancelAddCommand = new RelayCommand(() => IsAdding = false);
            AddCommand = new RelayCommand(AddProblem, () => !string.IsNullOrWhiteSpace(NewUrl) || !string.IsNullOrWhiteSpace(NewTitle));
            ReviewNowCommand = new RelayCommand(() => { if (SelectedProblem != null) Shell.Navigate<ReviewViewModel>("review").StartProblems(SelectedProblem); });
            OpenTrainingCommand = new RelayCommand(() => { if (SelectedProblem != null) Shell.OpenTrainingFor(SelectedProblem.Slug); });
        }

        public ObservableCollection<PatternRow> Patterns { get; } = new();
        public ObservableCollection<ProblemItem> Filtered { get; } = new();
        public IReadOnlyList<string> Companies { get; }
        public IReadOnlyList<PatternDef> PatternDefs => ProblemCatalog.Patterns;
        public IReadOnlyList<Difficulty> Difficulties { get; } = new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

        public ICommand OpenCommand { get; }
        public ICommand AttemptedCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand GradeAgainCommand { get; }
        public ICommand GradeHardCommand { get; }
        public ICommand GradeGoodCommand { get; }
        public ICommand GradeEasyCommand { get; }
        public ICommand ToggleStarCommand { get; }
        public ICommand ShowHintCommand { get; }
        public ICommand CloseDetailCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartAddCommand { get; }
        public ICommand CancelAddCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand ReviewNowCommand { get; }
        public ICommand OpenTrainingCommand { get; }

        public PatternRow? SelectedPattern
        {
            get => _selectedPattern;
            set { if (value == null) return; if (Set(ref _selectedPattern, value)) { OnPropertyChanged(nameof(PatternHeader)); ApplyFilter(); } }
        }

        public PatternDef? PatternHeader => ProblemCatalog.GetPattern(SelectedPattern?.Key ?? string.Empty);

        public ProblemItem? SelectedProblem
        {
            get => _selectedProblem;
            set
            {
                if (Set(ref _selectedProblem, value))
                {
                    ShowHint = false;
                    if (value != null) IsAdding = false;
                    OnPropertyChanged(nameof(HasSelection));
                    OnPropertyChanged(nameof(ShowSidePanel));
                }
            }
        }

        public bool HasSelection => SelectedProblem != null;
        public bool ShowSidePanel => HasSelection || IsAdding;

        public string Search { get => _search; set { if (Set(ref _search, value)) ApplyFilter(); } }
        public string DifficultyFilter { get => _difficulty; set { if (Set(ref _difficulty, value)) ApplyFilter(); } }
        public string StatusFilter { get => _statusFilter; set { if (Set(ref _statusFilter, value)) ApplyFilter(); } }
        public string Company { get => _company; set { if (Set(ref _company, value ?? AnyCompany)) ApplyFilter(); } }
        public bool ShowHint { get => _showHint; set => Set(ref _showHint, value); }

        public string ResultText { get; private set; } = string.Empty;

        // ----- Add form -----
        public bool IsAdding { get => _isAdding; set { if (Set(ref _isAdding, value)) OnPropertyChanged(nameof(ShowSidePanel)); } }
        public string NewUrl
        {
            get => _newUrl;
            set
            {
                if (!Set(ref _newUrl, value)) return;
                var slug = MigrationService.SlugFromUrl(value);
                if (slug.Length > 0 && string.IsNullOrWhiteSpace(NewTitle)) NewTitle = TitleFromSlug(slug);
            }
        }
        public string NewTitle { get => _newTitle; set => Set(ref _newTitle, value); }
        public Difficulty NewDifficulty { get => _newDifficulty; set => Set(ref _newDifficulty, value); }
        public string NewPattern { get => _newPattern; set => Set(ref _newPattern, value); }
        public string NewCompanies { get => _newCompanies; set => Set(ref _newCompanies, value); }

        public override void OnShown()
        {
            RefreshPatternCounts();
            foreach (var p in Store.Problems) p.Refresh();
            ApplyFilter();
        }

        public void ShowPattern(string key)
        {
            _search = string.Empty;
            _statusFilter = "All";
            _difficulty = "All";
            _company = AnyCompany;
            RaiseAll();
            SelectedPattern = Patterns.FirstOrDefault(p => p.Key == key) ?? Patterns[0];
            ApplyFilter();
        }

        public void ShowCompany(string company)
        {
            _search = string.Empty;
            _statusFilter = "All";
            _difficulty = "All";
            _selectedPattern = Patterns[0];
            var match = Companies.FirstOrDefault(c => c.Equals(company.Trim(), StringComparison.OrdinalIgnoreCase));
            _company = match ?? AnyCompany;
            if (match == null) _search = company.Trim();
            RaiseAll();
            ApplyFilter();
        }

        private void RefreshPatternCounts()
        {
            foreach (var row in Patterns)
            {
                var items = row.IsAll ? Store.Problems : Store.Problems.Where(p => p.PatternKey == row.Key).ToList();
                row.Solved = items.Count(p => p.IsSolved);
                row.Total = items.Count;
                row.Due = items.Count(p => p.IsDue);
                row.Refresh();
            }
        }

        private void ApplyFilter()
        {
            var key = SelectedPattern?.Key ?? string.Empty;
            var q = Store.Problems.AsEnumerable();
            if (key.Length > 0) q = q.Where(p => p.PatternKey == key);
            if (DifficultyFilter != "All") q = q.Where(p => p.DifficultyText == DifficultyFilter);
            q = StatusFilter switch
            {
                "Todo" => q.Where(p => !p.IsSolved),
                "Solved" => q.Where(p => p.IsSolved),
                "Due" => q.Where(p => p.IsDue),
                "Starred" => q.Where(p => p.Starred),
                _ => q,
            };
            if (Company != AnyCompany) q = q.Where(p => p.Def.Companies.Contains(Company, StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim();
                q = q.Where(p => p.Title.Contains(s, StringComparison.OrdinalIgnoreCase)
                              || p.PatternName.Contains(s, StringComparison.OrdinalIgnoreCase)
                              || p.CompaniesText.Contains(s, StringComparison.OrdinalIgnoreCase)
                              || p.Notes.Contains(s, StringComparison.OrdinalIgnoreCase));
            }

            var list = q.OrderBy(p => ProblemCatalog.PatternIndex(p.PatternKey)).ThenBy(p => p.Def.Order).ToList();
            var selected = SelectedProblem;
            Filtered.Clear();
            foreach (var p in list) Filtered.Add(p);
            if (selected != null && !list.Contains(selected)) SelectedProblem = null;

            var solved = list.Count(p => p.IsSolved);
            ResultText = $"{list.Count} problem{(list.Count == 1 ? "" : "s")} · {solved} solved";
            OnPropertyChanged(nameof(ResultText));
        }

        private void SetStatus(ProblemStatus status)
        {
            if (SelectedProblem == null) return;
            Store.SetStatus(SelectedProblem, status);
            RefreshPatternCounts();
            if (status == ProblemStatus.Todo) Shell.ShowToast("Progress reset for this problem.");
        }

        private void MarkSolved(Grade grade)
        {
            var p = SelectedProblem;
            if (p == null) return;
            var first = !p.IsSolved;
            if (first) Store.MarkSolved(p, grade);
            else Store.ReviewProblem(p, grade);
            RefreshPatternCounts();
            Shell.ShowToast($"{(first ? "Solved" : "Reviewed")} · next review in {Srs.FormatInterval(p.Progress.Srs.Interval)}");
            OnPropertyChanged(nameof(SelectedProblem));
        }

        private void DeleteSelected()
        {
            if (SelectedProblem is not { IsCustom: true } p) return;
            Store.DeleteCustomProblem(p);
            SelectedProblem = null;
            RefreshPatternCounts();
            ApplyFilter();
        }

        private void AddProblem()
        {
            var slug = MigrationService.SlugFromUrl(NewUrl);
            if (slug.Length == 0) slug = "custom-" + new string(NewTitle.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray()).Trim().Replace(' ', '-');
            if (Store.ProblemsBySlug.TryGetValue(slug, out var existing))
            {
                Shell.ShowToast($"'{existing.Title}' is already on your sheet.");
                SelectedProblem = existing;
                return;
            }

            var item = Store.AddCustomProblem(new CustomProblem
            {
                Slug = slug,
                Title = string.IsNullOrWhiteSpace(NewTitle) ? TitleFromSlug(slug) : NewTitle.Trim(),
                Difficulty = NewDifficulty,
                Pattern = NewPattern,
                Url = NewUrl.Trim(),
                Companies = NewCompanies.Trim(),
            });
            NewUrl = NewTitle = NewCompanies = string.Empty;
            IsAdding = false;
            RefreshPatternCounts();
            ApplyFilter();
            SelectedProblem = item;
            Shell.ShowToast("Added to your sheet.");
        }

        private static string TitleFromSlug(string slug) =>
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));
    }
}
