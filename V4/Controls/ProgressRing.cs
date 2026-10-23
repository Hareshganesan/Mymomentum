using System.Windows;
using System.Windows.Media;

namespace Momentum.Controls
{
    /// <summary>A thin circular progress arc. Value is 0..1.</summary>
    public sealed class ProgressRing : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(ProgressRing), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
            nameof(Thickness), typeof(double), typeof(ProgressRing), new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
            nameof(TrackBrush), typeof(Brush), typeof(ProgressRing), new FrameworkPropertyMetadata(Brushes.Gray, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
            nameof(FillBrush), typeof(Brush), typeof(ProgressRing), new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public double Thickness { get => (double)GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
        public Brush TrackBrush { get => (Brush)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }
        public Brush FillBrush { get => (Brush)GetValue(FillBrushProperty); set => SetValue(FillBrushProperty, value); }

        protected override void OnRender(DrawingContext dc)
        {
            var size = Math.Min(ActualWidth, ActualHeight);
            if (size <= Thickness * 2) return;
            var center = new Point(ActualWidth / 2, ActualHeight / 2);
            var radius = (size - Thickness) / 2;

            dc.DrawEllipse(null, new Pen(TrackBrush, Thickness), center, radius, radius);

            var v = Math.Clamp(Value, 0, 1);
            if (v <= 0) return;
            var pen = new Pen(FillBrush, Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            if (v >= 0.9999)
            {
                dc.DrawEllipse(null, pen, center, radius, radius);
                return;
            }

            var angle = v * 360.0;
            var start = PointOn(center, radius, 0);
            var end = PointOn(center, radius, angle);
            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(start, false, false);
                ctx.ArcTo(end, new Size(radius, radius), 0, angle > 180, SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }

        private static Point PointOn(Point c, double r, double degrees)
        {
            var rad = (degrees - 90) * Math.PI / 180;
            return new Point(c.X + r * Math.Cos(rad), c.Y + r * Math.Sin(rad));
        }
    }
}
