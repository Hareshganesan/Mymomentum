using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Momentum.Controls
{
    /// <summary>
    /// Native window integration: dark/light immersive mode for system UI and Windows 11 rounded corners.
    /// Surfaces are opaque (Quiet Graphite), so no acrylic/backdrop effects are applied.
    /// </summary>
    public static class Backdrop
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwcpRound = 2;

        [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static void Apply(Window window, bool dark)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            try
            {
                int darkMode = dark ? 1 : 0;
                DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));
                int corner = DwmwcpRound;
                DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref corner, sizeof(int));
            }
            catch (Exception ex)
            {
                Services.StorageService.Log($"Window integration failed: {ex.Message}");
            }
        }
    }
}
