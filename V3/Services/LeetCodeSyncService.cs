using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyTodo.Models;

namespace MyTodo.Services
{
    /// <summary>
    /// Provides safe, non-invasive, secure synchronization with LeetCode.
    /// Supports public profile sync (zero passwords needed) and optional session cookie sync.
    /// </summary>
    public static class LeetCodeSyncService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static LeetCodeSyncService()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        }

        /// <summary>
        /// Safely fetches public LeetCode profile data and recent accepted submissions.
        /// Requires only the public username - zero passwords or tokens.
        /// </summary>
        public static async Task<LeetCodeProfileResult> FetchPublicProfileAsync(string username)
        {
            var result = new LeetCodeProfileResult
            {
                Username = username.Trim()
            };

            if (string.IsNullOrWhiteSpace(username))
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Please enter a valid LeetCode username.";
                return result;
            }

            try
            {
                var graphqlQuery = new
                {
                    query = @"
                        query getUserProfile($username: String!) {
                            matchedUser(username: $username) {
                                username
                                profile {
                                    ranking
                                }
                                submitStatsGlobal {
                                    acSubmissionNum {
                                        difficulty
                                        count
                                    }
                                }
                            }
                            recentAcSubmissionList(username: $username, limit: 50) {
                                title
                                titleSlug
                            }
                        }",
                    variables = new { username = result.Username }
                };

                var jsonPayload = JsonSerializer.Serialize(graphqlQuery);
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://leetcode.com/graphql");
                request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"LeetCode responded with status: {response.StatusCode}. Profile may be private or username invalid.";
                    return result;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var dataElement))
                {
                    if (dataElement.TryGetProperty("matchedUser", out var matchedUser) && matchedUser.ValueKind == JsonValueKind.Object)
                    {
                        // Parse ranking
                        if (matchedUser.TryGetProperty("profile", out var profile) && profile.TryGetProperty("ranking", out var rankingProp))
                        {
                            result.Ranking = rankingProp.GetInt32();
                        }

                        // Parse solved counts
                        if (matchedUser.TryGetProperty("submitStatsGlobal", out var submitStats) &&
                            submitStats.TryGetProperty("acSubmissionNum", out var acList) &&
                            acList.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in acList.EnumerateArray())
                            {
                                if (item.TryGetProperty("difficulty", out var diffProp) && item.TryGetProperty("count", out var countProp))
                                {
                                    var diff = diffProp.GetString();
                                    var count = countProp.GetInt32();

                                    if (diff == "All") result.TotalSolved = count;
                                    else if (diff == "Easy") result.EasySolved = count;
                                    else if (diff == "Medium") result.MediumSolved = count;
                                    else if (diff == "Hard") result.HardSolved = count;
                                }
                            }
                        }

                        // Parse recent accepted submissions
                        if (dataElement.TryGetProperty("recentAcSubmissionList", out var recentSubmissions) &&
                            recentSubmissions.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var sub in recentSubmissions.EnumerateArray())
                            {
                                if (sub.TryGetProperty("title", out var titleProp))
                                {
                                    var title = titleProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(title))
                                    {
                                        result.SolvedTitles.Add(title.Trim());
                                    }
                                }

                                if (sub.TryGetProperty("titleSlug", out var slugProp))
                                {
                                    var slug = slugProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(slug))
                                    {
                                        result.SolvedSlugs.Add(slug.Trim().ToLower());
                                    }
                                }
                            }
                        }

                        result.IsSuccess = true;
                        return result;
                    }
                }

                result.IsSuccess = false;
                result.ErrorMessage = $"User '{result.Username}' not found on LeetCode. Please verify your handle.";
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Network or connection error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Optional full synchronization using user's LEETCODE_SESSION cookie.
        /// Securely queries leetcode.com/api/problems/all/ to retrieve ALL solved problems across account history.
        /// Cookie stays 100% local on user's machine.
        /// </summary>
        public static async Task<LeetCodeProfileResult> FetchFullHistoryWithSessionAsync(string sessionCookie, string username = "")
        {
            var result = new LeetCodeProfileResult
            {
                Username = username
            };

            if (string.IsNullOrWhiteSpace(sessionCookie))
            {
                return await FetchPublicProfileAsync(username);
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "https://leetcode.com/api/problems/all/");
                request.Headers.Add("Cookie", $"LEETCODE_SESSION={sessionCookie.Trim()}");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Session cookie expired or invalid. Falling back to public sync.";
                    return await FetchPublicProfileAsync(username);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("user_name", out var userProp) && string.IsNullOrWhiteSpace(result.Username))
                {
                    result.Username = userProp.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("num_solved", out var numSolvedProp))
                {
                    result.TotalSolved = numSolvedProp.GetInt32();
                }

                if (root.TryGetProperty("stat_status_pairs", out var pairs) && pairs.ValueKind == JsonValueKind.Array)
                {
                    foreach (var pair in pairs.EnumerateArray())
                    {
                        // Check if problem is solved (status == "ac")
                        if (pair.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "ac")
                        {
                            if (pair.TryGetProperty("stat", out var stat))
                            {
                                if (stat.TryGetProperty("question__title", out var titleProp))
                                {
                                    var title = titleProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(title))
                                    {
                                        result.SolvedTitles.Add(title.Trim());
                                    }
                                }

                                if (stat.TryGetProperty("question__title_slug", out var slugProp))
                                {
                                    var slug = slugProp.GetString();
                                    if (!string.IsNullOrWhiteSpace(slug))
                                    {
                                        result.SolvedSlugs.Add(slug.Trim().ToLower());
                                    }
                                }
                            }
                        }
                    }
                }

                result.IsSuccess = true;
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error during full session sync: {ex.Message}";
                return result;
            }
        }
    }
}
