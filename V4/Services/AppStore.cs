using System.Windows.Threading;
using Momentum.Content;
using Momentum.Models;
using Momentum.ViewModels;

namespace Momentum.Services
{
    /// <summary>
    /// Single source of truth: owns the persisted <see cref="AppState"/>, the joined problem/card items, and every
    /// mutation that should be logged as activity. Pages call into here and listen to <see cref="Changed"/>.
    /// </summary>
    public sealed class AppStore
    {
        public static AppStore Instance { get; } = new();

        private readonly DispatcherTimer _saveTimer;

        private AppStore()
        {
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _saveTimer.Tick += (_, _) => SaveNow();
        }

        public AppState State { get; private set; } = new();

        /// <summary>False until an account signs in (and after sign-out), so nothing is written to the wrong place.</summary>
        public bool IsLoaded { get; private set; }

        public void Unload()
        {
            SaveNow();
            IsLoaded = false;
        }
        public List<ProblemItem> Problems { get; } = new();
        public Dictionary<string, ProblemItem> ProblemsBySlug { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CardItem> Cards { get; } = new();

        /// <summary>Raised after any change to progress so pages and badges can refresh.</summary>
        public event Action? Changed;

        public void Load()
        {
            State = StorageService.Load() ?? new AppState();
            IsLoaded = true;
            Rebuild();
        }

        public void Replace(AppState state)
        {
            State = state;
            Rebuild();
            SaveNow();
            NotifyChanged();
        }

        private void Rebuild()
        {
            Problems.Clear();
            ProblemsBySlug.Clear();
            foreach (var def in ProblemCatalog.Problems) AddProblemItem(def);
            foreach (var custom in State.CustomProblems) AddProblemItem(ToDef(custom));

            Cards.Clear();
            foreach (var def in CsCatalog.Cards) AddCardItem(def);
            foreach (var custom in State.CustomCards) AddCardItem(ToDef(custom));
        }

        private ProblemItem AddProblemItem(ProblemDef def)
        {
            if (!State.Problems.TryGetValue(def.Slug, out var progress))
            {
                progress = new ProblemProgress();
                State.Problems[def.Slug] = progress;
            }
            var item = new ProblemItem(def, progress);
            Problems.Add(item);
            ProblemsBySlug[def.Slug] = item;
            return item;
        }

        private CardItem AddCardItem(CardDef def)
        {
            if (!State.Cards.TryGetValue(def.Id, out var progress))
            {
                progress = new CardProgress();
                State.Cards[def.Id] = progress;
            }
            var item = new CardItem(def, progress);
            Cards.Add(item);
            return item;
        }

        private static ProblemDef ToDef(CustomProblem c) => new()
        {
            Slug = c.Slug,
            Title = c.Title,
            Difficulty = c.Difficulty,
            Pattern = c.Pattern,
            CustomUrl = string.IsNullOrWhiteSpace(c.Url) ? null : c.Url,
            Companies = c.Companies.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            IsCustom = true,
            Order = 10_000,
        };

        private static CardDef ToDef(CustomCard c) => new() { Id = c.Id, Subject = c.Subject, Question = c.Question, Answer = c.Answer, IsCustom = true };

        // ---------- Persistence ----------

        public void ScheduleSave()
        {
            if (!IsLoaded) return;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        public void SaveNow()
        {
            _saveTimer.Stop();
            if (IsLoaded) StorageService.Save(State);
        }

        public void NotifyChanged()
        {
            ScheduleSave();
            Changed?.Invoke();
        }

        public void LogActivity(int amount = 1)
        {
            var key = DateTime.Today.ToString("yyyy-MM-dd");
            State.Activity.TryGetValue(key, out var n);
            State.Activity[key] = n + amount;
        }

        // ---------- Problem actions ----------

        /// <summary>First solve: records how it went, which seeds the first review interval.</summary>
        public void MarkSolved(ProblemItem p, Grade howItWent)
        {
            var wasSolved = p.IsSolved;
            p.Progress.Status = ProblemStatus.Solved;
            p.Progress.SolvedOn ??= DateTime.Now;
            Srs.Apply(p.Progress.Srs, howItWent, DateTime.Today);
            if (!wasSolved) LogActivity();
            p.Refresh();
            NotifyChanged();
        }

        public void ReviewProblem(ProblemItem p, Grade grade)
        {
            Srs.Apply(p.Progress.Srs, grade, DateTime.Today);
            LogActivity();
            p.Refresh();
            NotifyChanged();
        }

        public void SetStatus(ProblemItem p, ProblemStatus status)
        {
            p.Progress.Status = status;
            if (status != ProblemStatus.Solved)
            {
                p.Progress.Srs = new SrsState();
                p.Progress.SolvedOn = null;
                p.Progress.FromLeetCode = false;
            }
            p.Refresh();
            NotifyChanged();
        }

        public ProblemItem AddCustomProblem(CustomProblem custom)
        {
            State.CustomProblems.Add(custom);
            var item = AddProblemItem(ToDef(custom));
            NotifyChanged();
            return item;
        }

        public void DeleteCustomProblem(ProblemItem p)
        {
            if (!p.IsCustom) return;
            State.CustomProblems.RemoveAll(c => c.Slug.Equals(p.Slug, StringComparison.OrdinalIgnoreCase));
            State.Problems.Remove(p.Slug);
            Problems.Remove(p);
            ProblemsBySlug.Remove(p.Slug);
            NotifyChanged();
        }

        // ---------- Card actions ----------

        public void ReviewCard(CardItem c, Grade grade)
        {
            Srs.Apply(c.Progress.Srs, grade, DateTime.Today);
            LogActivity();
            c.Refresh();
            NotifyChanged();
        }

        public CardItem AddCustomCard(CustomCard card)
        {
            State.CustomCards.Add(card);
            var item = AddCardItem(ToDef(card));
            NotifyChanged();
            return item;
        }

        public void DeleteCustomCard(CardItem c)
        {
            if (!c.IsCustom) return;
            State.CustomCards.RemoveAll(x => x.Id == c.Id);
            State.Cards.Remove(c.Id);
            Cards.Remove(c);
            NotifyChanged();
        }

        // ---------- LeetCode ----------

        /// <summary>
        /// Marks sheet problems solved on LeetCode. Problems you solved long ago are queued for review a few per day
        /// instead of all at once, so a big import doesn't bury you.
        /// </summary>
        public int ApplyLeetCodeSolved(LeetCodeResult result)
        {
            if (result.Snapshot != null) State.LeetCode = result.Snapshot;

            var newlySolved = Problems
                .Where(p => !p.IsSolved && result.Solved.ContainsKey(p.Slug))
                .OrderBy(p => ProblemCatalog.PatternIndex(p.PatternKey)).ThenBy(p => p.Def.Order)
                .ToList();

            int staggered = 0;
            foreach (var p in newlySolved)
            {
                var when = result.Solved[p.Slug];
                p.Progress.Status = ProblemStatus.Solved;
                p.Progress.FromLeetCode = true;
                p.Progress.SolvedOn = when ?? p.Progress.SolvedOn;

                var recent = when.HasValue && (DateTime.Now - when.Value).TotalDays < 3;
                p.Progress.Srs = new SrsState
                {
                    // Fresh solves get the normal first interval; old ones trickle in at ~4 per day.
                    Due = recent ? DateTime.Today.AddDays(3) : DateTime.Today.AddDays(1 + staggered++ / 4),
                };
                p.Refresh();
            }

            if (newlySolved.Count > 0) LogActivity(newlySolved.Count(p => p.Progress.SolvedOn?.Date == DateTime.Today));
            NotifyChanged();
            return newlySolved.Count;
        }

        // ---------- Derived stats ----------

        public IEnumerable<ProblemItem> SheetProblems => Problems.Where(p => !p.IsCustom);
        public IEnumerable<ProblemItem> DueProblems => Problems.Where(p => p.IsDue).OrderBy(p => p.Progress.Srs.Due);
        public IEnumerable<CardItem> DueCards => Cards.Where(c => c.IsDue).OrderBy(c => c.Progress.Srs.Due);

        /// <summary>Merged daily activity: in-app actions plus LeetCode submissions.</summary>
        public Dictionary<string, int> MergedActivity()
        {
            var merged = new Dictionary<string, int>(State.Activity);
            if (State.LeetCode != null)
            {
                foreach (var (day, count) in State.LeetCode.Calendar)
                {
                    merged.TryGetValue(day, out var n);
                    merged[day] = n + count;
                }
            }
            return merged;
        }

        public int CurrentStreak()
        {
            var activity = MergedActivity();
            var day = DateTime.Today;
            // Today not done yet doesn't break the streak.
            if (!activity.TryGetValue(day.ToString("yyyy-MM-dd"), out var t) || t == 0) day = day.AddDays(-1);
            int streak = 0;
            while (activity.TryGetValue(day.ToString("yyyy-MM-dd"), out var n) && n > 0)
            {
                streak++;
                day = day.AddDays(-1);
            }
            return streak;
        }

        public int SolvedInLastDays(int days)
        {
            var since = DateTime.Today.AddDays(-days + 1);
            return Problems.Count(p => p.IsSolved && p.Progress.SolvedOn.HasValue && p.Progress.SolvedOn.Value.Date >= since);
        }
    }
}
