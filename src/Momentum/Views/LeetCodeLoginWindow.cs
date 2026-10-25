using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Momentum.Services;

namespace Momentum.Views
{
    /// <summary>
    /// Opens leetcode.com's own sign-in page. Once signed in, the LEETCODE_SESSION cookie and username are read
    /// from the browser view, so nothing has to be copied by hand. The password goes to LeetCode, never to Momentum.
    /// </summary>
    public sealed class LeetCodeLoginWindow : Window
    {
        private const string Script = """
            (async () => {
              try {
                const csrf = (document.cookie.match(/csrftoken=([^;]+)/) || [])[1] || '';
                const r = await fetch('/graphql/', { method: 'POST', credentials: 'include',
                  headers: { 'content-type': 'application/json', 'x-csrftoken': csrf },
                  body: JSON.stringify({ query: 'query { userStatus { username isSignedIn } }' }) });
                const j = await r.json();
                window.chrome.webview.postMessage(j.data.userStatus.isSignedIn ? (j.data.userStatus.username || '') : '');
              } catch { window.chrome.webview.postMessage(''); }
            })();
            """;

        private readonly WebView2 _web = new();
        private string? _session;
        private bool _checking;

        public string? Session { get; private set; }
        public string? Username { get; private set; }

        /// <summary>The embedded browser's profile; cleared before and after so no LeetCode login lingers on disk.</summary>
        private static string ProfileDir => Path.Combine(StorageService.DataDirectory, "webview");

        public LeetCodeLoginWindow()
        {
            Title = "Sign in to LeetCode";
            Width = 520;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SetResourceReference(BackgroundProperty, "Bg");
            Content = _web;
            Loaded += async (_, _) => await StartAsync();
            Closed += (_, _) => { _web.Dispose(); ClearProfile(); };
        }

        private async Task StartAsync()
        {
            try
            {
                ClearProfile();
                var env = await CoreWebView2Environment.CreateAsync(null, ProfileDir);
                await _web.EnsureCoreWebView2Async(env);
                _web.CoreWebView2.NavigationCompleted += async (_, _) => await CheckAsync();
                _web.CoreWebView2.WebMessageReceived += OnMessage;
                _web.CoreWebView2.Navigate("https://leetcode.com/accounts/login/");
            }
            catch (WebView2RuntimeNotFoundException)
            {
                MessageBox.Show(this, "Microsoft Edge WebView2 Runtime is needed for this. Install it from Microsoft, or paste the cookie manually in Settings.", "Momentum");
                Close();
            }
        }

        private async Task CheckAsync()
        {
            if (_checking || _web.CoreWebView2 == null) return;
            var cookies = await _web.CoreWebView2.CookieManager.GetCookiesAsync("https://leetcode.com");
            var session = cookies.FirstOrDefault(c => c.Name == "LEETCODE_SESSION")?.Value;
            if (string.IsNullOrEmpty(session)) return;
            _checking = true;
            _session = session;
            await _web.CoreWebView2.ExecuteScriptAsync(Script);
        }

        private void OnMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string user;
            try { user = e.TryGetWebMessageAsString() ?? string.Empty; } catch { user = string.Empty; }
            if (user.Length == 0) { _checking = false; return; }   // cookie set but not signed in yet (e.g. mid-redirect)
            Session = _session;
            Username = user;
            DialogResult = true;
        }

        private static void ClearProfile()
        {
            try { if (Directory.Exists(ProfileDir)) Directory.Delete(ProfileDir, true); }
            catch (Exception ex) { StorageService.Log($"webview profile cleanup: {ex.Message}"); }
        }
    }
}
