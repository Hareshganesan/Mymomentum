using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class PatternStat
    {
        public string Name { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
        public int Solved { get; init; }
        public int Total { get; init; }
        public int Due { get; init; }
        public int LeetCodeSolved { get; init; }
        public double Progress => Total == 0 ? 0 : (double)Solved / Total;
        public string CountText => $"{Solved}/{Total}";
        public string Detail => (Due > 0 ? $"{Due} due · " : "") + (LeetCodeSolved > 0 ? $"{LeetCodeSolved} on LeetCode" : "—");
    }

    public sealed class WeekBar
    {
        public string Label { get; init; } = string.Empty;
        public int Value { get; init; }
        public double Height { get; init; }
    }

    public sealed class InsightsViewModel : PageViewModel
    {
        public InsightsViewModel()
        {
            GoCommand = new RelayCommand<string>(key => Shell.Navigate(key));
            PatternCommand = new RelayCommand<string>(key => Shell.Navigate<ProblemsViewModel>("problems").ShowPattern(key));
        }

        public ICommand GoCommand { get; }
        public ICommand PatternCommand { get; }

        public Readiness Readiness { get; private set; } = new();
        public List<PatternStat> Patterns { get; private set; } = new();
        public List<WeekBar> Weeks { get; private set; } = new();
        public Dictionary<string, int> Activity { get; private set; } = new();
        public string Weakest { get; private set; } = string.Empty;

        public bool HasLeetCode => Store.State.LeetCode != null;
        public Models.LeetCodeSnapshot? LeetCode => Store.State.LeetCode;

        public override void OnShown()
        {
            Readiness = ReadinessService.Compute(Store);
            var tags = Store.State.LeetCode?.Tags ?? new();
            Patterns = ProblemCatalog.Patterns.Select(p =>
            {
                var items = Store.SheetProblems.Where(x => x.PatternKey == p.Key).ToList();
                var lc = ProblemCatalog.PatternToLeetCodeTags.TryGetValue(p.Key, out var t) ? t.Select(s => tags.GetValueOrDefault(s)).DefaultIfEmpty(0).Max() : 0;
                return new PatternStat { Key = p.Key, Name = p.Name, Solved = items.Count(x => x.IsSolved), Total = items.Count, Due = items.Count(x => x.IsDue), LeetCodeSolved = lc };
            }).ToList();

            var weak = Patterns.Where(p => p.Total > 0).OrderBy(p => p.Progress).ThenBy(p => p.LeetCodeSolved).Take(3).Select(p => p.Name);
            Weakest = "Weakest patterns right now: " + string.Join(", ", weak) + ".";

            Activity = Store.MergedActivity();
            var monday = DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
            var raw = Enumerable.Range(0, 12).Select(i =>
            {
                var start = monday.AddDays(-7 * (11 - i));
                var sum = Enumerable.Range(0, 7).Sum(d => Activity.GetValueOrDefault(start.AddDays(d).ToString("yyyy-MM-dd")));
                return (Label: start.ToString("d MMM"), Value: sum);
            }).ToList();
            var max = Math.Max(1, raw.Max(r => r.Value));
            Weeks = raw.Select(r => new WeekBar { Label = r.Label, Value = r.Value, Height = 4 + 96.0 * r.Value / max }).ToList();
            RaiseAll();
        }
    }
}
