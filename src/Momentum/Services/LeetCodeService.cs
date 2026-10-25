using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Momentum.Models;

namespace Momentum.Services
{
    public sealed class LeetCodeResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public LeetCodeSnapshot? Snapshot { get; set; }
        /// <summary>Accepted problem slugs with the time they were (last) accepted, when known.</summary>
        public Dictionary<string, DateTime?> Solved { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool IsFullHistory { get; set; }
    }

    /// <summary>
    /// Reads public LeetCode profile data (no password). The optional LEETCODE_SESSION cookie unlocks the full
    /// solved list; it never leaves this machine except in the request to leetcode.com.
    /// </summary>
    public static class LeetCodeService
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36");
            http.DefaultRequestHeaders.Add("Referer", "https://leetcode.com");
            return http;
        }

        private const string ProfileQuery = @"query momentum($u: String!) {
  matchedUser(username: $u) {
    username
    profile { ranking }
    submitStatsGlobal { acSubmissionNum { difficulty count } }
    userCalendar { streak totalActiveDays submissionCalendar }
    tagProblemCounts {
      advanced { tagSlug problemsSolved }
      intermediate { tagSlug problemsSolved }
      fundamental { tagSlug problemsSolved }
    }
  }
  recentAcSubmissionList(username: $u, limit: 20) { titleSlug timestamp }
}";

        public static async Task<LeetCodeResult> SyncAsync(string username, string? session)
        {
            var result = new LeetCodeResult();
            username = username.Trim();
            if (username.Length == 0)
            {
                result.Error = "Add your LeetCode username first.";
                return result;
            }

            try
            {
                var body = JsonSerializer.Serialize(new { query = ProfileQuery, variables = new { u = username } });
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://leetcode.com/graphql")
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                using var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    result.Error = $"LeetCode returned {(int)response.StatusCode}. Try again in a minute.";
                    return result;
                }

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (!doc.RootElement.TryGetProperty("data", out var data) ||
                    !data.TryGetProperty("matchedUser", out var user) || user.ValueKind != JsonValueKind.Object)
                {
                    result.Error = $"No LeetCode user named '{username}'.";
                    return result;
                }

                var snap = new LeetCodeSnapshot { SyncedAt = DateTime.Now, Username = username };

                if (user.TryGetProperty("profile", out var profile) && profile.TryGetProperty("ranking", out var rank) && rank.ValueKind == JsonValueKind.Number)
                    snap.Ranking = rank.GetInt32();

                if (user.TryGetProperty("submitStatsGlobal", out var stats) && stats.TryGetProperty("acSubmissionNum", out var ac))
                {
                    foreach (var item in ac.EnumerateArray())
                    {
                        var count = item.GetProperty("count").GetInt32();
                        switch (item.GetProperty("difficulty").GetString())
                        {
                            case "All": snap.Total = count; break;
                            case "Easy": snap.Easy = count; break;
                            case "Medium": snap.Medium = count; break;
                            case "Hard": snap.Hard = count; break;
                        }
                    }
                }

                if (user.TryGetProperty("userCalendar", out var cal) && cal.ValueKind == JsonValueKind.Object)
                {
                    if (cal.TryGetProperty("streak", out var streak) && streak.ValueKind == JsonValueKind.Number) snap.Streak = streak.GetInt32();
                    if (cal.TryGetProperty("totalActiveDays", out var active) && active.ValueKind == JsonValueKind.Number) snap.ActiveDays = active.GetInt32();
                    if (cal.TryGetProperty("submissionCalendar", out var raw) && raw.ValueKind == JsonValueKind.String)
                    {
                        using var calDoc = JsonDocument.Parse(raw.GetString() ?? "{}");
                        foreach (var day in calDoc.RootElement.EnumerateObject())
                        {
                            if (!long.TryParse(day.Name, out var ts)) continue;
                            var date = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime.Date;
                            snap.Calendar[date.ToString("yyyy-MM-dd")] = day.Value.GetInt32();
                        }
                    }
                }

                if (user.TryGetProperty("tagProblemCounts", out var tags) && tags.ValueKind == JsonValueKind.Object)
                {
                    foreach (var level in tags.EnumerateObject())
                    {
                        if (level.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var t in level.Value.EnumerateArray())
                            snap.Tags[t.GetProperty("tagSlug").GetString() ?? ""] = t.GetProperty("problemsSolved").GetInt32();
                    }
                }

                if (data.TryGetProperty("recentAcSubmissionList", out var recent) && recent.ValueKind == JsonValueKind.Array)
                {
                    foreach (var sub in recent.EnumerateArray())
                    {
                        var slug = sub.GetProperty("titleSlug").GetString();
                        if (string.IsNullOrWhiteSpace(slug)) continue;
                        DateTime? when = null;
                        if (long.TryParse(sub.GetProperty("timestamp").GetString(), out var ts))
                            when = DateTimeOffset.FromUnixTimeSeconds(ts).LocalDateTime;
                        if (!result.Solved.ContainsKey(slug)) result.Solved[slug] = when;
                    }
                }

                result.Snapshot = snap;
                result.Success = true;

                if (!string.IsNullOrWhiteSpace(session))
                    await AddFullHistoryAsync(result, session!);

                return result;
            }
            catch (Exception ex)
            {
                result.Error = $"Couldn't reach LeetCode ({ex.Message}).";
                return result;
            }
        }

        private static async Task AddFullHistoryAsync(LeetCodeResult result, string session)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://leetcode.com/api/problems/all/");
                request.Headers.Add("Cookie", $"LEETCODE_SESSION={session.Trim()}");
                using var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return;

                result.IsFullHistory = ReadSolvedList(await response.Content.ReadAsStringAsync(), result) > 0;
            }
            catch (Exception ex)
            {
                StorageService.Log($"Full history sync failed: {ex.Message}");
            }
        }

        /// <summary>Adds accepted slugs from leetcode.com/api/problems/all/ JSON; returns how many were found.</summary>
        private static int ReadSolvedList(string json, LeetCodeResult result)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("stat_status_pairs", out var pairs)) return -1;

            int found = 0;
            foreach (var pair in pairs.EnumerateArray())
            {
                if (!pair.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String || status.GetString() != "ac") continue;
                var slug = pair.GetProperty("stat").GetProperty("question__title_slug").GetString();
                if (string.IsNullOrWhiteSpace(slug)) continue;
                found++;
                if (!result.Solved.ContainsKey(slug)) result.Solved[slug] = null;
            }
            return found;
        }

        /// <summary>
        /// Reads a solved list the user saved from their own signed-in browser. No cookie or password ever reaches Momentum.
        /// </summary>
        public static LeetCodeResult ImportSolvedFile(string path)
        {
            try { return ImportSolvedText(File.ReadAllText(path)); }
            catch (Exception ex) { return new LeetCodeResult { Error = $"Couldn't read that file: {ex.Message}" }; }
        }

        /// <summary>Accepts the raw JSON, or the page saved/copied as HTML (browsers wrap the JSON in a pre tag).</summary>
        public static LeetCodeResult ImportSolvedText(string text)
        {
            var result = new LeetCodeResult();
            try
            {
                var pre = System.Text.RegularExpressions.Regex.Match(text, @"<pre[^>]*>(.*?)</pre>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (pre.Success) text = pre.Groups[1].Value;
                int start = text.IndexOf('{'), end = text.LastIndexOf('}');
                if (start < 0 || end <= start) { result.Error = "That isn't the LeetCode problem list. Open leetcode.com/api/problems/all/ while signed in."; return result; }
                var json = System.Net.WebUtility.HtmlDecode(text[start..(end + 1)]);
                var found = ReadSolvedList(json, result);
                if (found < 0) result.Error = "That isn't the LeetCode problem list. Save the page from leetcode.com/api/problems/all/.";
                else if (found == 0) result.Error = "No solved problems in that file. Make sure you were signed in to LeetCode when you saved it.";
                else { result.Success = true; result.IsFullHistory = true; }
            }
            catch (Exception ex)
            {
                result.Error = $"Couldn't read that list: {ex.Message}";
            }
            return result;
        }
    }
}
