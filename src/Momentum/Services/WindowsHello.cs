using System.Windows;
using System.Windows.Interop;
using Windows.Security.Credentials.UI;

namespace Momentum.Services
{
    /// <summary>Asks Windows to confirm the person at the PC (PIN, fingerprint or face) before sensitive changes.</summary>
    public static class WindowsHello
    {
        /// <summary>True if verified, false if refused or cancelled, null if Windows Hello isn't set up on this PC.</summary>
        public static async Task<bool?> VerifyAsync(string message)
        {
            try
            {
                if (await UserConsentVerifier.CheckAvailabilityAsync() != UserConsentVerifierAvailability.Available) return null;
                var hwnd = new WindowInteropHelper(Application.Current.MainWindow).Handle;
                var result = await UserConsentVerifierInterop.RequestVerificationForWindowAsync(hwnd, message);
                return result == UserConsentVerificationResult.Verified;
            }
            catch (Exception ex)
            {
                StorageService.Log($"Windows Hello unavailable: {ex.Message}");
                return null;
            }
        }
    }
}
