using Momentum.Content;
using Momentum.Models;
using Momentum.ViewModels;

namespace Momentum.Services
{
    /// <summary>
    /// Builds "what should I do today" from real state: due reviews first (cheap, protects what you know),
    /// then new problems from your current focus pattern (or an upcoming company's list), then Core CS cards.
    /// The plan is generated once per day so it doesn't shuffle under you.
    /// </summary>
    public static class PlanService
    {
        public const int MinutesPerReview = 10;
        public const double MinutesPerCard = 1.5;
        public const int NewCardsPerDay = 4;

        public static DayPlan GetOrBuildToday(AppStore store, bool forceRebuild = false)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            if (!forceRebuild && store.State.Plan?.Date == today) return store.State.Plan;
            var plan = Build(store);
            store.State.Plan = plan;
            store.ScheduleSave();
            return plan;
        }

        public static DayPlan Build(AppStore store)
        {
            var state = store.State;
            var plan = new DayPlan { Date = DateTime.Today.ToString("yyyy-MM-dd") };
            int budget = Math.Max(30, state.Profile.DailyMinutes);

            // 1. Upcoming events (next 3 days) — informational, checked off manually.
            foreach (var app in UpcomingEvents(state, 3))
            {
                var days = (app.NextEventDate!.Value.Date - DateTime.Today).Days;
                plan.Items.Add(new PlanItem
                {
                    Kind = PlanKind.Event,
                    RefId = app.Id,
                    Title = $"{app.Company}: {Fallback(app.NextEventTitle, app.Stage.ToString())} {When(days)}",
                    Subtitle = "Skim the company's tagged problems and your mistake notes.",
                });
            }

            // 2. Reviews — most overdue first, capped so they never eat the whole day.
            int reviewCap = Math.Clamp(budget / 3 / MinutesPerReview, 2, 8);
            foreach (var p in store.DueProblems.OrderByDescending(p => p.OverdueDays).Take(reviewCap))
            {
                plan.Items.Add(new PlanItem
                {
                    Kind = PlanKind.Review,
                    RefId = p.Slug,
                    Title = p.Title,
                    Subtitle = $"Review · {p.PatternName} · {p.DifficultyText}",
                    Minutes = MinutesPerReview,
                });
                budget -= MinutesPerReview;
            }

            // 3. Core CS cards: everything due plus a few new ones.
            int dueCards = store.Cards.Count(c => c.IsDue);
            int newCards = Math.Min(NewCardsPerDay, store.Cards.Count(c => c.IsNew));
            int cardCount = Math.Min(dueCards + newCards, 25);
            if (cardCount > 0)
            {
                int minutes = (int)Math.Ceiling(cardCount * MinutesPerCard);
                plan.Items.Add(new PlanItem
                {
                    Kind = PlanKind.Cards,
                    RefId = cardCount.ToString(),
                    Title = $"Core CS: {cardCount} flashcards",
                    Subtitle = dueCards > 0 ? $"{dueCards} due for review, {Math.Max(0, cardCount - dueCards)} new" : $"{cardCount} new cards from {WeakestSubjectName(store)}",
                    Minutes = minutes,
                });
                budget -= minutes;
            }

            // 4. New problems sized by pace and the remaining time budget.
            int targetNew = Math.Clamp((int)Math.Ceiling(RequiredPerDay(store)), 1, 4);
            foreach (var p in PickNewProblems(store, targetNew + 2))
            {
                if (targetNew == 0 || (budget < p.EstimatedMinutes && plan.Items.Any(i => i.Kind == PlanKind.NewProblem))) break;
                plan.Items.Add(new PlanItem
                {
                    Kind = PlanKind.NewProblem,
                    RefId = p.Slug,
                    Title = p.Title,
                    Subtitle = $"New · {p.PatternName} · {p.DifficultyText}",
                    Minutes = p.EstimatedMinutes,
                });
                budget -= p.EstimatedMinutes;
                targetNew--;
            }

            return plan;
        }

        /// <summary>Problems per day needed to finish the sheet by the target date (2/day if no date is set).</summary>
        public static double RequiredPerDay(AppStore store)
        {
            int remaining = store.SheetProblems.Count(p => !p.IsSolved);
            var target = store.State.Profile.TargetDate;
            if (remaining == 0) return 0;
            if (target == null) return 2;
            var days = Math.Max(1, (target.Value.Date - DateTime.Today).Days);
            return (double)remaining / days;
        }

        /// <summary>The first pattern in roadmap order that's less than 60% done — where new problems come from.</summary>
        public static PatternDef? FocusPattern(AppStore store)
        {
            foreach (var pattern in ProblemCatalog.Patterns)
            {
                var items = store.SheetProblems.Where(p => p.PatternKey == pattern.Key).ToList();
                if (items.Count == 0) continue;
                if (items.Count(p => p.IsSolved) < 0.6 * items.Count) return pattern;
            }
            return null;
        }

        public static IEnumerable<ProblemItem> PickNewProblems(AppStore store, int count)
        {
            var picked = new List<ProblemItem>();
            var unsolved = store.SheetProblems.Where(p => !p.IsSolved).ToList();

            // Company prep wins when an event is close.
            var soon = UpcomingEvents(store.State, 14).FirstOrDefault();
            if (soon != null)
            {
                picked.AddRange(unsolved
                    .Where(p => p.Def.Companies.Any(c => c.Equals(soon.Company.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(p => p.Difficulty == Difficulty.Medium ? 0 : p.Difficulty == Difficulty.Easy ? 1 : 2)
                    .Take(Math.Max(1, count / 2)));
            }

            // Attempted-but-unsolved problems are the cheapest wins.
            picked.AddRange(unsolved.Where(p => p.IsAttempted && !picked.Contains(p)).Take(1));

            // Then walk the roadmap from the focus pattern, easiest first.
            var startIndex = ProblemCatalog.PatternIndex(FocusPattern(store)?.Key ?? ProblemCatalog.Patterns[0].Key);
            foreach (var pattern in ProblemCatalog.Patterns.Skip(startIndex))
            {
                foreach (var p in unsolved.Where(p => p.PatternKey == pattern.Key && !picked.Contains(p))
                                          .OrderBy(p => p.Difficulty).ThenBy(p => p.Def.Order))
                {
                    if (picked.Count >= count) return picked;
                    picked.Add(p);
                }
            }
            return picked.Take(count);
        }

        public static IEnumerable<JobApplication> UpcomingEvents(AppState state, int withinDays) =>
            state.Applications
                .Where(a => a.Stage != Stage.Rejected && a.Stage != Stage.Offer && a.NextEventDate.HasValue)
                .Where(a =>
                {
                    var days = (a.NextEventDate!.Value.Date - DateTime.Today).Days;
                    return days >= 0 && days <= withinDays;
                })
                .OrderBy(a => a.NextEventDate);

        public static string WeakestSubjectName(AppStore store)
        {
            var weakest = CsCatalog.Subjects
                .Select(s => (s.Name, Ratio: Ratio(store.Cards.Where(c => c.Subject == s.Key).ToList())))
                .OrderBy(x => x.Ratio).FirstOrDefault();
            return weakest.Name ?? "Core CS";

            static double Ratio(List<CardItem> cards) => cards.Count == 0 ? 1 : (double)cards.Count(c => c.IsKnown) / cards.Count;
        }

        private static string When(int days) => days switch { 0 => "today", 1 => "tomorrow", _ => $"in {days} days" };
        private static string Fallback(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;

        /// <summary>Whether a plan item has been satisfied by what the user actually did.</summary>
        public static bool IsSatisfied(AppStore store, PlanItem item) => item.Kind switch
        {
            PlanKind.Review => store.ProblemsBySlug.TryGetValue(item.RefId, out var r) && !r.IsDue,
            PlanKind.NewProblem => store.ProblemsBySlug.TryGetValue(item.RefId, out var n) && n.IsSolved,
            PlanKind.Cards => int.TryParse(item.RefId, out var target) && store.Cards.Count(c => c.ReviewedToday) >= target,
            _ => item.Done,
        };
    }
}
