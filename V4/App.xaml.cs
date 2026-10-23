using System.Windows;
using System.Windows.Threading;
using Momentum.Services;
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

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
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
