using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MyTodo.ViewModels;

namespace MyTodo.Views
{
    /// <summary>
    /// Main window with custom title bar and celebration confetti effects.
    /// Optimized for 60+ FPS hardware accelerated rendering.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Random _random = new();
        private readonly Color[] _confettiColors = new[]
        {
            Color.FromRgb(255, 255, 255),   // Pure Crisp White
            Color.FromRgb(226, 232, 240),   // Silver Smoke
            Color.FromRgb(203, 213, 225),   // Platinum
            Color.FromRgb(241, 245, 249),   // Diamond White
            Color.FromRgb(148, 163, 184),   // Slate Silver
            Color.FromRgb(255, 255, 255)    // Pure White
        };

        private WindowState _previousWindowState;

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
                };
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            ApplyWindowsAcrylicFrostedGlass();
        }

        #region Windows 11 DWM Frosted Acrylic Glass

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        private void ApplyWindowsAcrylicFrostedGlass()
        {
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                if (handle == IntPtr.Zero) return;

                // Extend DWM frame into client area (-1 = entire window)
                var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(handle, ref margins);

                // Make WPF HwndSource CompositionTarget transparent so DWM glass shines through
                var source = HwndSource.FromHwnd(handle);
                if (source?.CompositionTarget != null)
                {
                    source.CompositionTarget.BackgroundColor = Colors.Transparent;
                }

                // Enable Windows 11 Immersive Dark Mode
                int darkMode = 1;
                DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                // Enable Windows 11 Native Acrylic Frosted Glass (DWMSBT_TRANSIENTWINDOW = 3)
                int backdropType = 3; 
                int hr = DwmSetWindowAttribute(handle, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

                // Native Windows 11 Rounded Corners (DWMWCP_ROUND = 2)
                int cornerPreference = 2;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                // Fallback for Windows 10
                if (hr != 0)
                {
                    EnableWindows10Acrylic(handle);
                }
            }
            catch { }
        }

        private void EnableWindows10Acrylic(IntPtr handle)
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 2,
                    GradientColor = unchecked((int)0x990A0F1D)
                };

                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPtr,
                    SizeOfData = accentStructSize
                };

                SetWindowCompositionAttribute(handle, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch { }
        }

        #endregion

        #region Window Controls

        /// <summary>
        /// Allows dragging the window by clicking anywhere on background/title
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    DragMove();
                }
                catch { }
            }
        }

        /// <summary>
        /// Minimize window
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
        /// Ensure all data is saved whenever the window closes
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveData();
            }
            base.OnClosing(e);
        }

        /// <summary>
        /// Close window
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveData();
            }
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

        #region Confetti Animation

        /// <summary>
        /// Creates a celebration confetti burst
        /// </summary>
        private void ShowConfettiAnimation()
        {
            if (ConfettiCanvas == null) return;
            ConfettiCanvas.Children.Clear();

            // 40 confetti pieces for optimal rendering speed
            for (int i = 0; i < 40; i++)
            {
                var confetti = CreateConfettiPiece();
                ConfettiCanvas.Children.Add(confetti);
                AnimateConfetti(confetti, i);
            }
        }

        private Shape CreateConfettiPiece()
        {
            Shape shape;
            if (_random.Next(2) == 0)
            {
                shape = new Rectangle
                {
                    Width = _random.Next(8, 14),
                    Height = _random.Next(8, 14),
                    RadiusX = 3,
                    RadiusY = 3
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

            var brush = new SolidColorBrush(_confettiColors[_random.Next(_confettiColors.Length)]);
            brush.Freeze();
            shape.Fill = brush;
            shape.Opacity = 0.95;
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

        private void AnimateConfetti(Shape confetti, int index)
        {
            var startX = _random.Next(0, Math.Max(100, (int)ActualWidth));
            var startY = -40;
            var endY = ActualHeight + 40;
            var horizontalDrift = _random.Next(-80, 80);
            var duration = TimeSpan.FromSeconds(_random.NextDouble() * 1.2 + 1.2);
            var delay = TimeSpan.FromMilliseconds(index * 25);

            System.Windows.Controls.Canvas.SetLeft(confetti, startX);
            System.Windows.Controls.Canvas.SetTop(confetti, startY);

            var storyboard = new Storyboard();

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

            var fadeAnimation = new DoubleAnimation
            {
                From = 0.95,
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
