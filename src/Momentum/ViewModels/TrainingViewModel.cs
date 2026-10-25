using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class TrainingViewModel : PageViewModel
    {
        private ObservableCollection<ProblemItem> _allProblems = new();
        private ObservableCollection<ProblemItem> _filteredProblems = new();
        private ProblemItem? _selectedProblem;
        private ProblemTraining? _training;
        private string _activeMode = "Optimal"; // "BruteForce", "Optimal", "TeachMe"
        private string _activeLanguage = "Python"; // "Python", "Cpp", "Java"
        private string _search = string.Empty;
        private string _selectedPattern = "All";
        private int _currentStepIndex = 0;
        private bool _isPlaying = false;
        private System.Windows.Threading.DispatcherTimer? _playTimer;

        public TrainingViewModel()
        {
            SelectModeCommand = new RelayCommand<string>(mode => ActiveMode = mode ?? "Optimal");
            SelectLanguageCommand = new RelayCommand<string>(lang => ActiveLanguage = lang ?? "Python");
            CopyCodeCommand = new RelayCommand(CopyActiveCode);
            ToggleBruteForceCommand = new RelayCommand(ToggleBruteForce);
            ToggleOptimalCommand = new RelayCommand(ToggleOptimal);
            NextProblemCommand = new RelayCommand(NextProblem);
            PrevProblemCommand = new RelayCommand(PrevProblem);
            OpenLeetCodeCommand = new RelayCommand(OpenLeetCode);

            StepNextCommand = new RelayCommand(StepNext);
            StepPrevCommand = new RelayCommand(StepPrev);
            StepFirstCommand = new RelayCommand(StepFirst);
            StepLastCommand = new RelayCommand(StepLast);
            ResetStepCommand = new RelayCommand(ResetStep);
            TogglePlayCommand = new RelayCommand(TogglePlay);
            JumpToStepCommand = new RelayCommand<int>(JumpToStep);
            LaunchVisualizerCommand = new RelayCommand(() => ActiveMode = "TeachMe");

            _playTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1600)
            };
            _playTimer.Tick += (s, e) =>
            {
                if (!CanStepNext)
                {
                    StopPlay();
                }
                else
                {
                    CurrentStepIndex++;
                }
            };

            ReloadProblems();
        }

        public override void OnShown()
        {
            ReloadProblems();
        }

        public ICommand SelectModeCommand { get; }
        public ICommand SelectLanguageCommand { get; }
        public ICommand CopyCodeCommand { get; }
        public ICommand ToggleBruteForceCommand { get; }
        public ICommand ToggleOptimalCommand { get; }
        public ICommand NextProblemCommand { get; }
        public ICommand PrevProblemCommand { get; }
        public ICommand OpenLeetCodeCommand { get; }
        public ICommand StepNextCommand { get; }
        public ICommand StepPrevCommand { get; }
        public ICommand StepFirstCommand { get; }
        public ICommand StepLastCommand { get; }
        public ICommand ResetStepCommand { get; }
        public ICommand TogglePlayCommand { get; }
        public ICommand JumpToStepCommand { get; }
        public ICommand LaunchVisualizerCommand { get; }

        public ObservableCollection<ProblemItem> FilteredProblems => _filteredProblems;

        public ProblemItem? SelectedProblem
        {
            get => _selectedProblem;
            set
            {
                if (Set(ref _selectedProblem, value))
                {
                    if (value != null)
                    {
                        LoadTrainingFor(value);
                    }
                    else
                    {
                        Training = null;
                    }
                    NotifyMasteryProperties();
                    OnPropertyChanged(nameof(ActiveOptimalCode));
                    OnPropertyChanged(nameof(ActiveBruteForceCode));
                    RefreshVisualCodeLines();
                }
            }
        }

        public ProblemTraining? Training
        {
            get => _training;
            private set => Set(ref _training, value);
        }

        public string ActiveMode
        {
            get => _activeMode;
            set
            {
                if (Set(ref _activeMode, value))
                {
                    OnPropertyChanged(nameof(IsBruteForceMode));
                    OnPropertyChanged(nameof(IsOptimalMode));
                    OnPropertyChanged(nameof(IsTeachMeMode));
                }
            }
        }

        public bool IsBruteForceMode => ActiveMode == "BruteForce";
        public bool IsOptimalMode => ActiveMode == "Optimal";
        public bool IsTeachMeMode => ActiveMode == "TeachMe";

        public string ActiveLanguage
        {
            get => _activeLanguage;
            set
            {
                if (Set(ref _activeLanguage, value))
                {
                    OnPropertyChanged(nameof(IsCpp));
                    OnPropertyChanged(nameof(IsPython));
                    OnPropertyChanged(nameof(IsJava));
                    OnPropertyChanged(nameof(ActiveOptimalCode));
                    OnPropertyChanged(nameof(ActiveBruteForceCode));
                }
            }
        }

        public bool IsCpp => ActiveLanguage == "Cpp";
        public bool IsPython => ActiveLanguage == "Python";
        public bool IsJava => ActiveLanguage == "Java";

        public string ActiveOptimalCode
        {
            get
            {
                if (_training == null) return string.Empty;
                return ActiveLanguage switch
                {
                    "Python" => !string.IsNullOrWhiteSpace(_training.OptimalCodePython) ? _training.OptimalCodePython : _training.OptimalCodeCpp,
                    "Java" => !string.IsNullOrWhiteSpace(_training.OptimalCodeJava) ? _training.OptimalCodeJava : _training.OptimalCodeCpp,
                    _ => _training.OptimalCodeCpp
                };
            }
        }

        public string ActiveBruteForceCode
        {
            get
            {
                if (_training == null) return string.Empty;
                return ActiveLanguage switch
                {
                    "Python" => !string.IsNullOrWhiteSpace(_training.BruteForceCodePython) ? _training.BruteForceCodePython : _training.BruteForceCode,
                    "Java" => !string.IsNullOrWhiteSpace(_training.BruteForceCodeJava) ? _training.BruteForceCodeJava : _training.BruteForceCode,
                    _ => !string.IsNullOrWhiteSpace(_training.BruteForceCodeCpp) ? _training.BruteForceCodeCpp : _training.BruteForceCode
                };
            }
        }

        // Clean mastery action bindings
        public bool IsBruteForceStudied => SelectedProblem?.BruteForceStudied == true;
        public string BruteForceButtonText => IsBruteForceStudied ? "Brute Force Studied" : "Mark Brute Force Studied";
        public string BruteForceButtonIcon => IsBruteForceStudied ? "\uE73E" : "\uE73C";

        public bool IsOptimalMastered => SelectedProblem?.OptimalMastered == true;
        public string OptimalButtonText => IsOptimalMastered ? "Optimal Mastered" : "Mark Optimal Mastered";
        public string OptimalButtonIcon => IsOptimalMastered ? "\uE73E" : "\uE73C";

        // Interactive Visualizer & Stepper State
        public int CurrentStepIndex
        {
            get => _currentStepIndex;
            set
            {
                if (Set(ref _currentStepIndex, value))
                {
                    NotifyStepChanged();
                }
            }
        }

        public VisualStep? CurrentVisualStep
        {
            get
            {
                if (_training?.VisualSteps == null || _training.VisualSteps.Count == 0) return null;
                if (_currentStepIndex >= 0 && _currentStepIndex < _training.VisualSteps.Count)
                    return _training.VisualSteps[_currentStepIndex];
                return _training.VisualSteps[0];
            }
        }

        public bool HasVisualSteps => _training?.VisualSteps != null && _training.VisualSteps.Count > 0;
        public int VisualStepCount => _training?.VisualSteps?.Count ?? 0;
        public bool HasLineExplanations => _training?.LineExplanations != null && _training.LineExplanations.Count > 0;
        public System.Collections.Generic.List<CodeLineExplanation> LineExplanations => _training?.LineExplanations ?? new();
        public string StepProgressText => HasVisualSteps ? $"Step {CurrentStepIndex + 1} of {VisualStepCount}" : "No Simulation";
        public bool CanStepPrev => HasVisualSteps && CurrentStepIndex > 0;
        public bool CanStepNext => HasVisualSteps && CurrentStepIndex < VisualStepCount - 1;
        public bool CanStepFirst => HasVisualSteps && CurrentStepIndex > 0;
        public bool CanStepLast => HasVisualSteps && CurrentStepIndex < VisualStepCount - 1;

        private ObservableCollection<VisualCodeLine> _visualCodeLines = new();
        public ObservableCollection<VisualCodeLine> VisualCodeLines => _visualCodeLines;

        public string ActiveFunctionSignature
        {
            get
            {
                if (_visualCodeLines.Count > 0)
                {
                    var first = _visualCodeLines.FirstOrDefault(l => l.Code.TrimStart().StartsWith("def "));
                    if (first != null)
                    {
                        var s = first.Code.Trim();
                        if (s.EndsWith(":")) s = s.Substring(0, s.Length - 1);
                        return s;
                    }
                }
                return SelectedProblem != null ? $"{SelectedProblem.Title}()" : "solution()";
            }
        }
        public bool IsPlaying => _isPlaying;
        public string PlayPauseIcon => _isPlaying ? "\uE769" : "\uE768";
        public string PlayPauseText => _isPlaying ? "Pause" : "Play";

        public void StepNext()
        {
            if (CanStepNext)
            {
                CurrentStepIndex++;
            }
        }

        public void StepPrev()
        {
            if (CanStepPrev)
            {
                CurrentStepIndex--;
            }
        }

        public void StepFirst()
        {
            StopPlay();
            if (HasVisualSteps)
            {
                CurrentStepIndex = 0;
            }
        }

        public void StepLast()
        {
            StopPlay();
            if (HasVisualSteps)
            {
                CurrentStepIndex = VisualStepCount - 1;
            }
        }

        public void ResetStep()
        {
            StepFirst();
        }

        public void TogglePlay()
        {
            if (_isPlaying)
            {
                StopPlay();
            }
            else
            {
                StartPlay();
            }
        }

        private void StartPlay()
        {
            if (!HasVisualSteps) return;
            if (CurrentStepIndex >= VisualStepCount - 1)
            {
                CurrentStepIndex = 0;
            }
            _isPlaying = true;
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(PlayPauseIcon));
            OnPropertyChanged(nameof(PlayPauseText));
            _playTimer?.Start();
        }

        private void StopPlay()
        {
            _isPlaying = false;
            _playTimer?.Stop();
            OnPropertyChanged(nameof(IsPlaying));
            OnPropertyChanged(nameof(PlayPauseIcon));
            OnPropertyChanged(nameof(PlayPauseText));
        }

        public void JumpToStep(int idx)
        {
            if (idx >= 0 && idx < VisualStepCount)
            {
                CurrentStepIndex = idx;
            }
        }

        private void NotifyStepChanged()
        {
            OnPropertyChanged(nameof(CurrentVisualStep));
            OnPropertyChanged(nameof(StepProgressText));
            OnPropertyChanged(nameof(CanStepPrev));
            OnPropertyChanged(nameof(CanStepNext));
            OnPropertyChanged(nameof(CanStepFirst));
            OnPropertyChanged(nameof(CanStepLast));
            UpdateActiveCodeLines();
        }

        private void NotifyMasteryProperties()
        {
            OnPropertyChanged(nameof(IsBruteForceStudied));
            OnPropertyChanged(nameof(BruteForceButtonText));
            OnPropertyChanged(nameof(BruteForceButtonIcon));
            OnPropertyChanged(nameof(IsOptimalMastered));
            OnPropertyChanged(nameof(OptimalButtonText));
            OnPropertyChanged(nameof(OptimalButtonIcon));
            OnPropertyChanged(nameof(TrainedCount));
            OnPropertyChanged(nameof(ProgressSummary));
        }

        public string Search
        {
            get => _search;
            set
            {
                if (Set(ref _search, value))
                {
                    ApplyFilter();
                }
            }
        }

        public string SelectedPattern
        {
            get => _selectedPattern;
            set
            {
                if (Set(ref _selectedPattern, value))
                {
                    ApplyFilter();
                }
            }
        }

        public int TrainedCount => _allProblems.Count(p => p.OptimalMastered);
        public int TotalCount => _allProblems.Count;
        public string ProgressSummary => $"{TrainedCount} of {TotalCount} Mastered";

        public void SelectProblemBySlug(string slug)
        {
            var target = _allProblems.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));
            if (target != null)
            {
                if (!_filteredProblems.Contains(target))
                {
                    _search = string.Empty;
                    _selectedPattern = "All";
                    OnPropertyChanged(nameof(Search));
                    OnPropertyChanged(nameof(SelectedPattern));
                    ApplyFilter();
                }
                SelectedProblem = target;
            }
        }

        private void ReloadProblems()
        {
            var currentSlug = _selectedProblem?.Slug;
            _allProblems.Clear();

            var sheet = ProblemCatalog.Problems;
            var custom = Store.State.CustomProblems.Select(c => new ProblemDef
            {
                Slug = c.Slug,
                Title = c.Title,
                Difficulty = c.Difficulty,
                Pattern = c.Pattern,
                CustomUrl = c.Url,
                IsCustom = true,
                Companies = c.Companies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            });

            foreach (var def in sheet.Concat(custom))
            {
                if (!Store.State.Problems.TryGetValue(def.Slug, out var prog))
                {
                    prog = new ProblemProgress();
                    Store.State.Problems[def.Slug] = prog;
                }
                _allProblems.Add(new ProblemItem(def, prog));
            }

            ApplyFilter();

            if (!string.IsNullOrEmpty(currentSlug))
            {
                SelectProblemBySlug(currentSlug);
            }
            else if (SelectedProblem == null && _filteredProblems.Count > 0)
            {
                SelectedProblem = _filteredProblems[0];
            }

            NotifyMasteryProperties();
            OnPropertyChanged(nameof(TotalCount));
        }

        private void ApplyFilter()
        {
            var query = _allProblems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_search))
            {
                var term = _search.Trim();
                query = query.Where(p =>
                    p.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.PatternName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    p.CompaniesText.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (_selectedPattern != "All")
            {
                query = query.Where(p => string.Equals(p.PatternKey, _selectedPattern, StringComparison.OrdinalIgnoreCase));
            }

            _filteredProblems.Clear();
            foreach (var item in query)
            {
                _filteredProblems.Add(item);
            }

            if (SelectedProblem != null && !_filteredProblems.Contains(SelectedProblem) && _filteredProblems.Count > 0)
            {
                SelectedProblem = _filteredProblems[0];
            }
        }

        private void LoadTrainingFor(ProblemItem problem)
        {
            StopPlay();
            Training = DsaTrainingCatalog.GetTraining(problem.Def);
            _currentStepIndex = 0;
            OnPropertyChanged(nameof(CurrentStepIndex));
            OnPropertyChanged(nameof(HasVisualSteps));
            OnPropertyChanged(nameof(VisualStepCount));
            OnPropertyChanged(nameof(HasLineExplanations));
            OnPropertyChanged(nameof(LineExplanations));
            NotifyStepChanged();
            OnPropertyChanged(nameof(ActiveOptimalCode));
            OnPropertyChanged(nameof(ActiveBruteForceCode));
            RefreshVisualCodeLines();
        }

        private void CopyActiveCode()
        {
            string codeToCopy = IsBruteForceMode
                ? ActiveBruteForceCode
                : ActiveOptimalCode;

            if (!string.IsNullOrWhiteSpace(codeToCopy))
            {
                try
                {
                    Clipboard.SetText(codeToCopy);
                    Shell.ShowToast("Code copied to clipboard!");
                }
                catch
                {
                    // Ignore clipboard lock issues
                }
            }
        }

        private void ToggleBruteForce()
        {
            if (SelectedProblem == null) return;
            SelectedProblem.BruteForceStudied = !SelectedProblem.BruteForceStudied;
            SelectedProblem.Progress.LastTrained = DateTime.UtcNow;
            Store.ScheduleSave();
            NotifyMasteryProperties();
            Shell.ShowToast(SelectedProblem.BruteForceStudied ? "Brute force marked studied!" : "Brute force reset");
        }

        private void ToggleOptimal()
        {
            if (SelectedProblem == null) return;
            SelectedProblem.OptimalMastered = !SelectedProblem.OptimalMastered;
            SelectedProblem.Progress.LastTrained = DateTime.UtcNow;
            Store.ScheduleSave();
            NotifyMasteryProperties();
            Shell.ShowToast(SelectedProblem.OptimalMastered ? "Optimal approach marked MASTERED!" : "Optimal approach reset");
        }

        private void NextProblem()
        {
            if (SelectedProblem == null || _filteredProblems.Count == 0) return;
            int idx = _filteredProblems.IndexOf(SelectedProblem);
            if (idx < _filteredProblems.Count - 1)
            {
                SelectedProblem = _filteredProblems[idx + 1];
            }
        }

        private void PrevProblem()
        {
            if (SelectedProblem == null || _filteredProblems.Count == 0) return;
            int idx = _filteredProblems.IndexOf(SelectedProblem);
            if (idx > 0)
            {
                SelectedProblem = _filteredProblems[idx - 1];
            }
        }

        private void OpenLeetCode()
        {
            if (SelectedProblem == null) return;
            Links.Open(SelectedProblem.Url);
        }

        private void RefreshVisualCodeLines()
        {
            _visualCodeLines.Clear();
            var code = !string.IsNullOrWhiteSpace(_training?.OptimalCodePython)
                ? _training.OptimalCodePython
                : ActiveOptimalCode;

            if (string.IsNullOrWhiteSpace(code))
            {
                OnPropertyChanged(nameof(ActiveFunctionSignature));
                return;
            }

            var allLines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Skip leading file comments so Line 1 begins cleanly at def
            int startIdx = 0;
            while (startIdx < allLines.Length &&
                   (allLines[startIdx].TrimStart().StartsWith("#") || string.IsNullOrWhiteSpace(allLines[startIdx])) &&
                   !allLines[startIdx].TrimStart().StartsWith("def "))
            {
                startIdx++;
            }
            if (startIdx >= allLines.Length) startIdx = 0;

            int activeNum = CurrentVisualStep?.LineNumber ?? 0;
            int displayLine = 1;

            for (int i = startIdx; i < allLines.Length; i++)
            {
                _visualCodeLines.Add(new VisualCodeLine
                {
                    LineNumber = displayLine,
                    Code = allLines[i],
                    IsActive = (displayLine == activeNum)
                });
                displayLine++;
            }
            OnPropertyChanged(nameof(ActiveFunctionSignature));
        }

        private void UpdateActiveCodeLines()
        {
            int activeNum = CurrentVisualStep?.LineNumber ?? 0;
            foreach (var line in _visualCodeLines)
            {
                line.IsActive = (line.LineNumber == activeNum);
            }
        }
    }

    public sealed class VisualCodeLine : ObservableObject
    {
        public int LineNumber { get; init; }
        public string Code { get; init; } = string.Empty;

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (Set(ref _isActive, value))
                {
                    OnPropertyChanged(nameof(PointerText));
                }
            }
        }

        public string PointerText => _isActive ? "▶" : " ";
    }

}