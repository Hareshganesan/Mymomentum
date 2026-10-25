using System.IO;
using System.Text.Json;
using Momentum.Models;

namespace Momentum.Services
{
    /// <summary>One-time import from Momentum v3 (MyTodo) so existing users keep their LeetCode link, solves and drives.</summary>
    public static class MigrationService
    {
        public static string V3File => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyMomentumPlacement", "placement_data.json");

        public static void TryImportV3(AppState state)
        {
            try
            {
                if (!File.Exists(V3File)) return;
                using var doc = JsonDocument.Parse(File.ReadAllText(V3File));
                var root = doc.RootElement;

                state.Profile.LeetCodeUsername = Str(root, "leetCodeUsername");
                state.Profile.LeetCodeSession = Str(root, "leetCodeSessionCookie");

                if (Get(root, "dsaProblems") is { ValueKind: JsonValueKind.Array } problems)
                {
                    foreach (var p in problems.EnumerateArray())
                    {
                        // Only carry over problems the user actually marked solved (v3 seeded some confidence values).
                        if (Str(p, "status") != "Solved" || Get(p, "lastSolvedDate") is not { ValueKind: JsonValueKind.String } solvedOn) continue;
                        var slug = SlugFromUrl(Str(p, "leetCodeUrl"));
                        if (slug.Length == 0) continue;
                        state.Problems[slug] = new ProblemProgress
                        {
                            Status = ProblemStatus.Solved,
                            SolvedOn = solvedOn.GetDateTime(),
                            Srs = new SrsState { Due = DateTime.Today.AddDays(1) },
                        };
                    }
                }

                if (Get(root, "companyApplications") is { ValueKind: JsonValueKind.Array } apps)
                {
                    foreach (var a in apps.EnumerateArray())
                    {
                        var app = new JobApplication
                        {
                            Company = Str(a, "companyName"),
                            Role = Str(a, "role"),
                            Ctc = Str(a, "packageCtc"),
                            NextEventTitle = Str(a, "upcomingEventTitle"),
                            Notes = Str(a, "roundNotes"),
                            Url = Str(a, "platformUrl"),
                        };
                        if (Get(a, "upcomingEventDate") is { ValueKind: JsonValueKind.String } d) app.NextEventDate = d.GetDateTime();
                        app.Stage = Str(a, "stage") switch
                        {
                            "Wishlist" => Stage.Wishlist,
                            "OnlineAssessment" => Stage.OA,
                            "TechnicalRound" => Stage.Interview,
                            "HrRound" => Stage.HR,
                            "OfferReceived" => Stage.Offer,
                            "Rejected" => Stage.Rejected,
                            _ => Stage.Applied,
                        };
                        if (app.Company.Length > 0) state.Applications.Add(app);
                    }
                }

                state.MigratedFromV3 = true;
            }
            catch (Exception ex)
            {
                StorageService.Log($"v3 import skipped: {ex.Message}");
            }
        }

        public static string SlugFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            var marker = "/problems/";
            var i = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return string.Empty;
            var rest = url[(i + marker.Length)..];
            var end = rest.IndexOfAny(new[] { '/', '?', '#' });
            return (end < 0 ? rest : rest[..end]).Trim().ToLowerInvariant();
        }

        private static JsonElement? Get(JsonElement e, string name)
        {
            foreach (var p in e.EnumerateObject())
                if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return p.Value;
            return null;
        }

        private static string Str(JsonElement e, string name) =>
            Get(e, name) is { ValueKind: JsonValueKind.String } v ? v.GetString() ?? string.Empty : string.Empty;
    }
}
