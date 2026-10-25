using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class MockPhase : ObservableObject
    {
        private bool _isCurrent;
        private bool _isDone;

        public string Name { get; init; } = string.Empty;
        public double Share { get; init; }
        public string[] Prompts { get; init; } = Array.Empty<string>();
        public int Minutes { get; set; }
        public bool IsCurrent { get => _isCurrent; set => Set(ref _isCurrent, value); }
        public bool IsDone { get => _isDone; set => Set(ref _isDone, value); }
        public string Label => $"{Name} · {Minutes}m";
    }

    public sealed class MockHistoryRow
    {
        public MockRecord Record { get; init; } = new();
        public string Date => Record.Date.ToString("d MMM");
        public string Title => Record.ProblemTitle;
        public string Time => $"{Record.Minutes}/{Record.TargetMinutes} min";
        public bool Solved => Record.Solved;
        public string Result => Record.Solved ? "Solved" : "Not solved";
        public string Score
        {
            get
            {
                var s = new[] { Record.Communication, Record.ProblemSolving, Record.CodeQuality, Record.Testing }.Where(x => x > 0).ToList();
                return s.Count == 0 ? "–" : $"{s.Average():0.0}/4";
            }
        }
    }

    public sealed class MockViewModel : PageViewModel
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private string _source = "Weak";
        private string _company = string.Empty;
        private int _duration = 45;
        private ProblemItem? _problem;
        private string _stage = "Setup";
        private int _elapsed;
        private bool _isPaused;
        private int _phaseIndex;
        private int _hintsUsed;
        private bool _hintVisible;
        private string _scratch = string.Empty;
        private string _outcome = "Solved";
        private int _communication = 3, _problemSolving = 3, _codeQuality = 3, _testing = 3;
        private string _learnings = string.Empty;

        public MockViewModel()
        {
            _timer.Tick += (_, _) => { if (!_isPaused) { _elapsed++; Tick(); } };
            ShuffleCommand = new RelayCommand(Pick);
            StartCommand = new RelayCommand(Start, () => Problem != null);
            OpenCommand = new RelayCommand(() => Links.Open(Problem?.Url));
            PauseCommand = new RelayCommand(() => { IsPaused = !IsPaused; });
            NextPhaseCommand = new RelayCommand(() => SetPhase(_phaseIndex + 1), () => _phaseIndex < Phases.Count - 1);
            HintCommand = new RelayCommand(() => { if (!HintVisible) HintsUsed++; HintVisible = true; });
            FinishCommand = new RelayCommand(() => { _timer.Stop(); Stage = "Debrief"; });
            AbandonCommand = new RelayCommand(() => { _timer.Stop(); Stage = "Setup"; Shell.ShowToast("Mock abandoned — nothing saved."); });
            SaveCommand = new RelayCommand(Save);
        }

        public ObservableCollection<MockPhase> Phases { get; } = new();
        public ObservableCollection<MockHistoryRow> History { get; } = new();
        public List<string> CompanyChoices { get; private set; } = new();

        public ICommand ShuffleCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand NextPhaseCommand { get; }
        public ICommand HintCommand { get; }
        public ICommand FinishCommand { get; }
        public ICommand AbandonCommand { get; }
        public ICommand SaveCommand { get; }

        public string Source { get => _source; set { if (Set(ref _source, value)) { OnPropertyChanged(nameof(IsCompanySource)); Pick(); } } }
        public bool IsCompanySource => Source == "Company";
        public string Company { get => _company; set { if (Set(ref _company, value ?? string.Empty) && IsCompanySource) Pick(); } }
        public int Duration { get => _duration; set => Set(ref _duration, value); }
        public ProblemItem? Problem { get => _problem; private set { Set(ref _problem, value); OnPropertyChanged(nameof(HasProblem)); } }
        public bool HasProblem => Problem != null;
        public string PickReason { get; private set; } = string.Empty;

        public string Stage
        {
            get => _stage;
            private set
            {
                Set(ref _stage, value);
                OnPropertyChanged(nameof(IsSetup));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsDebrief));
            }
        }
        public bool IsSetup => Stage == "Setup";
        public bool IsRunning => Stage == "Running";
        public bool IsDebrief => Stage == "Debrief";

        public bool IsPaused { get => _isPaused; set { Set(ref _isPaused, value); OnPropertyChanged(nameof(PauseLabel)); } }
        public string PauseLabel => IsPaused ? "Resume" : "Pause";
        public string TimerText
        {
            get
            {
                var remaining = Duration * 60 - _elapsed;
                var abs = Math.Abs(remaining);
                return (remaining < 0 ? "+" : "") + $"{abs / 60:00}:{abs % 60:00}";
            }
        }
        public bool IsOvertime => _elapsed > Duration * 60;
        public double TimeProgress => Math.Min(1, _elapsed / (Duration * 60.0));
        public MockPhase? CurrentPhase => _phaseIndex < Phases.Count ? Phases[_phaseIndex] : null;
        public int HintsUsed { get => _hintsUsed; private set => Set(ref _hintsUsed, value); }
        public bool HintVisible { get => _hintVisible; private set => Set(ref _hintVisible, value); }
        public string Scratch { get => _scratch; set => Set(ref _scratch, value); }
        public string PhaseNudge { get; private set; } = string.Empty;

        // Debrief
        public string Outcome { get => _outcome; set => Set(ref _outcome, value); }
        public int Communication { get => _communication; set => Set(ref _communication, value); }
        public int ProblemSolving { get => _problemSolving; set => Set(ref _problemSolving, value); }
        public int CodeQuality { get => _codeQuality; set => Set(ref _codeQuality, value); }
        public int Testing { get => _testing; set => Set(ref _testing, value); }
        public string Learnings { get => _learnings; set => Set(ref _learnings, value); }
        public string ElapsedText => $"{_elapsed / 60} min {_elapsed % 60:00} s of {Duration} min";

        // Stats
        public int MockCount { get; private set; }
        public string SolveRate { get; private set; } = "–";
        public string AvgScore { get; private set; } = "–";
        public string AvgTime { get; private set; } = "–";

        public override void OnShown()
        {
            var companies = Store.State.Profile.TargetCompanies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Concat(Store.State.Applications.Where(a => a.Stage != Models.Stage.Rejected).Select(a => a.Company))
                .Concat(ProblemCatalog.Problems.SelectMany(p => p.Companies).GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => g.Key).Take(12))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            CompanyChoices = companies;
            if (string.IsNullOrEmpty(_company) && companies.Count > 0) _company = companies[0];
            OnPropertyChanged(nameof(CompanyChoices));
            OnPropertyChanged(nameof(Company));

            if (IsSetup && Problem == null) Pick();
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            var mocks = Store.State.Mocks.OrderByDescending(m => m.Date).ToList();
            History.Clear();
            foreach (var m in mocks.Take(12)) History.Add(new MockHistoryRow { Record = m });
            MockCount = mocks.Count;
            SolveRate = mocks.Count == 0 ? "–" : $"{100.0 * mocks.Count(m => m.Solved) / mocks.Count:0}%";
            var scores = mocks.Select(m => new[] { m.Communication, m.ProblemSolving, m.CodeQuality, m.Testing }.Where(x => x > 0).DefaultIfEmpty(0).Average()).ToList();
            AvgScore = scores.Count == 0 ? "–" : $"{scores.Average():0.0}/4";
            AvgTime = mocks.Count == 0 ? "–" : $"{mocks.Average(m => m.Minutes):0} min";
            OnPropertyChanged(nameof(MockCount));
            OnPropertyChanged(nameof(SolveRate));
            OnPropertyChanged(nameof(AvgScore));
            OnPropertyChanged(nameof(AvgTime));
        }

        private void Pick()
        {
            var pool = Store.SheetProblems.ToList();
            var rng = Random.Shared;
            List<ProblemItem> candidates;
            switch (Source)
            {
                case "Company":
                    candidates = pool.Where(p => p.Def.Companies.Contains(Company, StringComparer.OrdinalIgnoreCase) && p.Difficulty != Difficulty.Easy).ToList();
                    PickReason = $"Commonly reported at {Company}.";
                    break;
                case "Hard":
                    candidates = pool.Where(p => p.Difficulty == Difficulty.Hard && !p.IsSolved).ToList();
                    PickReason = "An unsolved Hard — practise staying calm when stuck.";
                    break;
                case "Random":
                    candidates = pool.Where(p => p.Difficulty == Difficulty.Medium && !p.IsSolved).ToList();
                    PickReason = "A random unsolved Medium — the most common interview level.";
                    break;
                default:
                    var focus = PlanService.FocusPattern(Store);
                    candidates = pool.Where(p => !p.IsSolved && p.Difficulty == Difficulty.Medium && (focus == null || p.PatternKey == focus.Key)).ToList();
                    if (candidates.Count == 0) candidates = pool.Where(p => !p.IsSolved && p.Difficulty != Difficulty.Easy).ToList();
                    PickReason = focus == null ? "An unsolved problem from the sheet." : $"From your weakest pattern: {focus.Name}.";
                    break;
            }
            if (candidates.Count == 0)
            {
                candidates = pool.Where(p => p.Difficulty == Difficulty.Medium).ToList();
                PickReason += " (All matching problems are solved — picking a Medium to re-solve under time.)";
            }
            var previous = Problem;
            var options = candidates.Where(c => c != previous).ToList();
            Problem = options.Count > 0 ? options[rng.Next(options.Count)] : candidates.FirstOrDefault();
            OnPropertyChanged(nameof(PickReason));
        }

        private void Start()
        {
            if (Problem == null) return;
            Phases.Clear();
            var plan = new (string Name, double Share, string[] Prompts)[]
            {
                ("Understand", 0.12, new[] { "Restate the problem in your own words.", "Ask: input size? value ranges? duplicates? empty input?", "Work one example by hand, out loud." }),
                ("Approach", 0.22, new[] { "Start with the brute force and its complexity.", "Find the bottleneck — which pattern removes it?", "Get agreement on the approach before you code." }),
                ("Code", 0.48, new[] { "Write clean code with meaningful names.", "Keep narrating what and why.", "Handle edge cases explicitly, don't hope." }),
                ("Test", 0.18, new[] { "Dry-run your code on the example.", "Try edge cases: empty, single element, duplicates, negatives.", "State final time and space complexity." }),
            };
            foreach (var (name, share, prompts) in plan)
                Phases.Add(new MockPhase { Name = name, Share = share, Prompts = prompts, Minutes = (int)Math.Round(Duration * share) });

            _elapsed = 0;
            HintsUsed = 0;
            HintVisible = false;
            IsPaused = false;
            Scratch = string.Empty;
            Outcome = "Solved";
            Communication = ProblemSolving = CodeQuality = Testing = 3;
            Learnings = string.Empty;
            SetPhase(0);
            Stage = "Running";
            _timer.Start();
            Tick();
        }

        private void SetPhase(int index)
        {
            if (index < 0 || index >= Phases.Count) return;
            _phaseIndex = index;
            for (int i = 0; i < Phases.Count; i++)
            {
                Phases[i].IsDone = i < index;
                Phases[i].IsCurrent = i == index;
            }
            OnPropertyChanged(nameof(CurrentPhase));
            CommandManager.InvalidateRequerySuggested();
        }

        private void Tick()
        {
            // Nudge when you've overstayed the current phase's time box.
            double phaseEnd = Phases.Take(_phaseIndex + 1).Sum(p => p.Share) * Duration * 60;
            PhaseNudge = _elapsed > phaseEnd && _phaseIndex < Phases.Count - 1
                ? $"You're past the {CurrentPhase?.Name.ToLowerInvariant()} time box — consider moving on."
                : string.Empty;
            OnPropertyChanged(nameof(TimerText));
            OnPropertyChanged(nameof(IsOvertime));
            OnPropertyChanged(nameof(TimeProgress));
            OnPropertyChanged(nameof(PhaseNudge));
            OnPropertyChanged(nameof(ElapsedText));
        }

        private void Save()
        {
            var p = Problem;
            if (p == null) return;
            bool solved = Outcome == "Solved";
            Store.State.Mocks.Add(new MockRecord
            {
                ProblemSlug = p.Slug,
                ProblemTitle = p.Title,
                Difficulty = p.Difficulty,
                Minutes = Math.Max(1, (int)Math.Round(_elapsed / 60.0)),
                TargetMinutes = Duration,
                Solved = solved,
                HintsUsed = HintsUsed,
                Communication = Communication,
                ProblemSolving = ProblemSolving,
                CodeQuality = CodeQuality,
                Testing = Testing,
                Notes = Learnings.Trim(),
            });

            if (!string.IsNullOrWhiteSpace(Learnings))
                p.Mistakes = string.IsNullOrWhiteSpace(p.Mistakes) ? Learnings.Trim() : p.Mistakes + "\n" + Learnings.Trim();

            if (solved && !p.IsSolved) Store.MarkSolved(p, HintsUsed > 0 ? Grade.Hard : Grade.Good);
            else if (solved) Store.ReviewProblem(p, HintsUsed > 0 ? Grade.Hard : Grade.Good);
            else if (p.IsSolved) Store.ReviewProblem(p, Grade.Again);
            else if (p.IsTodo) Store.SetStatus(p, ProblemStatus.Attempted);

            Store.LogActivity(2);
            Store.NotifyChanged();
            Stage = "Setup";
            Problem = null;
            Pick();
            RefreshHistory();
            Shell.ShowToast("Mock saved. Learnings were added to the problem's mistake notes.");
        }
    }
}
