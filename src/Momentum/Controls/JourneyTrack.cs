using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Momentum.ViewModels;

namespace Momentum.Controls
{
    /// <summary>
    /// High-performance timeline track for job application stages.
    /// Draws all 5 stages, connecting lines, state dots and date labels in a single direct DrawingContext pass.
    /// Replaces dozens of nested ItemsControls, DataTemplates, and Ellipses with 0 child visual objects.
    /// </summary>
    public sealed class JourneyTrack : FrameworkElement
    {
        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            nameof(Nodes), typeof(IReadOnlyList<TrackNode>), typeof(JourneyTrack),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public IReadOnlyList<TrackNode>? Nodes
        {
            get => (IReadOnlyList<TrackNode>?)GetValue(NodesProperty);
            set => SetValue(NodesProperty, value);
        }

        private static readonly Typeface DefaultTypeface = new(
            new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"),
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

        private static readonly Typeface CurrentTypeface = new(
            new FontFamily("Segoe UI Variable Text, Segoe UI, sans-serif"),
            FontStyles.Normal,
            FontWeights.Medium,
            FontStretches.Normal);

        public JourneyTrack()
        {
            Height = 44;
            MinWidth = 280;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            double w = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0 ? 320 : availableSize.Width;
            return new Size(w, 44);
        }

        private Brush Res(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;

        protected override void OnRender(DrawingContext dc)
        {
            var nodes = Nodes;
            if (nodes == null || nodes.Count == 0) return;

            int count = nodes.Count;
            double w = ActualWidth;
            if (w <= 0) return;

            double colWidth = w / count;
            double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // Resolve theme brushes once for this render pass
            var accent = Res("Accent", Brushes.DodgerBlue);
            var accentSoft = Res("AccentSoft", Brushes.LightSkyBlue);
            var accentLine = Res("AccentLine", accent);
            var border = Res("Border", Brushes.DimGray);
            var borderStrong = Res("BorderStrong", Brushes.Gray);
            var surface = Res("Surface", Brushes.Black);
            var danger = Res("Danger", Brushes.Crimson);
            var dangerSoft = Res("DangerSoft", Brushes.IndianRed);
            var textBrush = Res("Text", Brushes.White);
            var textFaintBrush = Res("TextFaint", Brushes.Gray);

            var doneLinePen = new Pen(accentLine, 2);
            doneLinePen.Freeze();
            var pendingLinePen = new Pen(border, 1.5);
            pendingLinePen.Freeze();

            var currentRingPen = new Pen(accent, 1.5);
            currentRingPen.Freeze();
            var dangerRingPen = new Pen(danger, 1.5);
            dangerRingPen.Freeze();
            var dangerXPen = new Pen(danger, 1.5);
            dangerXPen.Freeze();
            var passedPen = new Pen(accent, 1.5);
            passedPen.Freeze();
            var notReachedPen = new Pen(borderStrong, 1.5);
            notReachedPen.Freeze();

            const double dotY = 8.0;

            // 1. Draw connecting lines between nodes
            for (int i = 0; i < count - 1; i++)
            {
                var node = nodes[i];
                if (!node.HasNext) continue;

                double x1 = i * colWidth + 8.0 + 8.0; // after the dot
                double x2 = (i + 1) * colWidth + 8.0 - 8.0; // before the next dot
                if (x2 > x1)
                {
                    dc.DrawLine(node.LineDone ? doneLinePen : pendingLinePen, new Point(x1, dotY), new Point(x2, dotY));
                }
            }

            // 2. Draw dots and labels
            for (int i = 0; i < count; i++)
            {
                var node = nodes[i];
                double cx = i * colWidth + 8.0;

                if (node.IsRejectedHere)
                {
                    // Outer danger halo
                    dc.DrawEllipse(dangerSoft, dangerRingPen, new Point(cx, dotY), 7.5, 7.5);
                    // 'X' icon
                    const double r = 3.0;
                    dc.DrawLine(dangerXPen, new Point(cx - r, dotY - r), new Point(cx + r, dotY + r));
                    dc.DrawLine(dangerXPen, new Point(cx - r, dotY + r), new Point(cx + r, dotY - r));
                }
                else if (node.IsCurrent)
                {
                    // Current active stage: pulsating ring around dot
                    dc.DrawEllipse(accentSoft, currentRingPen, new Point(cx, dotY), 8.0, 8.0);
                    dc.DrawEllipse(accent, null, new Point(cx, dotY), 4.5, 4.5);
                }
                else if (node.IsPassed)
                {
                    // Passed stage: filled accent dot
                    dc.DrawEllipse(accent, passedPen, new Point(cx, dotY), 5.0, 5.0);
                }
                else
                {
                    // Not reached yet
                    dc.DrawEllipse(surface, notReachedPen, new Point(cx, dotY), 5.0, 5.0);
                }

                // Label
                if (!string.IsNullOrEmpty(node.Label))
                {
                    var tf = node.IsCurrent ? CurrentTypeface : DefaultTypeface;
                    var brush = node.IsCurrent ? textBrush : textFaintBrush;
                    var ft = new FormattedText(node.Label, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tf, 11.5, brush, dpi);
                    dc.DrawText(ft, new Point(cx - 4.0, 18.0));
                }

                // Date
                if (!string.IsNullOrEmpty(node.Date))
                {
                    var ftDate = new FormattedText(node.Date, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, DefaultTypeface, 10.5, textFaintBrush, dpi);
                    dc.DrawText(ftDate, new Point(cx - 4.0, 31.0));
                }
            }
        }
    }
}
