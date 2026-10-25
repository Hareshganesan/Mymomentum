using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Momentum.Controls
{
    /// <summary>GitHub/LeetCode-style activity grid. Columns are weeks (oldest left), rows are Mon..Sun.</summary>
    public sealed class Heatmap : FrameworkElement
    {
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data), typeof(IDictionary<string, int>), typeof(Heatmap),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty WeeksProperty = DependencyProperty.Register(
            nameof(Weeks), typeof(int), typeof(Heatmap),
            new FrameworkPropertyMetadata(20, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty CellProperty = DependencyProperty.Register(
            nameof(Cell), typeof(double), typeof(Heatmap),
            new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public IDictionary<string, int>? Data { get => (IDictionary<string, int>?)GetValue(DataProperty); set => SetValue(DataProperty, value); }
        public int Weeks { get => (int)GetValue(WeeksProperty); set => SetValue(WeeksProperty, value); }
        public double Cell { get => (double)GetValue(CellProperty); set => SetValue(CellProperty, value); }

        private const double Gap = 3;

        public Heatmap()
        {
            ToolTipService.SetInitialShowDelay(this, 150);
            ToolTip = string.Empty;
        }

        protected override Size MeasureOverride(Size availableSize) =>
            new(Weeks * (Cell + Gap), 7 * (Cell + Gap));

        private DateTime StartMonday
        {
            get
            {
                var today = DateTime.Today;
                int sinceMonday = ((int)today.DayOfWeek + 6) % 7;
                return today.AddDays(-sinceMonday - (Weeks - 1) * 7);
            }
        }

        private Brush Res(string key) => TryFindResource(key) as Brush ?? Brushes.Gray;

        protected override void OnRender(DrawingContext dc)
        {
            var data = Data ?? new Dictionary<string, int>();
            var start = StartMonday;
            var today = DateTime.Today;
            var levels = new[] { Res("HeatEmpty"), Res("Heat1"), Res("Heat2"), Res("Heat3"), Res("Heat4") };

            for (int w = 0; w < Weeks; w++)
            {
                for (int d = 0; d < 7; d++)
                {
                    var date = start.AddDays(w * 7 + d);
                    if (date > today) continue;
                    data.TryGetValue(date.ToString("yyyy-MM-dd"), out var n);
                    var level = n switch { <= 0 => 0, <= 2 => 1, <= 5 => 2, <= 9 => 3, _ => 4 };
                    var rect = new Rect(w * (Cell + Gap), d * (Cell + Gap), Cell, Cell);
                    dc.DrawRoundedRectangle(levels[level], null, rect, 2.5, 2.5);
                    if (date == today)
                        dc.DrawRoundedRectangle(null, new Pen(Res("Accent"), 1.2), rect, 2.5, 2.5);
                }
            }
        }

        protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var p = e.GetPosition(this);
            int w = (int)(p.X / (Cell + Gap)), d = (int)(p.Y / (Cell + Gap));
            if (w < 0 || w >= Weeks || d < 0 || d > 6) return;
            var date = StartMonday.AddDays(w * 7 + d);
            if (date > DateTime.Today) { ToolTip = null; return; }
            (Data ?? new Dictionary<string, int>()).TryGetValue(date.ToString("yyyy-MM-dd"), out var n);
            ToolTip = $"{date:ddd, d MMM} · {(n == 0 ? "no activity" : n == 1 ? "1 action" : $"{n} actions")}";
        }
    }
}
