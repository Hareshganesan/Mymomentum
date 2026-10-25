using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Momentum.Services;
using Momentum.ViewModels;
using Momentum.Views;

namespace Momentum
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnUnhandled;
            TaskScheduler.UnobservedTaskException += (_, args) => { StorageService.Log(args.Exception.ToString()); args.SetObserved(); };

            if (e.Args.Length > 0 && e.Args[0] == "--capture-screenshots")
            {
                _ = RunScreenshotCaptureAsync();
                return;
            }

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }

        private async Task RunScreenshotCaptureAsync()
        {
            try
            {
                var window = new MainWindow();
                MainWindow = window;
                window.EnterForCapture();
                window.Width = 1440;
                window.Height = 900;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                window.Show();
                window.UpdateLayout();

                var shell = (ShellViewModel)window.DataContext;
                ApplyTheme(true);

                // Discover repo root
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string? repoRoot = null;
                var curr = new DirectoryInfo(baseDir);
                while (curr != null)
                {
                    if (File.Exists(Path.Combine(curr.FullName, "Momentum.sln")))
                    {
                        repoRoot = curr.FullName;
                        break;
                    }
                    curr = curr.Parent;
                }
                if (repoRoot == null) repoRoot = @"E:\MyMomentum_Stable\Mymomentum";

                // 1. ASSESSMENT LAB (Flagship V6 Feature)
                shell.Navigate("lab");
                await Task.Delay(1200);
                var labVm = (AssessmentLabViewModel)shell.CurrentPage!;
                var twoSum = labVm.AllQuestions.FirstOrDefault(q => q.Id == "two-sum") ?? labVm.AllQuestions.First();
                labVm.SelectQuestion(twoSum);
                labVm.UserCode = twoSum.ReferenceSolution;
                await labVm.RunAssessmentAsync(runAllTests: true);
                await Task.Delay(1500);
                window.UpdateLayout();

                SaveBitmap(window.Root, Path.Combine(repoRoot, "assets", "screenshots", "assessment_lab_editor.png"));
                SaveBitmap(window.Root, Path.Combine(repoRoot, "src", "Momentum", "Assets", "Screenshots", "assessment_lab_editor.png"));

                // 2. TRAINING COACH / PYTHON VISUAL TUTOR
                shell.Navigate("training");
                await Task.Delay(1200);
                var trainingVm = (TrainingViewModel)shell.CurrentPage!;
                trainingVm.SelectProblemBySlug("two-sum");
                trainingVm.ActiveMode = "TeachMe";
                await Task.Delay(400);
                trainingVm.StepNextCommand.Execute(null);
                await Task.Delay(300);
                trainingVm.StepNextCommand.Execute(null);
                await Task.Delay(1000);
                window.UpdateLayout();

                SaveBitmap(window.Root, Path.Combine(repoRoot, "assets", "screenshots", "visual_tutor.png"));
                SaveBitmap(window.Root, Path.Combine(repoRoot, "src", "Momentum", "Assets", "Screenshots", "visual_tutor.png"));

                // 3. PROBLEMS CATALOG
                shell.Navigate("problems");
                await Task.Delay(1200);
                window.UpdateLayout();

                SaveBitmap(window.Root, Path.Combine(repoRoot, "assets", "screenshots", "problems_catalog.png"));
                SaveBitmap(window.Root, Path.Combine(repoRoot, "src", "Momentum", "Assets", "Screenshots", "problems_catalog.png"));

                Shutdown(0);
            }
            catch (Exception ex)
            {
                StorageService.Log("Screenshot capture failed: " + ex);
                Shutdown(1);
            }
        }

        private static void SaveBitmap(FrameworkElement element, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            int width = (int)Math.Max(element.ActualWidth, 1440);
            int height = (int)Math.Max(element.ActualHeight, 900);

            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x0D, 0x0E, 0x11)), null, new Rect(0, 0, width, height));
                context.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, width, height));
            }

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            using var stream = File.Create(destination);
            encoder.Save(stream);
        }

        public static void ApplyTheme(bool dark)
        {
            var dictionaries = Current.Resources.MergedDictionaries;
            dictionaries[0] = new ResourceDictionary
            {
                Source = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative)
            };
            if (Current.MainWindow != null) Controls.Backdrop.Apply(Current.MainWindow, dark);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppStore.Instance.SaveNow();
            base.OnExit(e);
        }

        private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            StorageService.Log(e.Exception.ToString());
            AppStore.Instance.SaveNow();
            MessageBox.Show($"Something went wrong, but your progress is saved.\n\n{e.Exception.Message}", "Momentum",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }
    }
}
