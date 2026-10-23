using Momentum.Models;

namespace Momentum.Services
{
    /// <summary>
    /// SM-2 style spaced repetition. First successful recall waits a few days, then intervals grow by the ease factor;
    /// a lapse resets the card to tomorrow and makes it permanently a little harder.
    /// </summary>
    public static class Srs
    {
        public static void Apply(SrsState s, Grade grade, DateTime today)
        {
            switch (grade)
            {
                case Grade.Again:
                    s.Reps = 0;
                    s.Lapses++;
                    s.Ease = Math.Max(1.3, s.Ease - 0.2);
                    s.Interval = 1;
                    break;
                case Grade.Hard:
                    s.Ease = Math.Max(1.3, s.Ease - 0.15);
                    s.Interval = s.Reps == 0 ? 2 : Math.Max(s.Interval + 1, (int)Math.Round(s.Interval * 1.2));
                    s.Reps++;
                    break;
                case Grade.Good:
                    s.Interval = s.Reps switch { 0 => 4, 1 => 10, _ => (int)Math.Round(s.Interval * s.Ease) };
                    s.Reps++;
                    break;
                case Grade.Easy:
                    s.Ease += 0.15;
                    s.Interval = s.Reps switch { 0 => 7, 1 => 16, _ => (int)Math.Round(s.Interval * s.Ease * 1.3) };
                    s.Reps++;
                    break;
            }

            s.Interval = Math.Clamp(s.Interval, 1, 180);
            s.Last = today;
            s.LastGrade = grade;
            s.Due = today.Date.AddDays(s.Interval);
        }

        public static int PreviewInterval(SrsState s, Grade grade)
        {
            var copy = new SrsState { Ease = s.Ease, Interval = s.Interval, Reps = s.Reps, Lapses = s.Lapses };
            Apply(copy, grade, DateTime.Today);
            return copy.Interval;
        }

        public static string FormatInterval(int days) => days switch
        {
            <= 1 => "1 day",
            < 30 => $"{days} days",
            < 365 => $"{days / 30.0:0.#} mo",
            _ => $"{days / 365.0:0.#} yr",
        };

        public static string DueText(DateTime? due)
        {
            if (due == null) return string.Empty;
            var days = (due.Value.Date - DateTime.Today).Days;
            return days switch
            {
                < -1 => $"Overdue {-days}d",
                -1 => "Overdue 1d",
                0 => "Due today",
                1 => "Due tomorrow",
                _ => $"Review in {days}d",
            };
        }
    }
}
