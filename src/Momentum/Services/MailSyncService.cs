using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Momentum.Models;

namespace Momentum.Services
{
    public sealed record MailClassification(string Company, Stage Stage, bool Campus = false, string? Role = null);

    public sealed class MailSyncResult
    {
        public bool Success { get; set; }
        public string Error { get; set; } = string.Empty;
        public int Scanned { get; set; }
        public int JobEmails { get; set; }
        public int Created { get; set; }
        public int Updated { get; set; }
        public List<string> Changes { get; } = new();
    }

    /// <summary>
    /// Reads recent mail over IMAP (headers + a short preview only), recognises job-application emails, and turns
    /// them into application updates: new applications, stage advances, rejections. Nothing is ever sent or modified
    /// on the mail server — the inbox is opened read-only.
    /// </summary>
    public static class MailSyncService
    {
        // ---------- Credentials (DPAPI, current Windows user) ----------

        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Momentum.MailSync.v1");

        public static string Protect(string secret) =>
            string.IsNullOrEmpty(secret) ? string.Empty
                : Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), Entropy, DataProtectionScope.CurrentUser));

        public static string Unprotect(string blob)
        {
            try { return string.IsNullOrEmpty(blob) ? string.Empty : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(blob), Entropy, DataProtectionScope.CurrentUser)); }
            catch { return string.Empty; }
        }

        /// <summary>Reads a protected value, accepting older plain-text values saved before encryption.</summary>
        public static string UnprotectOrPlain(string value)
        {
            var plain = Unprotect(value);
            return plain.Length > 0 ? plain : value;
        }

        public static string GuessHost(string address)
        {
            var domain = address.Split('@').LastOrDefault()?.Trim().ToLowerInvariant() ?? string.Empty;
            return domain switch
            {
                "gmail.com" or "googlemail.com" => "imap.gmail.com",
                "outlook.com" or "hotmail.com" or "live.com" or "msn.com" => "outlook.office365.com",
                "yahoo.com" or "yahoo.co.in" or "ymail.com" => "imap.mail.yahoo.com",
                "icloud.com" or "me.com" => "imap.mail.me.com",
                "" => string.Empty,
                _ => "imap." + domain,
            };
        }

        // ---------- Sync ----------

        public static async Task<MailSyncResult> SyncAsync(AppState state, IProgress<string>? progress = null, CancellationToken ct = default)
        {
            var result = new MailSyncResult();
            var p = state.Profile;
            MigrateRules(state);
            if (!string.IsNullOrEmpty(p.GoogleToken)) { var r = await SyncGmailAsync(state, result, progress, ct); AgeOut(state, r); return r; }
            var password = Unprotect(p.MailSecret);
            if (string.IsNullOrWhiteSpace(p.MailAddress) || password.Length == 0)
            {
                result.Error = "Connect your mailbox first.";
                return result;
            }

            try
            {
                using var client = new ImapClient { Timeout = 30000 };
                await client.ConnectAsync(string.IsNullOrWhiteSpace(p.MailHost) ? GuessHost(p.MailAddress) : p.MailHost, 993, SecureSocketOptions.SslOnConnect, ct);
                await client.AuthenticateAsync(p.MailAddress.Trim(), password, ct);
                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

                var since = (p.MailLastSync ?? DateTime.Today.AddDays(-120)).AddDays(-2);
                var uids = await inbox.SearchAsync(SearchQuery.DeliveredAfter(since), ct);
                var recent = uids.Reverse().Take(1500).ToList();
                var summaries = recent.Count == 0 ? new List<IMessageSummary>()
                    : (await inbox.FetchAsync(recent, MessageSummaryItems.Envelope | MessageSummaryItems.UniqueId | MessageSummaryItems.PreviewText, ct)).ToList();

                foreach (var m in summaries.OrderBy(m => m.Envelope?.Date ?? DateTimeOffset.MinValue))
                {
                    var env = m.Envelope;
                    if (env == null) continue;
                    result.Scanned++;
                    var id = env.MessageId ?? $"{m.UniqueId}";
                    if (state.SeenMail.Contains(id)) continue;

                    var from = env.From.Mailboxes.FirstOrDefault();
                    var text = m.PreviewText ?? string.Empty;
                    if (IsCampusSender(from?.Name ?? string.Empty, from?.Address ?? string.Empty))
                    {
                        try { text = (await inbox.GetMessageAsync(m.UniqueId, ct)).TextBody ?? text; } catch { /* keep preview */ }
                    }
                    var c = Classify(env.Subject ?? string.Empty, from?.Name ?? string.Empty, from?.Address ?? string.Empty, text);
                    if (c == null) continue;

                    state.SeenMail.Add(id);
                    result.JobEmails++;
                    Apply(state, c, new MailEvent
                    {
                        Date = env.Date?.LocalDateTime ?? DateTime.Now,
                        Subject = env.Subject ?? string.Empty,
                        From = from?.Name is { Length: > 0 } n ? n : from?.Address ?? string.Empty,
                        Stage = c.Stage,
                    }, result);
                }

                await client.DisconnectAsync(true, ct);
                p.MailLastSync = DateTime.Now;
                result.Success = true;
                AgeOut(state, result);
            }
            catch (AuthenticationException)
            {
                result.Error = "Sign-in failed. Use an app password (not your normal password) and make sure IMAP is enabled.";
            }
            catch (Exception ex)
            {
                result.Error = $"Couldn't reach your mailbox ({ex.Message}).";
                StorageService.Log("Mail sync: " + ex);
            }
            return result;
        }

        public const int RulesVersion = 5;

        /// <summary>When the classifier gets stricter, drop cards it created automatically and re-read the inbox.</summary>
        private static void MigrateRules(AppState state)
        {
            if (state.Profile.MailRulesVersion >= RulesVersion) return;
            if (state.Profile.MailRulesVersion == 4)
            {
                // v5: campus names are now read from the full email; roles split into separate cards. Keep everything else.
                state.Applications.RemoveAll(a => a.Source == "Campus" && string.IsNullOrWhiteSpace(a.Notes) && string.IsNullOrWhiteSpace(a.Url) && string.IsNullOrWhiteSpace(a.Ctc));
                SplitByRole(state);
                state.SeenMail.Clear();
                state.Profile.MailLastSync = null;
                state.Profile.MailRulesVersion = RulesVersion;
                return;
            }
            state.Applications.RemoveAll(a => a.Emails.Count > 0 && string.IsNullOrWhiteSpace(a.Notes) && string.IsNullOrWhiteSpace(a.Url)
                                             && string.IsNullOrWhiteSpace(a.Role) && string.IsNullOrWhiteSpace(a.Ctc));
            foreach (var a in state.Applications) a.Emails.Clear();
            state.SeenMail.Clear();
            state.Profile.MailLastSync = null;
            state.Profile.MailRulesVersion = RulesVersion;
        }

        /// <summary>Splits a card whose emails mention different roles into one card per role, with each stage recomputed.</summary>
        public static void SplitByRole(AppState state)
        {
            foreach (var app in state.Applications.ToList())
            {
                var byRole = app.Emails.GroupBy(e => ExtractRole(e.Subject, string.Empty) is { } r ? RoleKey(r) : string.Empty).ToList();
                var named = byRole.Where(g => g.Key.Length > 0).ToList();
                if (named.Count == 0) continue;
                if (string.IsNullOrWhiteSpace(app.Role)) app.Role = ExtractRole(named[0].First().Subject, string.Empty) ?? string.Empty;
                if (named.Count < 2) continue;
                foreach (var g in named.Where(g => g.Key != RoleKey(app.Role)))
                {
                    var copy = new JobApplication
                    {
                        Company = app.Company, Role = ExtractRole(g.First().Subject, string.Empty) ?? string.Empty, Source = app.Source,
                        CreatedAt = g.Min(e => e.Date), Emails = g.ToList(), Stage = Stage.Applied,
                    };
                    foreach (var e in copy.Emails.OrderBy(e => e.Date))
                        if (e.Stage is { } st && (st == Stage.Rejected || st > copy.Stage)) copy.MoveTo(st);
                    app.Emails.RemoveAll(e => copy.Emails.Contains(e));
                    state.Applications.Add(copy);
                }
            }
        }

        public const int StaleDays = 21, GhostDaysEarly = 45, GhostDaysLate = 60;

        /// <summary>No reply for 45 days (60 after an interview/HR round) → archived as "no response".</summary>
        public static void AgeOut(AppState state, MailSyncResult result)
        {
            foreach (var a in state.Applications.Where(a => a.Stage is Stage.Applied or Stage.OA or Stage.Interview or Stage.HR))
            {
                var last = a.Emails.Count > 0 ? a.Emails.Max(e => e.Date) : a.CreatedAt;
                if (a.RestoredAt is { } r && r > last) last = r;
                var limit = a.Stage is Stage.Interview or Stage.HR ? GhostDaysLate : GhostDaysEarly;
                if (a.NextEventDate is { } d && d.Date >= DateTime.Today) continue;   // something is scheduled
                if ((DateTime.Now - last).TotalDays < limit) continue;
                a.MoveTo(Stage.Rejected);
                a.Notes = (a.Notes.Length > 0 ? a.Notes + "\n" : string.Empty) + $"Auto-archived {DateTime.Today:d MMM}: no response for {limit}+ days.";
                result.Changes.Add($"{a.Company} archived (no reply in {limit} days)");
                result.Updated++;
            }
        }

        public static bool IsCampusSender(string fromName, string fromAddress)
        {
            var domain = fromAddress.Split('@').LastOrDefault()?.ToLowerInvariant() ?? string.Empty;
            return Regex.IsMatch(domain, @"(\.edu|\.ac\.[a-z]+|\.edu\.[a-z]+)$|amrita")
                || Regex.IsMatch(fromName, @"\bCIR\b|placement|training|\bT\s?&\s?P\b|\bTPO\b", RegexOptions.IgnoreCase);
        }

        private static readonly Regex[] RolePatterns =
        {
            R(@"(?:application|applying|applied|candidacy)\s+(?:for|to)\s+(?:the\s+)?(?:role\s+of\s+|position\s+of\s+)?(?<r>[A-Za-z][\w/&+.()\- ]{2,60}?)\s+(?:role\s+|position\s+|job\s+|opening\s+)?(?:at|with|@)\s+[A-Z]"),
            R(@"\b(?:role|position|job title|designation|profile)\s*[:\-–]\s*(?<r>[^\n|,;]{3,60})"),
            R(@"job application\s*[:\-–]\s*(?<r>[^\n|(]{3,60})"),
            R(@"\b(?:for|of)\s+(?:the\s+)?(?<r>[A-Za-z][\w/&+.()\- ]{2,50}?)\s+(?:role|position|opening)\b"),
            R(@"\b(?<r>(?:Senior\s+|Junior\s+|Associate\s+|Graduate\s+)?(?:Software|SDE|Data|ML|Machine Learning|AI|Frontend|Front[- ]End|Backend|Back[- ]End|Full[- ]?Stack|Product|Systems?|Cloud|DevOps|Security|Research|Hardware|Embedded|Mobile|Android|iOS|Web|QA|Test|Analytics?|Business|Quant|Silicon|ASIC|VLSI|Design)[\w/&+.\- ]{0,30}?(?:Engineer(?:ing)?|Developer|Intern(?:ship)?|Scientist|Analyst|Associate|Trainee|Manager)(?:\s*(?:I{1,3}|[1-3]))?)\b"),
            R(@"\b(?<r>SDE[- ]?(?:I{1,3}|[1-3])?(?:\s+Intern)?)\b"),
        };

        /// <summary>Role named in the email, e.g. "Software Development Engineer Intern" or "SDE-1"; null when unclear.</summary>
        public static string? ExtractRole(string subject, string preview)
        {
            foreach (var text in new[] { subject, preview.Length > 1500 ? preview[..1500] : preview })
            {
                foreach (var rx in RolePatterns)
                {
                    var m = rx.Match(text);
                    if (!m.Success) continue;
                    var r = Regex.Replace(m.Groups["r"].Value, @"\(?\b(?:R|JR|REQ)[-_ ]?\d+\)?|\b20\d\d\b|[-–|,]\s*$", "", RegexOptions.IgnoreCase).Trim(' ', '-', '.', ',', '(', ')');
                    r = Regex.Replace(r, @"\s+", " ");
                    if (r.Length is < 2 or > 60 || Regex.IsMatch(r, @"^(the|a|an|your|our|this|we|you|us)\b", RegexOptions.IgnoreCase)) continue;
                    return char.ToUpperInvariant(r[0]) + r[1..];
                }
            }
            return null;
        }

        public static string RoleKey(string? role) =>
            Regex.Replace((role ?? string.Empty).ToLowerInvariant().Replace("internship", "intern").Replace("software development engineer", "sde"), @"[^a-z0-9]", "");

        private static bool SameCompany(string a, string b)
        {
            var k = Normalize(a); var key = Normalize(b);
            return k.Length > 0 && (k == key || k.StartsWith(key) || key.StartsWith(k));
        }

        public static bool IsConnected(Profile p) =>
            !string.IsNullOrEmpty(p.GoogleToken) || (!string.IsNullOrEmpty(p.MailSecret) && !string.IsNullOrWhiteSpace(p.MailAddress));

        private static async Task<MailSyncResult> SyncGmailAsync(AppState state, MailSyncResult result, IProgress<string>? progress, CancellationToken ct)
        {
            var p = state.Profile;
            var token = new GoogleMailService.TokenSource(p.GoogleClientId, Unprotect(p.GoogleClientSecret), Unprotect(p.GoogleToken));
            if (await token.GetAsync(ct: ct) == null)
            {
                result.Error = "Google sign-in expired or was revoked. Sign in with Google again.";
                return result;
            }
            var since = (p.MailLastSync ?? DateTime.Today.AddDays(-120)).AddDays(-2);
            progress?.Report("Searching your inbox for job emails…");
            List<GmailMessage> messages;
            try { messages = await GoogleMailService.FetchJobMailAsync(token, since, progress, ct); }
            catch (InvalidOperationException ex) { result.Error = ex.Message; return result; }
            foreach (var m in messages.OrderBy(m => m.Date))
            {
                result.Scanned++;
                if (state.SeenMail.Contains(m.Id)) continue;
                var mb = MimeKit.MailboxAddress.TryParse(m.From, out var parsed) ? parsed : null;
                var text = m.Snippet;
                if (IsCampusSender(mb?.Name ?? string.Empty, mb?.Address ?? m.From))
                    text = await GoogleMailService.FetchBodyAsync(token, m.Id, ct) ?? m.Snippet;
                var c = Classify(m.Subject, mb?.Name ?? string.Empty, mb?.Address ?? m.From, text);
                if (c == null) continue;
                state.SeenMail.Add(m.Id);
                result.JobEmails++;
                Apply(state, c, new MailEvent { Date = m.Date, Subject = m.Subject, From = mb?.Name is { Length: > 0 } n ? n : mb?.Address ?? m.From, Stage = c.Stage }, result);
            }
            p.MailLastSync = DateTime.Now;
            result.Success = true;
            return result;
        }

        /// <summary>Creates or advances the matching application. Stages only move forward; a rejection archives.</summary>
        public static void Apply(AppState state, MailClassification c, MailEvent evt, MailSyncResult result)
        {
            // One card per (company, role). Emails without a clear role go to that company's most recently active card.
            var same = state.Applications.Where(a => SameCompany(a.Company, c.Company)).ToList();
            JobApplication? app = c.Role != null
                ? same.FirstOrDefault(a => RoleKey(a.Role) == RoleKey(c.Role)) ?? same.FirstOrDefault(a => string.IsNullOrWhiteSpace(a.Role))
                : same.OrderByDescending(a => a.Emails.Count == 0 ? a.CreatedAt : a.Emails.Max(e => e.Date)).FirstOrDefault();
            if (app != null && c.Role != null && string.IsNullOrWhiteSpace(app.Role)) app.Role = c.Role;

            if (app == null)
            {
                app = new JobApplication { Company = c.Company, Role = c.Role ?? string.Empty, Stage = c.Stage, CreatedAt = evt.Date, Source = c.Campus ? "Campus" : "Email" };
                if (c.Stage == Stage.Rejected) app.ReachedStage = Stage.Applied;
                state.Applications.Add(app);
                result.Created++;
                result.Changes.Add($"Added {c.Company} ({Label(c.Stage)})");
            }
            else if (app.Stage != Stage.Offer && (c.Stage == Stage.Rejected ? app.Stage != Stage.Rejected : c.Stage > app.Stage && app.Stage != Stage.Rejected))
            {
                app.MoveTo(c.Stage);
                result.Updated++;
                result.Changes.Add($"{app.Company} → {Label(c.Stage)}");
            }

            if (c.Stage is Stage.OA or Stage.Interview && string.IsNullOrWhiteSpace(app.NextEventTitle))
                app.NextEventTitle = c.Stage == Stage.OA ? "Online assessment" : "Interview";
            // Re-scans never duplicate a timeline entry.
            if (!app.Emails.Any(e => e.Subject == evt.Subject && Math.Abs((e.Date - evt.Date).TotalMinutes) < 2)) app.Emails.Add(evt);
        }

        public static string Label(Stage s) => s switch
        {
            Stage.OA => "online test",
            Stage.Interview => "interview",
            Stage.HR => "HR round",
            Stage.Offer => "offer",
            Stage.Rejected => "rejected",
            _ => "applied",
        };

        // ---------- Classifier (pure, testable) ----------

        private static readonly Regex Noise = R(@"job alert|jobs? (you may|for you|recommended|matching)|new jobs|recommended jobs|people are hiring|is hiring|newsletter|webinar|unsubscribe from job|top job picks|apply now to|similar jobs");
        private static readonly (Stage Stage, Regex Rx)[] Rules =
        {
            (Stage.Rejected, R(@"unfortunately|regret to inform|not (be )?(moving|move) forward|decided to (move|proceed|go) (forward )?with other|not been selected|will not be (proceeding|progressing)|position has been filled|no longer (under )?consider|not (to )?proceed with your (application|candidacy)")),
            (Stage.Offer, R(@"offer letter|pleased to (offer|extend)|delighted to offer|job offer|extend (you )?an offer|congratulations.{0,60}(offer|selected|hired)|you('ve| have) been selected")),
            (Stage.Interview, R(@"interview (invitation|invite|schedul|confirm|slot|details)|invit(e|ation) (you )?(to|for) (an? |the )?(interview|technical|virtual)|schedule (an|your|the) interview|interview with|technical (round|interview)|next round|shortlisted for")),
            (Stage.OA, R(@"online (assessment|test)|coding (assessment|challenge|test|round)|hackerrank|hackerearth|codility|codesignal|mettl|amcat|aptitude test|test link|take.?home|assessment (invite|invitation|link)|complete (the|your|an) assessment")),
            (Stage.Applied, R(@"thank(s| you) for (applying|your application|your interest in)|application (received|submitted|confirmation|has been received)|we('ve| have) received your application|successfully applied|your application (for|to|at|has been)|received your (resume|application)")),
        };

        private static readonly Regex SenderNoise = R(@"internshala|unstop|naukri|linkedin|leetcode|digialm|tcsion|hirepro|codechef|codeforces|geeksforgeeks|glassdoor|indeed|foundit|apna|shine\.com|coursera|udemy|summer.?school|newsletter|community|events?@|webinar|hackathon");
        private static readonly HashSet<string> PersonalDomains = new(StringComparer.OrdinalIgnoreCase)
            { "gmail.com", "googlemail.com", "yahoo.com", "yahoo.co.in", "outlook.com", "hotmail.com", "live.com", "icloud.com", "rediffmail.com", "proton.me", "protonmail.com" };
        private static readonly HashSet<string> NotCompanyWords = new(StringComparer.OrdinalIgnoreCase)
            { "and", "or", "for", "the", "of", "to", "from", "your", "you", "our", "taking", "thanks", "thank", "apology", "apologies", "update", "invitation",
              "registration", "register", "round", "results", "result", "reminder", "important", "regarding", "re", "fwd", "dear", "hello", "hi", "cir",
              "placement", "placements", "college", "university", "school", "campus", "team", "students", "student", "drive", "batch",
              "data", "science", "engineer", "engineering", "developer", "analyst", "intern", "internship", "role", "position", "opening", "opportunity", "job", "jobs", "graduate", "trainee", "associate", "fy27", "fy26", "2025", "2026", "2027" };

        /// <summary>Rejects sentence fragments, people and programmes mistaken for company names.</summary>
        public static bool IsPlausibleCompany(string name)
        {
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length is 0 or > 4 || name.Length < 2) return false;
            if (words.Any(w => NotCompanyWords.Contains(w.Trim('.', ',', '!', ':', '\''))) ) return false;
            if (!char.IsUpper(words[0][0]) && !char.IsDigit(words[0][0])) return false;
            if (Regex.IsMatch(name, @"(?i)summer\s?school|hackathon|webinar|contest|challenge|bootcamp|workshop|course")) return false;
            return !Platforms.Contains(name.Replace(" ", ""));
        }

        private static readonly HashSet<string> Platforms = new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail", "googlemail", "outlook", "hotmail", "yahoo", "greenhouse", "lever", "myworkday", "myworkdayjobs", "workday", "smartrecruiters",
            "ashbyhq", "icims", "taleo", "successfactors", "jobvite", "hackerrank", "hackerearth", "codility", "codesignal", "mettl", "linkedin",
            "naukri", "indeed", "unstop", "superset", "internshala", "wellfound", "glassdoor", "instahyre", "cutshort", "hirist", "freshteam",
            "zohorecruit", "keka", "darwinbox", "eightfold", "phenom", "oraclecloud", "recruitee", "breezy", "applytojob", "bamboohr", "amcat", "aon",
        };

        private static readonly Regex GenericName = R(@"\b(no.?reply|do.?not.?reply|careers?|jobs?|recruit(ing|ment|er)?|talent( acquisition)?|acquisition|hiring|team|hr|people|notifications?|alerts?|university|campus|via .*|@.*)\b");

        public static MailClassification? Classify(string subject, string fromName, string fromAddress, string preview)
        {
            var text = $"{subject}\n{preview}";
            if (Noise.IsMatch(subject) || SenderNoise.IsMatch($"{fromName} {fromAddress}")) return null;
            var domain = fromAddress.Split('@').LastOrDefault()?.ToLowerInvariant() ?? string.Empty;
            // College / placement cell mail is only used for real test or interview rounds (see below).
            bool campus = IsCampusSender(fromName, fromAddress);
            bool personal = PersonalDomains.Contains(domain);
            Stage? stage = null;
            foreach (var (s, rx) in Rules)
                if (rx.IsMatch(text)) { stage = s; break; }
            if (stage == null) return null;
            if (campus)
            {
                // Announcements and registrations aren't applications; tests, interviews and results are.
                if (stage is not (Stage.OA or Stage.Interview or Stage.Offer)) return null;
                // For campus mail the preview is the full body: labelled fields first ("Company: X"), then the subject, then prose.
                var cc = CampusCompany(preview, labelsOnly: true) ?? CampusCompany(subject) ?? CampusCompany(preview);
                return cc != null && IsPlausibleCompany(cc) ? new MailClassification(cc, stage.Value, true, ExtractRole(subject, preview)) : null;
            }

            // People writing from personal webmail aren't companies; only trust an explicit "application to X" then.
            var company = CompanyFromSubject(subject) ?? (personal ? null : CompanyFromName(fromName) ?? CompanyFromDomain(fromAddress)) ?? CompanyFromSubject(preview);
            return company == null || !IsPlausibleCompany(company) ? null : new MailClassification(company, stage.Value, false, ExtractRole(subject, preview));
        }

        private static readonly Regex[] SubjectCompany =
        {
            R(@"\b(?:application|applying|interest|candidacy)\s+(?:to|at|with|in)\s+(?<c>[A-Z][\w&.'\- ]{1,40}?)(?=\s*(?:[!.,:;|()\-–—]|$|\s+for\s|\s+-\s))", false),
            R(@"\b(?:role|position|opportunity|internship|job)\s+at\s+(?<c>[A-Z][\w&.'\- ]{1,40}?)(?=\s*(?:[!.,:;|()\-–—]|$))", false),
            R(@"\b(?:offer|interview|assessment|test|letter|update|invitation)\s+(?:from|with|at)\s+(?<c>[A-Z][\w&.'\- ]{1,40}?)(?=\s*(?:[!.,:;|()\-–—]|$))", false),
        };

        private static readonly HashSet<string> StageWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "online", "coding", "interview", "assessment", "invitation", "invite", "application", "offer", "letter", "update",
            "test", "hiring", "recruiting", "recruitment", "careers", "career", "sde", "software", "intern", "internship", "congratulations",
            "drive", "campus", "placement", "shortlist", "shortlisted", "round", "results", "schedule", "registration",
        };

        private static string? CompanyFromSubject(string subject)
        {
            foreach (var rx in SubjectCompany)
            {
                var m = rx.Match(subject);
                if (!m.Success) continue;
                // "Microsoft Online Assessment" → "Microsoft": cut at the first stage word
                var raw = m.Groups["c"].Value;
                var words = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeWhile(w => !StageWords.Contains(w.Trim('.', ',', '!', ':'))).ToArray();
                var c = Clean(string.Join(' ', words));
                if (c != null && !Platforms.Contains(c.Replace(" ", "")) && !Regex.IsMatch(c, @"^(your|the|our|a|an|this|we|thank|application|update|re|fwd)\b", RegexOptions.IgnoreCase))
                    return c;
            }
            return null;
        }

        private static readonly Regex[] CampusPatterns =
        {
            R(@"^(?:(?:re|fwd?)\s*:\s*)*(?<c>[A-Z][\w&.'\- ]{1,30}?)\s*(?:\||:|–|—|-|\()", false),
            R(@"\b(?:for|of|by|from|with)\s+(?<c>[A-Z][\w&.'\-]{1,20}(?:\s[A-Z][\w&.'\-]{1,20}){0,2})(?=\s*(?:[!.,:;|()\-–—]|$|\s+(?:online|test|drive|recruit|interview|assessment|campus|placement|hiring|round)))", false),
            R(@"(?<c>[A-Z][\w&.'\-]{1,20}(?:\s[A-Z][\w&.'\-]{1,20}){0,2})\s+(?:online test|online assessment|test|drive|recruitment|campus|hiring|assessment|interview)", false),
        };

        private static readonly Regex[] CampusLabelPatterns =
        {
            R(@"(?im)^[\s*•\-]*(?:name of the\s+)?(?:company|organi[sz]ation|employer|recruiter|firm)(?:\s+name)?\s*[:\-–]\s*(?<c>[A-Z][\w&.'\- ]{1,40}?)\s*$", false),
            R(@"(?<c>[A-Z][\w&.'\-]{1,20}(?:\s[A-Z][\w&.'\-]{1,20}){0,2})\s+(?:is visiting|will be visiting|is conducting|has scheduled|is hiring|has shortlisted)", false),
            R(@"\b(?:drive|recruitment|hiring|test|assessment|interviews?)\s+(?:by|of|for|from|with)\s+(?:M/s\.?\s+)?(?<c>[A-Z][\w&.'\-]{1,20}(?:\s[A-Z][\w&.'\-]{1,20}){0,2})", false),
        };

        private static string? CampusCompany(string text, bool labelsOnly = false)
        {
            if (labelsOnly)
            {
                foreach (var rx in CampusLabelPatterns)
                {
                    var lm = rx.Match(text);
                    if (!lm.Success) continue;
                    var words = lm.Groups["c"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeWhile(w => !StageWords.Contains(w.Trim('.', ',', '!', ':'))).ToArray();
                    var lc = Clean(string.Join(' ', words));
                    if (lc != null && IsPlausibleCompany(lc)) return lc;
                }
                return null;
            }
            var subject = text.Length > 300 ? text[..300] : text;
            foreach (var rx in CampusPatterns)
            {
                var m = rx.Match(subject);
                if (!m.Success) continue;
                var words = m.Groups["c"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).TakeWhile(w => !StageWords.Contains(w.Trim('.', ',', '!', ':'))).ToArray();
                var c = Clean(string.Join(' ', words));
                if (c != null && IsPlausibleCompany(c)) return c;
            }
            return null;
        }

        private static string? CompanyFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var cleaned = GenericName.Replace(name, " ");
            cleaned = Regex.Replace(cleaned, @"[^\w&.' ]", " ");
            var c = Clean(cleaned);
            return c == null || Platforms.Contains(c.Replace(" ", "")) ? null : c;
        }

        private static string? CompanyFromDomain(string address)
        {
            var domain = address.Split('@').LastOrDefault() ?? string.Empty;
            var parts = domain.ToLowerInvariant().Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return null;
            // amazon.com → amazon; mail.google.com → google; tcs.co.in → tcs
            var core = parts.Length >= 3 && parts[^2].Length <= 3 ? parts[^3] : parts[^2];
            core = core.Split('-')[0];   // greenhouse-mail → greenhouse
            core = Regex.Replace(core, @"(careers|jobs|recruit(ing)?|talent|mail|hire|hiring|corp|inc|group|global|tech|hcm|hr|ats)$", "");
            if (core.Length < 2 || Platforms.Contains(core)) return null;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(core);
        }

        private static string? Clean(string raw)
        {
            var c = Regex.Replace(raw, @"\s+", " ").Trim(' ', '.', '-', '\'', '&');
            c = Regex.Replace(c, @"\b(Inc|LLC|Ltd|Limited|Pvt|Private|Corp|Corporation|Technologies|Software|India|Group)\.?$", "", RegexOptions.IgnoreCase).Trim(' ', ',', '.');
            return c.Length is >= 2 and <= 40 ? c : null;
        }

        private static string Normalize(string company) =>
            Regex.Replace(company.ToLowerInvariant(), @"\b(inc|llc|ltd|limited|pvt|private|corp|technologies|software|india|group)\b|[^a-z0-9]", "");

        private static Regex R(string pattern, bool ignoreCase = true) =>
            new(pattern, (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
