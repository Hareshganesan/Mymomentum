using System.Windows.Input;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    /// <summary>One thing to recall: either a solved problem or a Core CS card.</summary>
    public sealed class ReviewEntry
    {
        public ProblemItem? Problem { get; init; }
        public CardItem? Card { get; init; }
        public bool IsProblem => Problem != null;
        public bool IsCard => Card != null;
        public SrsState Srs => Problem?.Progress.Srs ?? Card!.Progress.Srs;
        public string Kicker => IsProblem ? $"Problem · {Problem!.DifficultyText}" : $"Core CS · {Card!.SubjectName}";
        public string Front => IsProblem ? Problem!.Title : Card!.Question;
        public string Prompt => IsProblem
            ? "Before revealing, say out loud: which pattern, the key idea, and the time & space complexity."
            : Card!.IsNew ? "New card — read the answer, then rate how well you already knew it." : "Answer it out loud as you would in an interview.";
    }

    public sealed class ReviewViewModel : PageViewModel
    {
        private readonly List<ReviewEntry> _queue = new();
        private int _index;
        private bool _isRevealed;
        private bool _isActive;
        private bool _isComplete;
        private int _reviewed;
        private int _forgot;

        public ReviewViewModel()
        {
            StartMixedCommand = new RelayCommand(() => StartMixed());
            StartProblemsCommand = new RelayCommand(() => StartProblems(null));
            StartCardsCommand = new RelayCommand(() => StartCards());
            PracticeWeakCommand = new RelayCommand(StartWeak);
            RevealCommand = new RelayCommand(() => IsRevealed = true, () => IsActive && !IsRevealed);
            AgainCommand = new RelayCommand(() => ApplyGrade(Grade.Again), () => IsActive && IsRevealed);
            HardCommand = new RelayCommand(() => ApplyGrade(Grade.Hard), () => IsActive && IsRevealed);
            GoodCommand = new RelayCommand(() => ApplyGrade(Grade.Good), () => IsActive && IsRevealed);
            EasyCommand = new RelayCommand(() => ApplyGrade(Grade.Easy), () => IsActive && IsRevealed);
            SkipCommand = new RelayCommand(Skip, () => IsActive);
            EndCommand = new RelayCommand(() => { IsActive = false; IsComplete = _reviewed > 0; OnShown(); });
            OpenCommand = new RelayCommand(() => Links.Open(Current?.Problem?.Url));
            BackCommand = new RelayCommand(() => { IsComplete = false; OnShown(); });
            GoTodayCommand = new RelayCommand(() => Shell.Navigate("today"));
        }

        public ICommand StartMixedCommand { get; }
        public ICommand StartProblemsCommand { get; }
        public ICommand StartCardsCommand { get; }
        public ICommand PracticeWeakCommand { get; }
        public ICommand RevealCommand { get; }
        public ICommand AgainCommand { get; }
        public ICommand HardCommand { get; }
        public ICommand GoodCommand { get; }
        public ICommand EasyCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand EndCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand GoTodayCommand { get; }

        public int DueProblems { get; private set; }
        public int DueCards { get; private set; }
        public int NewCards { get; private set; }
        public int WeakCount { get; private set; }
        public bool NothingDue => DueProblems + DueCards == 0;
        public string UpcomingText { get; private set; } = string.Empty;

        public bool IsActive { get => _isActive; private set { Set(ref _isActive, value); OnPropertyChanged(nameof(IsIdle)); CommandManager.InvalidateRequerySuggested(); } }
        public bool IsComplete { get => _isComplete; private set { Set(ref _isComplete, value); OnPropertyChanged(nameof(IsIdle)); } }
        public bool IsIdle => !IsActive && !IsComplete;
        public bool IsRevealed { get => _isRevealed; private set { Set(ref _isRevealed, value); CommandManager.InvalidateRequerySuggested(); } }

        public ReviewEntry? Current => _index < _queue.Count ? _queue[_index] : null;
        public string ProgressText => $"{Math.Min(_index + 1, _queue.Count)} / {_queue.Count}";
        public double Progress => _queue.Count == 0 ? 0 : (double)_index / _queue.Count;

        public string AgainLabel => Current?.IsProblem == true ? "Forgot" : "Didn't know";
        public string HardLabel => Current?.IsProblem == true ? "Fuzzy" : "Shaky";
        public string GoodLabel => Current?.IsProblem == true ? "Recalled" : "Knew it";
        public string EasyLabel => "Instant";
        public string AgainInterval => Preview(Grade.Again);
        public string HardInterval => Preview(Grade.Hard);
        public string GoodInterval => Preview(Grade.Good);
        public string EasyInterval => Preview(Grade.Easy);

        public string SummaryText => _reviewed == 0 ? "No reviews this session."
            : $"You reviewed {_reviewed} item{(_reviewed == 1 ? "" : "s")}" + (_forgot > 0 ? $" — {_forgot} came back for tomorrow." : " and recalled them all.");

        private string Preview(Grade g) => Current == null ? string.Empty : Srs.FormatInterval(Srs.PreviewInterval(Current.Srs, g));

        public override void OnShown()
        {
            if (IsActive) return;
            DueProblems = Store.DueProblems.Count();
            DueCards = Store.DueCards.Count();
            // Cards whose very first review happened today count against the daily new-card quota.
            int learnedToday = Store.Cards.Count(c => c.ReviewedToday && c.Progress.Srs.Reps + c.Progress.Srs.Lapses == 1);
            NewCards = Math.Min(Math.Max(0, PlanService.NewCardsPerDay - learnedToday), Store.Cards.Count(c => c.IsNew));
            WeakCount = WeakProblems().Count();

            var next = Store.Problems.Where(p => p.IsSolved && p.Progress.Srs.Due > DateTime.Today).Select(p => p.Progress.Srs.Due!.Value)
                .Concat(Store.Cards.Where(c => c.Progress.Srs.Due > DateTime.Today).Select(c => c.Progress.Srs.Due!.Value))
                .DefaultIfEmpty().Min();
            var tomorrow = Store.Problems.Count(p => p.IsSolved && p.Progress.Srs.Due?.Date == DateTime.Today.AddDays(1))
                         + Store.Cards.Count(c => c.Progress.Srs.Due?.Date == DateTime.Today.AddDays(1));
            UpcomingText = next == default ? "Solve problems and study cards to build your review queue."
                : tomorrow > 0 ? $"{tomorrow} item{(tomorrow == 1 ? "" : "s")} come due tomorrow." : $"Next review on {next:ddd, d MMM}.";
            RaiseAll();
        }

        public void StartMixed()
        {
            var entries = Store.DueProblems.Select(p => new ReviewEntry { Problem = p })
                .Concat(DueCardEntries()).ToList();
            if (entries.Count == 0) entries.AddRange(NewCardEntries());
            Begin(entries);
        }

        public void StartProblems(ProblemItem? first)
        {
            var entries = new List<ReviewEntry>();
            if (first != null) entries.Add(new ReviewEntry { Problem = first });
            entries.AddRange(Store.DueProblems.Where(p => p != first).Select(p => new ReviewEntry { Problem = p }));
            Begin(entries);
        }

        public void StartCards()
        {
            Begin(DueCardEntries().Concat(NewCardEntries()).ToList());
        }

        private void StartWeak() => Begin(WeakProblems().Take(8).Select(p => new ReviewEntry { Problem = p }).ToList());

        private IEnumerable<ReviewEntry> DueCardEntries() => Store.DueCards.Select(c => new ReviewEntry { Card = c });
        private IEnumerable<ReviewEntry> NewCardEntries() => Store.Cards.Where(c => c.IsNew).Take(NewCards == 0 ? PlanService.NewCardsPerDay : NewCards).Select(c => new ReviewEntry { Card = c });

        /// <summary>Solved problems you recalled poorly last time — worth practising ahead of schedule.</summary>
        private IEnumerable<ProblemItem> WeakProblems() => Store.Problems
            .Where(p => p.IsSolved && !p.IsDue && (p.Progress.Srs.LastGrade <= Grade.Hard || p.Progress.Srs.Lapses > 0 || p.Progress.Srs.Ease < 2.3))
            .OrderBy(p => p.Progress.Srs.Ease);

        private void Begin(List<ReviewEntry> entries)
        {
            if (entries.Count == 0)
            {
                Shell.ShowToast("Nothing to review right now.");
                return;
            }
            _queue.Clear();
            _queue.AddRange(entries);
            _index = 0;
            _reviewed = 0;
            _forgot = 0;
            IsComplete = false;
            IsRevealed = false;
            IsActive = true;
            RaiseAll();
        }

        private void ApplyGrade(Grade grade)
        {
            var entry = Current;
            if (entry == null) return;
            if (entry.Problem != null) Store.ReviewProblem(entry.Problem, grade);
            else Store.ReviewCard(entry.Card!, grade);
            _reviewed++;
            if (grade == Grade.Again) _forgot++;
            Advance();
        }

        private void Skip() => Advance();

        private void Advance()
        {
            _index++;
            IsRevealed = false;
            if (_index >= _queue.Count)
            {
                IsActive = false;
                IsComplete = true;
                OnShown();
            }
            RaiseAll();
        }
    }
}
