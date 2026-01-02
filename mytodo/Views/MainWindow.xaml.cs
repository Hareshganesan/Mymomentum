using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MyTodo.ViewModels;

namespace MyTodo.Views
{
    /// <summary>
    /// Main window with custom title bar, animations, and confetti effects.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Random _random = new();
        private readonly Color[] _confettiColors = new[]
        {
            Color.FromRgb(212, 175, 55),   // Gold Primary
            Color.FromRgb(255, 215, 0),    // Gold Light
            Color.FromRgb(184, 134, 11),   // Gold Dark
            Color.FromRgb(255, 255, 255),  // White sparkle
            Color.FromRgb(74, 222, 128),   // Green success
            Color.FromRgb(212, 175, 55),   // More gold!
        };

        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to confetti trigger
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.ShowConfetti) && vm.ShowConfetti)
                    {
                        ShowConfettiAnimation();
                    }
                };
            }
        }

        #region Window Controls

        /// <summary>
        /// Allows dragging the window by clicking anywhere on it
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Minimize window with animation
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Close window - saves data via ViewModel
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region Confetti Animation

        /// <summary>
        /// Creates a fun confetti celebration effect
        /// </summary>
        private void ShowConfettiAnimation()
        {
            ConfettiCanvas.Children.Clear();

            // Create 50 confetti pieces
            for (int i = 0; i < 50; i++)
            {
                var confetti = CreateConfettiPiece();
                ConfettiCanvas.Children.Add(confetti);
                AnimateConfetti(confetti, i);
            }
        }

        /// <summary>
        /// Creates a single confetti piece (rectangle or ellipse)
        /// </summary>
        private Shape CreateConfettiPiece()
        {
            Shape shape;
            
            if (_random.Next(2) == 0)
            {
                shape = new Rectangle
                {
                    Width = _random.Next(8, 15),
                    Height = _random.Next(8, 15),
                    RadiusX = 2,
                    RadiusY = 2
                };
            }
            else
            {
                shape = new Ellipse
                {
                    Width = _random.Next(8, 12),
                    Height = _random.Next(8, 12)
                };
            }

            shape.Fill = new SolidColorBrush(_confettiColors[_random.Next(_confettiColors.Length)]);
            shape.Opacity = 0.9;
            shape.RenderTransformOrigin = new Point(0.5, 0.5);
            shape.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new RotateTransform(_random.Next(360)),
                    new TranslateTransform()
                }
            };

            return shape;
        }

        /// <summary>
        /// Animates a confetti piece falling with rotation
        /// </summary>
        private void AnimateConfetti(Shape confetti, int index)
        {
            var startX = _random.Next(0, (int)ActualWidth);
            var startY = -50;
            var endY = ActualHeight + 50;
            var horizontalDrift = _random.Next(-100, 100);
            var duration = TimeSpan.FromSeconds(_random.NextDouble() * 1.5 + 1.5);
            var delay = TimeSpan.FromMilliseconds(index * 30);

            // Set initial position
            System.Windows.Controls.Canvas.SetLeft(confetti, startX);
            System.Windows.Controls.Canvas.SetTop(confetti, startY);

            var storyboard = new Storyboard();

            // Vertical drop animation
            var dropAnimation = new DoubleAnimation
            {
                From = startY,
                To = endY,
                Duration = duration,
                BeginTime = delay,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(dropAnimation, confetti);
            Storyboard.SetTargetProperty(dropAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(dropAnimation);

            // Horizontal drift animation
            var driftAnimation = new DoubleAnimation
            {
                From = startX,
                To = startX + horizontalDrift,
                Duration = duration,
                BeginTime = delay,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(driftAnimation, confetti);
            Storyboard.SetTargetProperty(driftAnimation, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(driftAnimation);

            // Rotation animation
            var rotateAnimation = new DoubleAnimation
            {
                By = _random.Next(360, 720) * (_random.Next(2) == 0 ? 1 : -1),
                Duration = duration,
                BeginTime = delay
            };
            Storyboard.SetTarget(rotateAnimation, confetti);
            Storyboard.SetTargetProperty(rotateAnimation, 
                new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)"));
            storyboard.Children.Add(rotateAnimation);

            // Fade out at end
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3),
                BeginTime = delay + duration - TimeSpan.FromSeconds(0.3)
            };
            Storyboard.SetTarget(fadeAnimation, confetti);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath("Opacity"));
            storyboard.Children.Add(fadeAnimation);

            storyboard.Begin();
        }

        #endregion
    }
}
