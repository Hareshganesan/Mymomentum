using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MyTodo
{
    /// <summary>
    /// Main application entry point.
    /// Handles application lifecycle events, rendering optimization, and bulletproof crash prevention.
    /// </summary>
    public partial class App : Application
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyMomentumPlacement");

        private static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyMomentumPlacement", "app_errors.log");

        private static readonly string FallbackLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Catch and gracefully handle all UI thread unhandled exceptions to prevent crashing
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Catch AppDomain background thread exceptions
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Catch async Task unobserved exceptions
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("DispatcherUnhandledException", e.Exception);
            MessageBox.Show($"Startup Error: {e.Exception.Message}\n\nStack:\n{e.Exception.StackTrace}\n\nInner: {e.Exception.InnerException?.Message}", "Momentum Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("AppDomain_UnhandledException", ex);
                MessageBox.Show($"Fatal Error: {ex.Message}\n\nStack:\n{ex.StackTrace}\n\nInner: {ex.InnerException?.Message}", "Momentum Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("TaskScheduler_UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (MainWindow?.DataContext is ViewModels.MainViewModel vm)
                {
                    vm.SaveData();
                }
            }
            catch { }
            base.OnExit(e);
        }

        private static void LogException(string source, Exception ex)
        {
            var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\nInner: {ex.InnerException?.Message}\n\n";
            try
            {
                File.AppendAllText(FallbackLogPath, message);
            }
            catch { }

            try
            {
                var dir = Path.GetDirectoryName(CrashLogPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(CrashLogPath, message);
            }
            catch { }
        }
    }
}
