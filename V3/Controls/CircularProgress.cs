using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// High-performance animated circular progress with cyber indigo/cyan gradient styling.
    /// </summary>
    public class CircularProgress : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularProgress),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgress),
                new PropertyMetadata(6.0, OnPropertyChangedRefresh));

        public static readonly DependencyProperty ProgressColorProperty =
            DependencyProperty.Register(nameof(ProgressColor), typeof(Brush), typeof(CircularProgress),
                new PropertyMetadata(null, OnPropertyChangedRefresh));

        public static readonly DependencyProperty TrackColorProperty =
            DependencyProperty.Register(nameof(TrackColor), typeof(Brush), typeof(CircularProgress),
                new PropertyMetadata(null, OnPropertyChangedRefresh));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Brush ProgressColor
        {
            get => (Brush)GetValue(ProgressColorProperty);
            set => SetValue(ProgressColorProperty, value);
        }

        public Brush TrackColor
        {
            get => (Brush)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }

        #endregion

        private Path _progressPath = null!;
        private Path _trackPath = null!;
        private Grid _container = null!;

        // Monochrome Frosted White / Platinum gradient colors (No purple/blue)
        private static readonly Color WhiteColor = Color.FromArgb(255, 255, 255, 255);
        private static readonly Color PlatinumColor = Color.FromArgb(180, 255, 255, 255);
        private static readonly SolidColorBrush DefaultTrackBrush;
        private static readonly LinearGradientBrush DefaultGradientBrush;

        static CircularProgress()
        {
            DefaultTrackBrush = new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)); // Translucent white track
            DefaultTrackBrush.Freeze();

            DefaultGradientBrush = new LinearGradientBrush(
                WhiteColor, PlatinumColor,
                new Point(0, 0), new Point(1, 1));
            DefaultGradientBrush.Freeze();
        }

        public CircularProgress()
        {
            InitializeVisual();
        }

        private void InitializeVisual()
        {
            _container = new Grid();

            _trackPath = new Path
            {
                StrokeThickness = StrokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Stroke = DefaultTrackBrush
            };

            _progressPath = new Path
            {
                StrokeThickness = StrokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Stroke = DefaultGradientBrush
            };

            _container.Children.Add(_trackPath);
            _container.Children.Add(_progressPath);
            Content = _container;

            SizeChanged += (s, e) => UpdateVisual();
            Loaded += (s, e) => UpdateVisual();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp)
            {
                cp.UpdateVisual();
            }
        }

        private static void OnPropertyChangedRefresh(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp)
            {
                cp.UpdateVisual();
            }
        }

        private void UpdateVisual()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            var size = Math.Min(ActualWidth, ActualHeight);
            var radius = Math.Max(1, (size - StrokeThickness) / 2);
            var center = new Point(size / 2, size / 2);

            _trackPath.Stroke = TrackColor ?? DefaultTrackBrush;
            _trackPath.StrokeThickness = StrokeThickness;
            _trackPath.Data = new EllipseGeometry(center, radius, radius);

            var clampedValue = Math.Max(0, Math.Min(100, Value));
            if (clampedValue > 0)
            {
                var angle = clampedValue / 100.0 * 360.0;
                var startAngle = -90.0;
                var endAngle = startAngle + angle;

                _progressPath.Stroke = ProgressColor ?? DefaultGradientBrush;
                _progressPath.StrokeThickness = StrokeThickness;
                _progressPath.Data = CreateArc(center, radius, startAngle, endAngle);
                _progressPath.Visibility = Visibility.Visible;
            }
            else
            {
                _progressPath.Visibility = Visibility.Collapsed;
            }
        }

        private Geometry CreateArc(Point center, double radius, double startAngle, double endAngle)
        {
            var startRad = startAngle * Math.PI / 180;
            var endRad = endAngle * Math.PI / 180;

            var startPoint = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));

            var endPoint = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            var isLargeArc = Math.Abs(endAngle - startAngle) > 180;

            var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            figure.Segments.Add(new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}
