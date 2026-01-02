using System.Windows;
using MyTodo.Services;

namespace MyTodo
{
    /// <summary>
    /// Main application entry point.
    /// Handles application lifecycle events including save/load.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
