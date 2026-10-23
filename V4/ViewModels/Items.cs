using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    /// <summary>A problem from the sheet (or a custom one) joined with the user's progress on it.</summary>
    public sealed class ProblemItem : ObservableObject
    {
        public ProblemItem(ProblemDef def, ProblemProgress progress)
        {
            Def = def;
            Progress = progress;
        }

        public ProblemDef Def { get; }
        public ProblemProgress Progress { get; }

        public string Slug => Def.Slug;
        public string Title => Def.Title;
        public Difficulty Difficulty => Def.Difficulty;
        public string DifficultyText => Def.Difficulty.ToString();
        public string PatternKey => Def.Pattern;
        public string PatternName => ProblemCatalog.PatternName(Def.Pattern);
        public string PatternCue => ProblemCatalog.GetPattern(Def.Pattern)?.Cue ?? string.Empty;
        public string PatternRecipe => ProblemCatalog.GetPattern(Def.Pattern)?.Recipe ?? string.Empty;
        public string Hint => Def.Hint;
        public bool HasHint => !string.IsNullOrWhiteSpace(Def.Hint);
        public string Url => Def.Url;
        public bool IsCustom => Def.IsCustom;
        public string CompaniesText => string.Join(" · ", Def.Companies);
        public bool HasCompanies => Def.Companies.Length > 0;
        public int EstimatedMinutes => Def.EstimatedMinutes;

        public ProblemStatus Status => Progress.Status;
        public bool IsSolved => Progress.Status == ProblemStatus.Solved;
        public bool IsAttempted => Progress.Status == ProblemStatus.Attempted;
        public bool IsTodo => Progress.Status == ProblemStatus.Todo;

        public bool IsDue => IsSolved && Progress.Srs.Due.HasValue && Progress.Srs.Due.Value.Date <= DateTime.Today;
        public int OverdueDays => IsDue ? (DateTime.Today - Progress.Srs.Due!.Value.Date).Days : 0;
        public string DueText => IsSolved ? Srs.DueText(Progress.Srs.Due) : string.Empty;

        public string StatusText => Progress.Status switch
        {
            ProblemStatus.Solved => Progress.FromLeetCode && Progress.Srs.Reps == 0 ? "Solved on LeetCode" : "Solved",
            ProblemStatus.Attempted => "Attempted",
            _ => "Not started",
        };

        public string ReviewSummary
        {
            get
            {
                if (!IsSolved) return "Solve it once to start spaced review.";
                var s = Progress.Srs;
                var reviewed = s.Reps + s.Lapses == 0 ? "Not reviewed yet" : $"Reviewed {s.Reps + s.Lapses}×, ease {s.Ease:0.0}";
                return $"{reviewed} · {Srs.DueText(s.Due)}";
            }
        }

        public string Notes
        {
            get => Progress.Notes;
            set { if (Progress.Notes == value) return; Progress.Notes = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNotes)); AppStore.Instance.ScheduleSave(); }
        }

        public bool HasNotes => !string.IsNullOrWhiteSpace(Progress.Notes);

        public string Mistakes
        {
            get => Progress.Mistakes;
            set { if (Progress.Mistakes == value) return; Progress.Mistakes = value; OnPropertyChanged(); AppStore.Instance.ScheduleSave(); }
        }

        public bool Starred
        {
            get => Progress.Starred;
            set { if (Progress.Starred == value) return; Progress.Starred = value; OnPropertyChanged(); AppStore.Instance.ScheduleSave(); }
        }

        public void Refresh() => RaiseAll();
    }

    /// <summary>A Core CS flashcard joined with its review state.</summary>
    public sealed class CardItem : ObservableObject
    {
        private bool _isExpanded;

        public CardItem(CardDef def, CardProgress progress)
        {
            Def = def;
            Progress = progress;
        }

        public CardDef Def { get; }
        public CardProgress Progress { get; }

        public string Id => Def.Id;
        public string Subject => Def.Subject;
        public string SubjectName => CsCatalog.SubjectName(Def.Subject);
        public string Question => Def.Question;
        public string Answer => Def.Answer;
        public bool IsCustom => Def.IsCustom;

        public bool IsNew => Progress.Srs.Last == null;
        public bool IsDue => Progress.Srs.Due.HasValue && Progress.Srs.Due.Value.Date <= DateTime.Today;
        public bool IsKnown => !IsNew && Progress.Srs.LastGrade >= Grade.Good;
        public bool ReviewedToday => Progress.Srs.Last?.Date == DateTime.Today;

        public string StateText
        {
            get
            {
                if (IsNew) return "New";
                if (IsDue) return Srs.DueText(Progress.Srs.Due);
                return Progress.Srs.LastGrade switch
                {
                    Grade.Again => "Didn't know",
                    Grade.Hard => "Shaky",
                    _ => "Known",
                } + " · " + Srs.DueText(Progress.Srs.Due);
            }
        }

        public bool Starred
        {
            get => Progress.Starred;
            set { if (Progress.Starred == value) return; Progress.Starred = value; OnPropertyChanged(); AppStore.Instance.ScheduleSave(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }

        public void Refresh() => RaiseAll();
    }
}
