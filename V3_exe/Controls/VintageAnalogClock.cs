using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MyTodo.Controls
{
    /// <summary>
    /// Vintage analog clock control for Pomodoro timer
    /// </summary>
    public class VintageAnalogClock : Canvas
    {
        private Line? _minuteHand;
        private Line? _secondHand;

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register("Minutes", typeof(int), typeof(VintageAnalogClock),
                new PropertyMetadata(25, OnTimeChanged));

        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register("Seconds", typeof(int), typeof(VintageAnalogClock),
                new PropertyMetadata(0, OnTimeChanged));

        public int Minutes
        {
            get => (int)GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        }

        public int Seconds
        {
            get => (int)GetValue(SecondsProperty);
            set => SetValue(SecondsProperty, value);
        }

        public VintageAnalogClock()
        {
            Width = 300;
            Height = 300;
            Background = Brushes.Transparent;
            Loaded += (s, e) => CreateClock();
        }

        private static void OnTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VintageAnalogClock clock)
            {
                clock.UpdateClock();
            }
        }

        private void CreateClock()
        {
            Children.Clear();

            // OUTER BRASS FRAME
            var outerFrame = new Ellipse
            {
                Width = 300,
                Height = 300,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(255, 215, 0), 0.0),    // Gold
                        new GradientStop(Color.FromRgb(218, 165, 32), 0.3),   // Goldenrod
                        new GradientStop(Color.FromRgb(184, 134, 11), 0.7),   // Dark goldenrod
                        new GradientStop(Color.FromRgb(139, 101, 8), 1.0)     // Darker
                    }
                },
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };
            Children.Add(outerFrame);

            // MIDDLE RING
            var middleRing = new Ellipse
            {
                Width = 280,
                Height = 280,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(139, 101, 8), 0.0),
                        new GradientStop(Color.FromRgb(184, 134, 11), 0.5),
                        new GradientStop(Color.FromRgb(139, 101, 8), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(100, 75, 5)),
                StrokeThickness = 1
            };
            Canvas.SetLeft(middleRing, 10);
            Canvas.SetTop(middleRing, 10);
            Children.Add(middleRing);

            // CLOCK FACE
            var clockFace = new Ellipse
            {
                Width = 250,
                Height = 250,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(255, 250, 230), 0.0),  // Cream center
                        new GradientStop(Color.FromRgb(245, 240, 220), 0.5),
                        new GradientStop(Color.FromRgb(210, 195, 165), 1.0)   // Aged edges
                    }
                },
                Stroke = new SolidColorBrush(Color.FromRgb(139, 101, 8)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(clockFace, 25);
            Canvas.SetTop(clockFace, 25);
            Children.Add(clockFace);

            // HOUR MARKERS & ROMAN NUMERALS
            for (int i = 0; i < 12; i++)
            {
                double angle = i * 30 - 90; // Start from 12 o'clock
                double angleRad = angle * Math.PI / 180;

                // Hour markers
                double markerRadius = 115;
                double markerLength = (i % 3 == 0) ? 15 : 10;
                double markerThickness = (i % 3 == 0) ? 3 : 2;

                var marker = new Line
                {
                    X1 = 150 + markerRadius * Math.Cos(angleRad),
                    Y1 = 150 + markerRadius * Math.Sin(angleRad),
                    X2 = 150 + (markerRadius - markerLength) * Math.Cos(angleRad),
                    Y2 = 150 + (markerRadius - markerLength) * Math.Sin(angleRad),
                    Stroke = new SolidColorBrush(Color.FromRgb(139, 101, 8)),
                    StrokeThickness = markerThickness
                };
                Children.Add(marker);

                // Roman numerals
                string[] romanNumerals = { "XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI" };
                double textRadius = 90;
                var text = new TextBlock
                {
                    Text = romanNumerals[i],
                    FontFamily = new FontFamily("Palatino Linotype"),
                    FontSize = (i % 3 == 0) ? 18 : 14,
                    FontWeight = (i % 3 == 0) ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(Color.FromRgb(80, 60, 20)),
                    TextAlignment = TextAlignment.Center
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, 150 + textRadius * Math.Cos(angleRad) - text.DesiredSize.Width / 2);
                Canvas.SetTop(text, 150 + textRadius * Math.Sin(angleRad) - text.DesiredSize.Height / 2);
                Children.Add(text);
            }

            // POMODORO BRANDING
            var brandText = new TextBlock
            {
                Text = "POMODORO",
                FontFamily = new FontFamily("Copperplate Gothic Bold"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 101, 8)),
                TextAlignment = TextAlignment.Center
            };
            brandText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(brandText, 150 - brandText.DesiredSize.Width / 2);
            Canvas.SetTop(brandText, 180);
            Children.Add(brandText);

            // MINUTE HAND (gold)
            _minuteHand = new Line
            {
                X1 = 150,
                Y1 = 150,
                X2 = 150,
                Y2 = 50,
                Stroke = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(255, 215, 0), 0.0),
                        new GradientStop(Color.FromRgb(218, 165, 32), 1.0)
                    }
                },
                StrokeThickness = 7,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Triangle
            };
            Children.Add(_minuteHand);

            // SECOND HAND (red)
            _secondHand = new Line
            {
                X1 = 150,
                Y1 = 150,
                X2 = 150,
                Y2 = 40,
                Stroke = new SolidColorBrush(Color.FromRgb(180, 30, 30)),
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            Children.Add(_secondHand);

            // CENTER BOSS (3 layers)
            var centerBoss1 = new Ellipse
            {
                Width = 24,
                Height = 24,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(255, 215, 0), 0.0),
                        new GradientStop(Color.FromRgb(184, 134, 11), 1.0)
                    }
                }
            };
            Canvas.SetLeft(centerBoss1, 138);
            Canvas.SetTop(centerBoss1, 138);
            Children.Add(centerBoss1);

            var centerBoss2 = new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromRgb(218, 165, 32), 0.0),
                        new GradientStop(Color.FromRgb(139, 101, 8), 1.0)
                    }
                }
            };
            Canvas.SetLeft(centerBoss2, 142);
            Canvas.SetTop(centerBoss2, 142);
            Children.Add(centerBoss2);

            var centerBoss3 = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = Brushes.Black
            };
            Canvas.SetLeft(centerBoss3, 146);
            Canvas.SetTop(centerBoss3, 146);
            Children.Add(centerBoss3);

            UpdateClock();
        }

        private void UpdateClock()
        {
            if (_minuteHand == null || _secondHand == null) return;

            // Calculate angles
            double minuteAngle = (Minutes * 6) - 90;  // 6 degrees per minute
            double secondAngle = (Seconds * 6) - 90;  // 6 degrees per second

            // Rotate minute hand
            double minuteAngleRad = minuteAngle * Math.PI / 180;
            _minuteHand.X2 = 150 + 100 * Math.Cos(minuteAngleRad);
            _minuteHand.Y2 = 150 + 100 * Math.Sin(minuteAngleRad);

            // Rotate second hand
            double secondAngleRad = secondAngle * Math.PI / 180;
            _secondHand.X2 = 150 + 110 * Math.Cos(secondAngleRad);
            _secondHand.Y2 = 150 - 110 * Math.Sin(secondAngleRad);
        }
    }
}
