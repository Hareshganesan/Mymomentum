using System.Diagnostics;
using Momentum.Services;

namespace Momentum.Core
{
    public static class Links
    {
        public static void Open(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) url = "https://" + url;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                StorageService.Log($"Could not open {url}: {ex.Message}");
            }
        }
    }
}
