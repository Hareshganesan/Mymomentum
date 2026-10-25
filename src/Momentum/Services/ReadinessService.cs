using Momentum.Content;
using Momentum.Models;

namespace Momentum.Services
{
    public sealed class ReadinessPart
    {
        public string Name { get; init; } = string.Empty;
        public double Weight { get; init; }
        public double Ratio { get; init; }
        public string Detail { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string NavKey { get; init; } = string.Empty;

        public double Points => Math.Round(Weight * Ratio, 1);
        public double Percent => Math.Round(Ratio * 100);
        public string PointsText => $"{Points:0}/{Weight:0}";
        /// <summary>How many points are on the table — used to pick the next best action.</summary>
        public double Gap => Weight * (1 - Ratio);
    }

    public sealed class Readiness
    {
        public List<ReadinessPart> Parts { get; init; } = new();
        public double Score => Math.Round(Parts.Sum(p => p.Points));
        public ReadinessPart? NextBest => Parts.OrderByDescending(p => p.Gap).FirstOrDefault();

        public string Level => Score switch
        {
            >= 85 => "Interview ready",
            >= 65 => "Strong candidate",
            >= 45 => "OA ready",
            >= 25 => "Building up",
            _ => "Getting started",
        };
    }

    /// <summary>
    /// Readiness is a transparent weighted score — every point maps to something you can go and do.
    /// Retention counts as much as volume: solving 200 problems you can't recall doesn't make you ready.
    /// </summary>
    public static class ReadinessService
    {
        public static Readiness Compute(AppStore store)
        {
            var state = store.State;
            var sheet = store.SheetProblems.ToList();
            var solved = store.Problems.Where(p => p.IsSolved).ToList();

            static int W(Difficulty d) => d switch { Difficulty.Easy => 1, Difficulty.Medium => 2, _ => 3 };
            double coverage = sheet.Count == 0 ? 0 : (double)sheet.Where(p => p.IsSolved).Sum(p => W(p.Difficulty)) / sheet.Sum(p => W(p.Difficulty));

            int patternsCovered = ProblemCatalog.Patterns.Count(pat =>
            {
                var items = sheet.Where(p => p.PatternKey == pat.Key).ToList();
                return items.Count > 0 && items.Count(p => p.IsSolved) >= Math.Ceiling(items.Count * 0.5);
            });
            double breadth = (double)patternsCovered / ProblemCatalog.Patterns.Count;

            // Retained = recalled well at the last review and not badly overdue, or solved within the last week.
            int retained = solved.Count(p =>
            {
                var s = p.Progress.Srs;
                var overdue = s.Due.HasValue ? (DateTime.Today - s.Due.Value.Date).Days : 0;
                if (s.Last == null) return p.Progress.SolvedOn.HasValue && (DateTime.Today - p.Progress.SolvedOn.Value.Date).Days <= 7 && !p.Progress.FromLeetCode;
                return s.LastGrade >= Grade.Good && overdue <= 3;
            });
            double retention = solved.Count == 0 ? 0 : (double)retained / solved.Count;
            // Retention of a tiny set shouldn't be worth full marks.
            retention *= Math.Min(1, solved.Count / 20.0);

            var cards = store.Cards;
            int cardsKnown = cards.Count(c => c.IsKnown && (!c.IsDue || (DateTime.Today - c.Progress.Srs.Due!.Value.Date).Days <= 7));
            double cs = cards.Count == 0 ? 0 : (double)cardsKnown / cards.Count;

            var recentMocks = state.Mocks.OrderByDescending(m => m.Date).Take(5).ToList();
            double mockVolume = Math.Min(1, state.Mocks.Count / 6.0);
            double mockQuality = recentMocks.Count == 0 ? 0 : recentMocks.Average(m => (m.Solved ? 1.0 : 0.4) * Avg(m) / 4.0);
            double mocks = 0.5 * mockVolume + 0.5 * mockQuality;

            int themesCovered = BehavioralCatalog.Themes.Count(t => state.Stories.Any(s => s.Themes.Contains(t)));
            bool hasPitch = !string.IsNullOrWhiteSpace(state.Profile.PitchPresent) && !string.IsNullOrWhiteSpace(state.Profile.PitchFuture);
            double hr = 0.8 * themesCovered / BehavioralCatalog.Themes.Count + (hasPitch ? 0.2 : 0);

            int sheetSolved = sheet.Count(p => p.IsSolved);
            return new Readiness
            {
                Parts =
                {
                    new ReadinessPart
                    {
                        Name = "Problem coverage", Weight = 30, Ratio = coverage, NavKey = "problems",
                        Detail = $"{sheetSolved}/{sheet.Count} sheet problems, weighted by difficulty",
                        Action = "Solve today's new problems — Mediums are worth twice an Easy.",
                    },
                    new ReadinessPart
                    {
                        Name = "Pattern breadth", Weight = 15, Ratio = breadth, NavKey = "problems",
                        Detail = $"{patternsCovered}/{ProblemCatalog.Patterns.Count} patterns at least half done",
                        Action = $"Work on {PlanService.FocusPattern(store)?.Name ?? "your weakest pattern"} next.",
                    },
                    new ReadinessPart
                    {
                        Name = "Retention", Weight = 15, Ratio = retention, NavKey = "review",
                        Detail = solved.Count == 0 ? "Nothing to retain yet" : $"{retained}/{solved.Count} solved problems you can still recall",
                        Action = "Clear your review queue — it takes ~10 minutes per problem.",
                    },
                    new ReadinessPart
                    {
                        Name = "Core CS", Weight = 20, Ratio = cs, NavKey = "corecs",
                        Detail = $"{cardsKnown}/{cards.Count} cards known (OS, DBMS, SQL, CN, OOP, SD)",
                        Action = $"Study a few new cards from {PlanService.WeakestSubjectName(store)}.",
                    },
                    new ReadinessPart
                    {
                        Name = "Mock interviews", Weight = 10, Ratio = mocks, NavKey = "mock",
                        Detail = state.Mocks.Count == 0 ? "No mock interviews yet" : $"{state.Mocks.Count} mocks, recent avg {recentMocks.Average(m => Avg(m)):0.0}/4",
                        Action = "Run a 45-minute timed mock — talk out loud the whole time.",
                    },
                    new ReadinessPart
                    {
                        Name = "HR & behavioural", Weight = 10, Ratio = hr, NavKey = "stories",
                        Detail = $"{themesCovered}/{BehavioralCatalog.Themes.Count} story themes covered{(hasPitch ? ", pitch ready" : ", no pitch yet")}",
                        Action = hasPitch ? "Write one STAR story for a theme you haven't covered." : "Draft your 'Tell me about yourself' pitch.",
                    },
                }
            };
        }

        private static double Avg(MockRecord m)
        {
            var scores = new[] { m.Communication, m.ProblemSolving, m.CodeQuality, m.Testing }.Where(s => s > 0).ToList();
            return scores.Count == 0 ? 0 : scores.Average();
        }
    }
}
