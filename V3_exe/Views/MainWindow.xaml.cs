using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MyTodo.ViewModels;

namespace MyTodo.Views
{
    /// <summary>
    /// Main window with custom title bar, animations, and confetti effects.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Win32 API for blur effect
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public int AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private const int WCA_ACCENT_POLICY = 19;
        private const int ACCENT_ENABLE_BLURBEHIND = 3;

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

        private WindowState _previousWindowState;
        private DispatcherTimer? _particleTimer;
        private readonly List<Ellipse> _particles = new();

        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to ViewModel events
            if (DataContext is MainViewModel vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(MainViewModel.ShowConfetti) && vm.ShowConfetti)
                    {
                        ShowConfettiAnimation();
                    }
                    else if (e.PropertyName == nameof(MainViewModel.IsFullScreen))
                    {
                        ToggleFullScreen(vm.IsFullScreen);
                    }
                    else if (e.PropertyName == nameof(MainViewModel.IsDarkMode))
                    {
                        ApplyTheme(vm.IsDarkMode);
                    }
                };
            }

            // Start animated background
            Loaded += (s, e) =>
            {
                StartBackgroundAnimation();
                EnableBlur();
            };
        }

        #region Blur Effect

        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy
            {
                AccentState = ACCENT_ENABLE_BLURBEHIND,
                GradientColor = unchecked((int)0x30000000) // Very transparent overlay for better visibility
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = WCA_ACCENT_POLICY,
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        private void ApplyTheme(bool isDark)
        {
            // Update MainBorder background
            var mainBorder = this.FindName("MainBorder") as System.Windows.Controls.Border;
            if (mainBorder != null)
            {
                if (isDark)
                {
                    mainBorder.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x08, 0x08, 0x10));
                }
                else
                {
                    // Light mode - soft warm white with transparency
                    mainBorder.Background = new SolidColorBrush(Color.FromArgb(0x70, 0xFD, 0xFB, 0xF7));
                }
            }

            // Update application resource brushes dynamically
            try
            {
                var resources = Application.Current.Resources;
                
                if (isDark)
                {
                    // Dark Mode Colors
                    UpdateBrush(resources, "BackgroundBrush", Color.FromRgb(0x0A, 0x0A, 0x0F));
                    UpdateBrush(resources, "SurfaceBrush", Color.FromRgb(0x12, 0x12, 0x18));
                    UpdateBrush(resources, "CardBrush", Color.FromRgb(0x1A, 0x1A, 0x22));
                    UpdateBrush(resources, "CardHoverBrush", Color.FromRgb(0x22, 0x22, 0x30));
                    UpdateBrush(resources, "BorderBrush", Color.FromRgb(0x2A, 0x2A, 0x35));
                    UpdateBrush(resources, "TextPrimaryBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                    UpdateBrush(resources, "TextSecondaryBrush", Color.FromRgb(0xB0, 0xB0, 0xB8));
                    UpdateBrush(resources, "TextMutedBrush", Color.FromRgb(0x60, 0x60, 0x68));
                }
                else
                {
                    // Light Mode Colors  
                    UpdateBrush(resources, "BackgroundBrush", Color.FromRgb(0xFD, 0xFB, 0xF7)); // Warm off-white
                    UpdateBrush(resources, "SurfaceBrush", Color.FromRgb(0xFA, 0xF8, 0xF4));     // Cream surface
                    UpdateBrush(resources, "CardBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));        // Pure white
                    UpdateBrush(resources, "CardHoverBrush", Color.FromRgb(0xF5, 0xF3, 0xEF));   // Subtle hover
                    UpdateBrush(resources, "BorderBrush", Color.FromRgb(0xE8, 0xE5, 0xE0));      // Soft border
                    UpdateBrush(resources, "TextPrimaryBrush", Color.FromRgb(0x1A, 0x1A, 0x1A)); // Almost black
                    UpdateBrush(resources, "TextSecondaryBrush", Color.FromRgb(0x5A, 0x5A, 0x5A)); // Medium gray
                    UpdateBrush(resources, "TextMutedBrush", Color.FromRgb(0x9A, 0x9A, 0x9A));   // Light gray
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        private void UpdateBrush(ResourceDictionary resources, string key, Color color)
        {
            if (resources.Contains(key) && resources[key] is SolidColorBrush brush)
            {
                // Animate color transition for smooth theme change
                var colorAnimation = new ColorAnimation
                {
                    To = color,
                    Duration = TimeSpan.FromMilliseconds(300),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
            }
        }

        #endregion

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
        /// Maximize/Restore window
        /// </summary>
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
            else
                WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// Close window - saves data via ViewModel
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Toggle full screen mode
        /// </summary>
        private void ToggleFullScreen(bool fullScreen)
        {
            if (fullScreen)
            {
                _previousWindowState = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                WindowState = WindowState.Maximized;
            }
            else
            {
                Topmost = false;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.CanResize;
                WindowState = _previousWindowState;
            }
        }

        #endregion

        #region Animated Background

        /// <summary>
        /// Starts the animated particle background
        /// </summary>
        private void StartBackgroundAnimation()
        {
            // Create initial particles
            CreateBackgroundParticles();

            // Timer to animate particles
            _particleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _particleTimer.Tick += AnimateParticles;
            _particleTimer.Start();
        }

        /// <summary>
        /// Creates floating gold particles for ambient effect
        /// </summary>
        private void CreateBackgroundParticles()
        {
            if (ParticleCanvas == null) return;

            ParticleCanvas.Children.Clear();
            _particles.Clear();

            for (int i = 0; i < 15; i++)
            {
                var particle = new Ellipse
                {
                    Width = _random.Next(4, 12),
                    Height = _random.Next(4, 12),
                    Opacity = _random.NextDouble() * 0.15 + 0.05,
                    Tag = new ParticleData
                    {
                        SpeedX = (_random.NextDouble() - 0.5) * 0.8,
                        SpeedY = (_random.NextDouble() - 0.5) * 0.5,
                        X = _random.NextDouble() * ActualWidth,
                        Y = _random.NextDouble() * ActualHeight
                    }
                };

                particle.Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop(Color.FromArgb(180, 212, 175, 55), 0),
                        new GradientStop(Color.FromArgb(0, 212, 175, 55), 1)
                    }
                };

                var data = (ParticleData)particle.Tag;
                System.Windows.Controls.Canvas.SetLeft(particle, data.X);
                System.Windows.Controls.Canvas.SetTop(particle, data.Y);

                ParticleCanvas.Children.Add(particle);
                _particles.Add(particle);
            }
        }

        /// <summary>
        /// Animates particles floating around
        /// </summary>
        private void AnimateParticles(object? sender, EventArgs e)
        {
            if (ParticleCanvas == null || ActualWidth == 0) return;

            foreach (var particle in _particles)
            {
                if (particle.Tag is ParticleData data)
                {
                    // Update position
                    data.X += data.SpeedX;
                    data.Y += data.SpeedY;

                    // Wrap around edges
                    if (data.X < -20) data.X = ActualWidth + 10;
                    if (data.X > ActualWidth + 20) data.X = -10;
                    if (data.Y < -20) data.Y = ActualHeight + 10;
                    if (data.Y > ActualHeight + 20) data.Y = -10;

                    System.Windows.Controls.Canvas.SetLeft(particle, data.X);
                    System.Windows.Controls.Canvas.SetTop(particle, data.Y);
                }
            }
        }

        private class ParticleData
        {
            public double SpeedX { get; set; }
            public double SpeedY { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
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
