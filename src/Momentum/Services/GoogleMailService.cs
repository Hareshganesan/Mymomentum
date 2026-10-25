using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Momentum.Services
{
    public sealed record GmailMessage(string Id, string Subject, string From, DateTime Date, string Snippet);

    /// <summary>
    /// "Sign in with Google" for desktop apps: OAuth 2.0 authorization code + PKCE with a loopback redirect, read-only
    /// Gmail scope. Only job-related messages are requested, and only their headers + snippet.
    /// </summary>
    public static class GoogleMailService
    {
        private const string Scope = "https://www.googleapis.com/auth/gmail.readonly";
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>Gmail search that keeps OTPs, banking and personal mail out of what Momentum ever fetches.</summary>
        private const string JobQuery =
            "(application OR applied OR applying OR candidacy OR interview OR assessment OR hackerrank OR hackerearth OR codility OR codesignal " +
            "OR \"online test\" OR \"coding test\" OR shortlisted OR shortlist OR \"offer letter\" OR unfortunately OR recruiting OR recruitment OR drive OR \"test schedule\") " +
            "-\"job alert\" -\"jobs for you\" -otp -\"verification code\" -\"one-time\"";

        public static async Task<(string? RefreshToken, string? Email, string Error)> SignInAsync(string clientId, string clientSecret, CancellationToken ct = default)
        {
            var verifier = B64Url(RandomNumberGenerator.GetBytes(32));
            var challenge = B64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            var state = B64Url(RandomNumberGenerator.GetBytes(16));

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var redirect = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
                var url = "https://accounts.google.com/o/oauth2/v2/auth?response_type=code&access_type=offline&prompt=consent" +
                          $"&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirect)}" +
                          $"&scope={Uri.EscapeDataString(Scope)}&code_challenge={challenge}&code_challenge_method=S256&state={state}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

                // Wait (up to 5 minutes) for Google to redirect the browser back to us.
                var accept = listener.AcceptTcpClientAsync(ct).AsTask();
                if (await Task.WhenAny(accept, Task.Delay(TimeSpan.FromMinutes(5), ct)) != accept) return (null, null, "Sign-in timed out.");
                using var client = await accept;
                using var stream = client.GetStream();
                var requestLine = await new StreamReader(stream, Encoding.ASCII, false, 4096, leaveOpen: true).ReadLineAsync(ct) ?? string.Empty;
                var query = ParseQuery(requestLine);

                var ok = query.TryGetValue("code", out var code) && query.GetValueOrDefault("state") == state;
                var page = ok ? "<h2>Signed in.</h2><p>You can close this tab and return to Momentum.</p>" : "<h2>Sign-in was cancelled.</h2><p>You can close this tab.</p>";
                var body = Encoding.UTF8.GetBytes($"<html><body style=\"font-family:Segoe UI;background:#0B0C0E;color:#F0F0EE;padding:40px\">{page}</body></html>");
                var head = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                await stream.WriteAsync(head, ct);
                await stream.WriteAsync(body, ct);
                if (!ok) return (null, null, query.GetValueOrDefault("error") ?? "Sign-in was cancelled.");

                var token = await PostForm("https://oauth2.googleapis.com/token", new()
                {
                    ["code"] = code!, ["client_id"] = clientId, ["client_secret"] = clientSecret,
                    ["redirect_uri"] = redirect, ["grant_type"] = "authorization_code", ["code_verifier"] = verifier,
                }, ct);
                if (token == null || !token.Value.TryGetProperty("refresh_token", out var refresh)) return (null, null, "Google didn't return a sign-in token. Check the Client ID and secret.");

                var access = token.Value.GetProperty("access_token").GetString()!;
                var profile = await GetJson("https://gmail.googleapis.com/gmail/v1/users/me/profile", access, ct);
                return (refresh.GetString(), profile?.GetProperty("emailAddress").GetString(), string.Empty);
            }
            catch (Exception ex)
            {
                StorageService.Log("Google sign-in: " + ex);
                return (null, null, $"Sign-in failed ({ex.Message}).");
            }
            finally
            {
                listener.Stop();
            }
        }

        public static async Task<string?> AccessTokenAsync(string clientId, string clientSecret, string refreshToken, CancellationToken ct = default)
        {
            var json = await PostForm("https://oauth2.googleapis.com/token", new()
            {
                ["client_id"] = clientId, ["client_secret"] = clientSecret, ["refresh_token"] = refreshToken, ["grant_type"] = "refresh_token",
            }, ct);
            return json?.TryGetProperty("access_token", out var a) == true ? a.GetString() : null;
        }

        public static async Task RevokeAsync(string token)
        {
            try { await Http.PostAsync("https://oauth2.googleapis.com/revoke?token=" + Uri.EscapeDataString(token), null); }
            catch (Exception ex) { StorageService.Log("Google revoke: " + ex.Message); }
        }

        /// <summary>Access tokens last an hour; this renews them during long syncs and after a 401.</summary>
        public sealed class TokenSource
        {
            private readonly string _id, _secret, _refresh;
            private string? _access;
            private DateTime _at;
            public TokenSource(string clientId, string clientSecret, string refreshToken) { _id = clientId; _secret = clientSecret; _refresh = refreshToken; }

            public async Task<string?> GetAsync(bool force = false, CancellationToken ct = default)
            {
                if (!force && _access != null && (DateTime.Now - _at).TotalMinutes < 45) return _access;
                _access = await AccessTokenAsync(_id, _secret, _refresh, ct);
                _at = DateTime.Now;
                return _access;
            }
        }

        /// <summary>Job-related messages since a date: headers (Subject/From) and snippet only, fetched 8 at a time.</summary>
        public static async Task<List<GmailMessage>> FetchJobMailAsync(TokenSource token, DateTime since, IProgress<string>? progress, CancellationToken ct = default)
        {
            var ids = new List<string>();
            string? pageToken = null;
            var q = Uri.EscapeDataString($"after:{since:yyyy/MM/dd} {JobQuery}");
            do
            {
                var list = await GetJson($"https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=100&q={q}" + (pageToken == null ? "" : $"&pageToken={pageToken}"), token, ct)
                           ?? throw new InvalidOperationException("Gmail didn't respond to the search.");
                if (list.TryGetProperty("messages", out var msgs))
                    ids.AddRange(msgs.EnumerateArray().Select(m => m.GetProperty("id").GetString()!));
                pageToken = list.TryGetProperty("nextPageToken", out var t) ? t.GetString() : null;
            } while (pageToken != null && ids.Count < 600);

            var result = new System.Collections.Concurrent.ConcurrentBag<GmailMessage>();
            int done = 0, failures = 0;
            using var gate = new SemaphoreSlim(8);
            await Task.WhenAll(ids.Select(async id =>
            {
                await gate.WaitAsync(ct);
                try
                {
                    if (Volatile.Read(ref failures) > 15) return;   // Gmail is refusing; stop instead of crawling
                    var m = await GetJson($"https://gmail.googleapis.com/gmail/v1/users/me/messages/{id}?format=metadata&metadataHeaders=Subject&metadataHeaders=From", token, ct);
                    if (m is not { } msg) { Interlocked.Increment(ref failures); return; }
                    string Header(string name) => msg.GetProperty("payload").TryGetProperty("headers", out var hs)
                        ? hs.EnumerateArray().FirstOrDefault(h => h.GetProperty("name").GetString()!.Equals(name, StringComparison.OrdinalIgnoreCase)) is { ValueKind: JsonValueKind.Object } h ? h.GetProperty("value").GetString() ?? "" : ""
                        : "";
                    var date = long.TryParse(msg.GetProperty("internalDate").GetString(), out var ms) ? DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime : DateTime.Now;
                    result.Add(new GmailMessage(id, Header("Subject"), Header("From"), date, msg.TryGetProperty("snippet", out var sn) ? WebUtility.HtmlDecode(sn.GetString() ?? "") : ""));
                }
                finally
                {
                    var n = Interlocked.Increment(ref done);
                    if (n % 20 == 0 || n == ids.Count) progress?.Report($"Reading {n} of {ids.Count} job emails…");
                    gate.Release();
                }
            }));
            if (failures > 15) throw new InvalidOperationException("Gmail stopped responding. Try Sync inbox again in a minute.");
            return result.ToList();
        }

        /// <summary>Plain-text body (first ~6000 chars). Only used for placement-cell mail, where the company is in the body.</summary>
        public static async Task<string?> FetchBodyAsync(TokenSource token, string id, CancellationToken ct = default)
        {
            var m = await GetJson($"https://gmail.googleapis.com/gmail/v1/users/me/messages/{id}?format=full", token, ct);
            if (m == null) return null;
            string? plain = null, html = null;
            void Walk(JsonElement part)
            {
                var mime = part.TryGetProperty("mimeType", out var mt) ? mt.GetString() : null;
                if (part.TryGetProperty("body", out var body) && body.TryGetProperty("data", out var data))
                {
                    var b64 = data.GetString()!.Replace('-', '+').Replace('_', '/');
                    b64 = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
                    var text = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                    if (mime == "text/plain") plain ??= text; else if (mime == "text/html") html ??= text;
                }
                if (part.TryGetProperty("parts", out var parts)) foreach (var p in parts.EnumerateArray()) Walk(p);
            }
            Walk(m.Value.GetProperty("payload"));
            var result = plain ?? (html == null ? null : WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(html, "<(br|/p|/div|/tr|/li)[^>]*>|<[^>]+>", mm => mm.Value.StartsWith("</") || mm.Value.StartsWith("<br") ? "\n" : " ")));
            return result == null ? null : result.Length > 6000 ? result[..6000] : result;
        }

        // ---------- helpers ----------

        private static async Task<JsonElement?> PostForm(string url, Dictionary<string, string> form, CancellationToken ct)
        {
            using var resp = await Http.PostAsync(url, new FormUrlEncodedContent(form), ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) { StorageService.Log($"Google token error {(int)resp.StatusCode}: {text}"); return null; }
            return JsonDocument.Parse(text).RootElement.Clone();
        }

        private static async Task<JsonElement?> GetJson(string url, string accessToken, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var resp = await Http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) { StorageService.Log($"Gmail API {(int)resp.StatusCode} for {url.Split('?')[0]}"); return null; }
            return JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct)).RootElement.Clone();
        }

        private static async Task<JsonElement?> GetJson(string url, TokenSource token, CancellationToken ct)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var access = await token.GetAsync(force: attempt > 0, ct);
                if (access == null) return null;
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access);
                using var resp = await Http.SendAsync(req, ct);
                if (resp.StatusCode == HttpStatusCode.Unauthorized && attempt == 0) continue;   // expired: refresh and retry once
                if (!resp.IsSuccessStatusCode) { StorageService.Log($"Gmail API {(int)resp.StatusCode} for {url.Split('?')[0]}"); return null; }
                return JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct)).RootElement.Clone();
            }
            return null;
        }

        private static Dictionary<string, string> ParseQuery(string requestLine)
        {
            var result = new Dictionary<string, string>();
            var path = requestLine.Split(' ').ElementAtOrDefault(1) ?? string.Empty;
            var q = path.Contains('?') ? path[(path.IndexOf('?') + 1)..] : string.Empty;
            foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                result[Uri.UnescapeDataString(kv[0])] = kv.Length > 1 ? Uri.UnescapeDataString(kv[1].Replace('+', ' ')) : string.Empty;
            }
            return result;
        }

        private static string B64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
