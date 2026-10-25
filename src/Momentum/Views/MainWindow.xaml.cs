using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Momentum.Services;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class MainWindow : Window
    {
        public static bool IsCaptureMode { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            // Fit smaller / high-DPI screens (e.g. 1080p at 125%).
            var work = SystemParameters.WorkArea;
            Width = Math.Min(Width, work.Width - 40);
            Height = Math.Min(Height, work.Height - 40);
            SourceInitialized += (_, _) => Controls.Backdrop.Apply(this, AppStore.Instance.State.Profile.DarkTheme);
            StateChanged += (_, _) => UpdateMaximizedState();
            PreviewKeyDown += OnPreviewKeyDown;

            if (AccountService.TryResume()) EnterApp();
            else ShowLogin();
            Loaded += (_, _) => { if (!IsCaptureMode) PlaySplash(); };
        }

        // ---------- Launch animation ----------

        private void PlaySplash()
        {
            // Respect Windows' "Animation effects" setting.
            if (!SystemParameters.ClientAreaAnimation)
            {
                SplashLayer.Visibility = Visibility.Collapsed;
                return;
            }

            static DoubleAnimation Anim(double from, double to, double startSec, double durSec, IEasingFunction? ease = null) =>
                new(from, to, TimeSpan.FromSeconds(durSec)) { BeginTime = TimeSpan.FromSeconds(startSec), EasingFunction = ease ?? new CubicEase { EasingMode = EasingMode.EaseInOut }, FillBehavior = FillBehavior.HoldEnd };

            // Keep the app hidden behind the splash, then fade it in as the splash leaves.
            var behind = Root.Children.OfType<UIElement>().Where(c => c != SplashLayer).ToList();
            foreach (var c in behind) c.Opacity = 0;

            BuildGreetings();
            GreetingCanvas.BeginAnimation(OpacityProperty, Anim(0, 1, 0.1, 1.0));

            // Write "momentum" like a pen: dash length = the stroke's own length, then slide the offset to zero.
            void Write(Path path, double start, double duration)
            {
                double len = PathLength(path.Data) / path.StrokeThickness;
                path.StrokeDashArray = new DoubleCollection { len, len };
                path.StrokeDashOffset = len;
                path.BeginAnimation(Shape.StrokeDashOffsetProperty, Anim(len, 0, start, duration, new SineEase { EasingMode = EasingMode.EaseInOut }));
            }
            Write(SplashScript, 0.3, 2.2);
            Write(SplashCross, 2.45, 0.25);

            var fade = Anim(1, 0, 3.4, 0.4);
            fade.Completed += (_, _) => SplashLayer.Visibility = Visibility.Collapsed;
            SplashLayer.BeginAnimation(OpacityProperty, fade);
            foreach (var c in behind) c.BeginAnimation(OpacityProperty, Anim(0, 1, 3.45, 0.45));
        }

        private static readonly string[] Greetings =
        {
            "salut", "olá", "hola", "hej", "ciao", "ahoj", "hallo", "bonjour", "привет", "வணக்கம்", "नमस्ते", "こんにちは",
            "안녕하세요", "merhaba", "hei", "xin chào", "γεια σας", "szia", "mabuhay", "habari", "hallå", "namaskar", "aloha", "salve",
        };

        /// <summary>Faint rows of greetings behind the wordmark, kept clear of the middle.</summary>
        private void BuildGreetings()
        {
            GreetingCanvas.Children.Clear();
            double w = Math.Max(ActualWidth, 1200), h = Math.Max(ActualHeight, 760);
            var font = new FontFamily("Segoe Script, Ink Free, Segoe UI");
            var brush = (Brush)FindResource("Border");
            int i = 0;
            for (int row = 0; row < 6; row++)
            {
                double y = 30 + row * (h - 60) / 5.5;
                double x = row % 2 == 0 ? -40 : 60;
                while (x < w)
                {
                    var tb = new TextBlock { Text = Greetings[i++ % Greetings.Length], FontFamily = font, FontSize = 40, Foreground = brush };
                    tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    bool nearCentre = Math.Abs(y + 30 - h / 2) < 150 && Math.Abs(x + tb.DesiredSize.Width / 2 - w / 2) < 380;
                    if (!nearCentre)
                    {
                        Canvas.SetLeft(tb, x);
                        Canvas.SetTop(tb, y);
                        GreetingCanvas.Children.Add(tb);
                    }
                    x += tb.DesiredSize.Width + 70;
                }
            }
        }

        private static double PathLength(Geometry geometry)
        {
            double total = 0;
            foreach (var figure in geometry.GetFlattenedPathGeometry().Figures)
            {
                var last = figure.StartPoint;
                foreach (var seg in figure.Segments)
                {
                    IEnumerable<Point> pts = seg switch
                    {
                        PolyLineSegment pl => pl.Points,
                        LineSegment l => new[] { l.Point },
                        _ => Array.Empty<Point>(),
                    };
                    foreach (var pt in pts) { total += (pt - last).Length; last = pt; }
                }
            }
            return total;
        }

        private void Splash_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var fade = new DoubleAnimation(SplashLayer.Opacity, 0, TimeSpan.FromSeconds(0.15));
            fade.Completed += (_, _) => SplashLayer.Visibility = Visibility.Collapsed;
            SplashLayer.BeginAnimation(OpacityProperty, fade);
            foreach (var c in Root.Children.OfType<UIElement>().Where(c => c != SplashLayer))
                c.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromSeconds(0.2)));
        }

        // ---------- Accounts ----------

        private void ShowLogin()
        {
            var login = new LoginViewModel();
            login.SignedIn += EnterApp;
            Login.DataContext = login;
            DataContext = null;
            SetShellVisible(false);
            LoginLayer.Visibility = Visibility.Visible;
        }

        private readonly Dictionary<Type, FrameworkElement> _viewCache = new();

        private void HookNavigation(ShellViewModel shell)
        {
            shell.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ShellViewModel.CurrentPage))
                {
                    ShowPage(shell.CurrentPage);
                }
            };
            ShowPage(shell.CurrentPage);
        }

        private void ShowPage(PageViewModel? page)
        {
            if (page == null) return;
            var pageType = page.GetType();

            foreach (UIElement child in PageHost.Children)
                child.Visibility = Visibility.Collapsed;

            if (!_viewCache.TryGetValue(pageType, out var view))
            {
                var template = Application.Current.TryFindResource(new DataTemplateKey(pageType)) as DataTemplate;
                if (template != null)
                {
                    view = template.LoadContent() as FrameworkElement;
                }
                if (view == null)
                {
                    var viewTypeName = pageType.FullName?.Replace("ViewModels", "Views").Replace("ViewModel", "View");
                    var vt = pageType.Assembly.GetType(viewTypeName ?? string.Empty);
                    if (vt != null) view = Activator.CreateInstance(vt) as FrameworkElement;
                }

                if (view != null)
                {
                    _viewCache[pageType] = view;
                    PageHost.Children.Add(view);
                }
            }

            if (view != null)
            {
                view.DataContext = page;
                view.Visibility = Visibility.Visible;
            }
        }

        public void EnterForCapture()
        {
            IsCaptureMode = true;
            SplashLayer.Visibility = Visibility.Collapsed;
            LoginLayer.Visibility = Visibility.Collapsed;
            EnterApp();
        }

        private async void EnterApp()
        {
            var store = AppStore.Instance;
            store.Load();
            App.ApplyTheme(store.State.Profile.DarkTheme);
            var shell = new ShellViewModel();
            shell.SignOutRequested += SignOut;
            DataContext = shell;
            HookNavigation(shell);
            LoginLayer.Visibility = Visibility.Collapsed;
            SetShellVisible(true);
            Login.DataContext = null;
            await shell.SyncLeetCodeAsync(quiet: true);
            await shell.SyncMailAsync(quiet: true);
        }

        /// <summary>Hides the app chrome while signed out (bound elements keep their own visibility).</summary>
        private void SetShellVisible(bool visible)
        {
            foreach (UIElement child in Root.Children)
                if (child != LoginLayer && child != SplashLayer && !System.Windows.Data.BindingOperations.IsDataBound(child, VisibilityProperty))
                    child.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SignOut()
        {
            if (DataContext is ShellViewModel shell) shell.Detach();
            _viewCache.Clear();
            PageHost.Children.Clear();
            AppStore.Instance.Unload();
            AccountService.SignOut();
            ShowLogin();
        }

        // ---------- Window ----------

        private void UpdateMaximizedState()
        {
            // A chromeless maximized window overhangs the screen by the resize border; pad it back in.
            Root.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
            MaxButton.Content = WindowState == WindowState.Maximized ? "" : "";
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is ShellViewModel { HasConfirm: true } dlg && (e.Key is Key.Escape or Key.Enter))
            {
                dlg.Answer(e.Key == Key.Enter);
                e.Handled = true;
                return;
            }
            if (Keyboard.Modifiers != ModifierKeys.Control) return;
            int index = e.Key switch
            {
                >= Key.D1 and <= Key.D9 => e.Key - Key.D1,
                >= Key.NumPad1 and <= Key.NumPad9 => e.Key - Key.NumPad1,
                _ => -1,
            };
            if (index >= 0 && DataContext is ShellViewModel shell && index < shell.Nav.Count)
            {
                shell.Navigate(shell.Nav[index].Key);
                e.Handled = true;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
