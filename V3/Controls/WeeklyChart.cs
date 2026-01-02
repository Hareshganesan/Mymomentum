using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// Premium weekly activity chart with gold bars and interactive hover effects.
    /// </summary>
    public class WeeklyChart : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(IList<double>), typeof(WeeklyChart),
                new PropertyMetadata(null, OnDataChanged));

        public static readonly DependencyProperty BarColorProperty =
            DependencyProperty.Register(nameof(BarColor), typeof(Brush), typeof(WeeklyChart),
                new PropertyMetadata(null, OnPropertyChangedRefresh));

        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(nameof(BarWidth), typeof(double), typeof(WeeklyChart),
                new PropertyMetadata(24.0, OnPropertyChangedRefresh));

        public static readonly DependencyProperty BarCornerRadiusProperty =
            DependencyProperty.Register(nameof(BarCornerRadius), typeof(double), typeof(WeeklyChart),
                new PropertyMetadata(6.0, OnPropertyChangedRefresh));

        public IList<double> Data
        {
            get => (IList<double>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public Brush BarColor
        {
            get => (Brush)GetValue(BarColorProperty);
            set => SetValue(BarColorProperty, value);
        }

        public double BarWidth
        {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        public double BarCornerRadius
        {
            get => (double)GetValue(BarCornerRadiusProperty);
            set => SetValue(BarCornerRadiusProperty, value);
        }

        #endregion

        private Canvas _chartCanvas = null!;
        private StackPanel _labelsPanel = null!;
        private readonly string[] _dayLabels = { "M", "T", "W", "T", "F", "S", "S" };
        private readonly List<Border> _bars = new();
        private readonly List<TextBlock> _tooltips = new();

        // Gold colors
        private static readonly Color GoldDark = Color.FromRgb(184, 134, 11);
        private static readonly Color GoldPrimary = Color.FromRgb(212, 175, 55);
        private static readonly Color GoldLight = Color.FromRgb(255, 215, 0);
        private static readonly Color DarkSurface = Color.FromRgb(20, 20, 28);

        public WeeklyChart()
        {
            InitializeVisual();
        }

        private void InitializeVisual()
        {
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Chart canvas
            _chartCanvas = new Canvas
            {
                ClipToBounds = true
            };
            Grid.SetRow(_chartCanvas, 0);

            // Day labels
            _labelsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(_labelsPanel, 1);

            mainGrid.Children.Add(_chartCanvas);
            mainGrid.Children.Add(_labelsPanel);
            
            Content = mainGrid;
            SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisual(true);
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WeeklyChart chart)
            {
                chart.UpdateVisual(true);
            }
        }

        private static void OnPropertyChangedRefresh(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WeeklyChart chart)
            {
                chart.UpdateVisual(false);
            }
        }

        private void UpdateVisual(bool animate)
        {
            if (_chartCanvas == null || _chartCanvas.ActualWidth <= 0) return;

            var data = Data ?? new List<double> { 0, 0, 0, 0, 0, 0, 0 };
            while (data.Count < 7) data = data.Concat(new[] { 0.0 }).ToList();

            _chartCanvas.Children.Clear();
            _labelsPanel.Children.Clear();
            _bars.Clear();
            _tooltips.Clear();

            var maxValue = data.Any(v => v > 0) ? data.Max() : 1;
            var chartHeight = _chartCanvas.ActualHeight - 20; // Leave room for tooltips
            var chartWidth = _chartCanvas.ActualWidth;
            var barSpacing = (chartWidth - (7 * BarWidth)) / 8;

            // Gold gradient brush
            var goldGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 1),
                EndPoint = new Point(0.5, 0)
            };
            goldGradient.GradientStops.Add(new GradientStop(GoldDark, 0.0));
            goldGradient.GradientStops.Add(new GradientStop(GoldPrimary, 0.5));
            goldGradient.GradientStops.Add(new GradientStop(GoldLight, 1.0));

            // Empty bar brush (subtle dark)
            var emptyBrush = new SolidColorBrush(DarkSurface);

            for (int i = 0; i < 7; i++)
            {
                var value = Math.Max(0, data.Count > i ? data[i] : 0);
                var normalizedHeight = maxValue > 0 ? (value / maxValue) * (chartHeight - 10) : 0;
                var minHeight = 4;
                var barHeight = Math.Max(normalizedHeight, minHeight);
                var x = barSpacing + i * (BarWidth + barSpacing);
                var y = chartHeight - barHeight;

                // Create bar
                var bar = new Border
                {
                    Width = BarWidth,
                    Height = animate ? minHeight : barHeight,
                    CornerRadius = new CornerRadius(BarCornerRadius, BarCornerRadius, 2, 2),
                    Background = value > 0 ? (BarColor ?? goldGradient) : emptyBrush,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = value
                };

                // Glow effect for bars with value
                if (value > 0)
                {
                    bar.Effect = new DropShadowEffect
                    {
                        BlurRadius = 15,
                        ShadowDepth = 0,
                        Color = GoldPrimary,
                        Opacity = 0.4
                    };
                }

                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, animate ? chartHeight - minHeight : y);
                _chartCanvas.Children.Add(bar);
                _bars.Add(bar);

                // Tooltip
                var tooltip = new TextBlock
                {
                    Text = value.ToString("0"),
                    Foreground = new SolidColorBrush(GoldLight),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Visibility = Visibility.Collapsed,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 5,
                        ShadowDepth = 0,
                        Color = Colors.Black,
                        Opacity = 0.8
                    }
                };
                _tooltips.Add(tooltip);
                _chartCanvas.Children.Add(tooltip);

                // Hover effects
                int barIndex = i;
                double targetHeight = barHeight;
                double targetY = y;

                bar.MouseEnter += (s, e) =>
                {
                    if ((double)bar.Tag > 0)
                    {
                        var scaleTransform = new ScaleTransform(1, 1);
                        bar.RenderTransform = scaleTransform;
                        bar.RenderTransformOrigin = new Point(0.5, 1);

                        var hoverAnim = new DoubleAnimation(1.1, TimeSpan.FromMilliseconds(150))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, hoverAnim);

                        // Show tooltip
                        _tooltips[barIndex].Visibility = Visibility.Visible;
                        Canvas.SetLeft(_tooltips[barIndex], x + BarWidth / 2 - 8);
                        Canvas.SetTop(_tooltips[barIndex], targetY - 20);
                    }
                };

                bar.MouseLeave += (s, e) =>
                {
                    if (bar.RenderTransform is ScaleTransform st)
                    {
                        var leaveAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
                        st.BeginAnimation(ScaleTransform.ScaleXProperty, leaveAnim);
                    }
                    _tooltips[barIndex].Visibility = Visibility.Collapsed;
                };

                // Animate bar growing
                if (animate && value > 0)
                {
                    var heightAnim = new DoubleAnimation
                    {
                        From = minHeight,
                        To = barHeight,
                        Duration = TimeSpan.FromMilliseconds(500),
                        BeginTime = TimeSpan.FromMilliseconds(i * 80),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    var topAnim = new DoubleAnimation
                    {
                        From = chartHeight - minHeight,
                        To = y,
                        Duration = TimeSpan.FromMilliseconds(500),
                        BeginTime = TimeSpan.FromMilliseconds(i * 80),
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };

                    bar.BeginAnimation(HeightProperty, heightAnim);
                    bar.BeginAnimation(Canvas.TopProperty, topAnim);
                }

                // Day label
                var label = new TextBlock
                {
                    Text = _dayLabels[i],
                    Foreground = new SolidColorBrush(Color.FromRgb(128, 128, 140)),
                    FontSize = 12,
                    FontWeight = FontWeights.Medium,
                    Width = BarWidth + barSpacing,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(i == 0 ? barSpacing / 2 : 0, 0, 0, 0)
                };
                _labelsPanel.Children.Add(label);
            }
        }
    }
}
