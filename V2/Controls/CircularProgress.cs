using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// Premium animated circular progress with gold styling and glow effects.
    /// </summary>
    public class CircularProgress : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularProgress),
                new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircularProgress),
                new PropertyMetadata(8.0, OnPropertyChangedRefresh));

        public static readonly DependencyProperty ProgressColorProperty =
            DependencyProperty.Register(nameof(ProgressColor), typeof(Brush), typeof(CircularProgress),
                new PropertyMetadata(null, OnPropertyChangedRefresh));

        public static readonly DependencyProperty TrackColorProperty =
            DependencyProperty.Register(nameof(TrackColor), typeof(Brush), typeof(CircularProgress),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(26, 26, 34)), OnPropertyChangedRefresh));

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
        private double _animatedValue = 0;

        // Gold gradient colors
        private static readonly Color GoldDark = Color.FromRgb(184, 134, 11);
        private static readonly Color GoldPrimary = Color.FromRgb(212, 175, 55);
        private static readonly Color GoldLight = Color.FromRgb(255, 215, 0);

        public CircularProgress()
        {
            InitializeVisual();
        }

        private void InitializeVisual()
        {
            _container = new Grid();

            // Track (background circle)
            _trackPath = new Path
            {
                StrokeThickness = StrokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };

            // Progress arc with gradient
            _progressPath = new Path
            {
                StrokeThickness = StrokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
            };

            _container.Children.Add(_trackPath);
            _container.Children.Add(_progressPath);
            Content = _container;

            SizeChanged += OnSizeChanged;
            Loaded += (s, e) => AnimateToValue(Value);
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisual();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp)
            {
                cp.AnimateToValue((double)e.NewValue);
            }
        }

        private static void OnPropertyChangedRefresh(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgress cp)
            {
                cp.UpdateVisual();
            }
        }

        private void AnimateToValue(double targetValue)
        {
            var animation = new DoubleAnimation
            {
                From = _animatedValue,
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            animation.CurrentTimeInvalidated += (s, e) =>
            {
                if (s is AnimationClock clock && clock.CurrentProgress.HasValue)
                {
                    _animatedValue = _animatedValue + (targetValue - _animatedValue) * clock.CurrentProgress.Value;
                    UpdateVisual();
                }
            };

            animation.Completed += (s, e) =>
            {
                _animatedValue = targetValue;
                UpdateVisual();
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(animation);
            Storyboard.SetTarget(animation, this);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.Opacity)"));
            
            // Manual animation since we're updating visual
            _animatedValue = targetValue;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;

            var size = Math.Min(ActualWidth, ActualHeight);
            var radius = (size - StrokeThickness) / 2;
            var center = new Point(size / 2, size / 2);

            // Update track
            _trackPath.Stroke = TrackColor;
            _trackPath.StrokeThickness = StrokeThickness;
            _trackPath.Data = new EllipseGeometry(center, radius, radius);

            // Update progress arc
            var clampedValue = Math.Max(0, Math.Min(100, Value));
            if (clampedValue > 0)
            {
                var angle = clampedValue / 100.0 * 360.0;
                var startAngle = -90.0;
                var endAngle = startAngle + angle;

                // Use gold gradient if no custom color
                if (ProgressColor != null)
                {
                    _progressPath.Stroke = ProgressColor;
                }
                else
                {
                    _progressPath.Stroke = new LinearGradientBrush(
                        GoldDark, GoldLight, 
                        new Point(0, 0), new Point(1, 1));
                }
                
                _progressPath.StrokeThickness = StrokeThickness;
                _progressPath.Data = CreateArc(center, radius, startAngle, endAngle);
                _progressPath.Visibility = Visibility.Visible;

                // Gold glow effect
                _progressPath.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Color = GoldPrimary,
                    Opacity = 0.6
                };
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
