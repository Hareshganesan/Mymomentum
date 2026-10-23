namespace Momentum.Models
{
    public enum Difficulty { Easy, Medium, Hard }

    public enum ProblemStatus { Todo, Attempted, Solved }

    /// <summary>Recall grades used by both problem and flashcard reviews (Anki-style).</summary>
    public enum Grade { Again = 1, Hard = 2, Good = 3, Easy = 4 }

    public enum Stage { Wishlist, Applied, OA, Interview, HR, Offer, Rejected }

    /// <summary>Everything Momentum persists. Static content (problem sheet, CS bank) lives in code; only progress is saved.</summary>
    public class AppState
    {
        public int Version { get; set; } = 4;
        public Profile Profile { get; set; } = new();

        public Dictionary<string, ProblemProgress> Problems { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<CustomProblem> CustomProblems { get; set; } = new();

        public Dictionary<string, CardProgress> Cards { get; set; } = new();
        public List<CustomCard> CustomCards { get; set; } = new();

        public List<JobApplication> Applications { get; set; } = new();
        public List<Story> Stories { get; set; } = new();
        public List<MockRecord> Mocks { get; set; } = new();
        public List<CustomTask> Tasks { get; set; } = new();

        /// <summary>yyyy-MM-dd → number of meaningful actions (solves, reviews, mocks) logged in the app.</summary>
        public Dictionary<string, int> Activity { get; set; } = new();

        public DayPlan? Plan { get; set; }
        public LeetCodeSnapshot? LeetCode { get; set; }
        public bool MigratedFromV3 { get; set; }
        /// <summary>Message-IDs already processed by inbox sync, so re-syncs never double-count.</summary>
        public HashSet<string> SeenMail { get; set; } = new();
    }

    public class Profile
    {
        public bool Onboarded { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? TargetDate { get; set; }
        public int DailyMinutes { get; set; } = 90;
        public string Language { get; set; } = "C++";
        public string TargetCompanies { get; set; } = string.Empty;
        public string LeetCodeUsername { get; set; } = string.Empty;
        public string LeetCodeSession { get; set; } = string.Empty;
        public bool DarkTheme { get; set; } = true;
        public string PitchPresent { get; set; } = string.Empty;
        public string PitchPast { get; set; } = string.Empty;
        public string PitchFuture { get; set; } = string.Empty;

        // Inbox sync (IMAP). The app password is DPAPI-encrypted for this Windows user.
        public string MailAddress { get; set; } = string.Empty;
        public string MailHost { get; set; } = string.Empty;
        public string MailSecret { get; set; } = string.Empty;
        public DateTime? MailLastSync { get; set; }
        public int MailRulesVersion { get; set; }

        // Google sign-in (read-only Gmail). Secret and refresh token are DPAPI-encrypted.
        public string GoogleClientId { get; set; } = string.Empty;
        public string GoogleClientSecret { get; set; } = string.Empty;
        public string GoogleToken { get; set; } = string.Empty;
        public string GoogleEmail { get; set; } = string.Empty;
    }

    public class SrsState
    {
        public double Ease { get; set; } = 2.5;
        public int Interval { get; set; }
        public int Reps { get; set; }
        public int Lapses { get; set; }
        public DateTime? Due { get; set; }
        public DateTime? Last { get; set; }
        public Grade? LastGrade { get; set; }
    }

    public class ProblemProgress
    {
        public ProblemStatus Status { get; set; }
        public DateTime? SolvedOn { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Mistakes { get; set; } = string.Empty;
        public bool Starred { get; set; }
        public bool FromLeetCode { get; set; }
        public SrsState Srs { get; set; } = new();
    }

    public class CustomProblem
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; } = Difficulty.Medium;
        public string Pattern { get; set; } = "arrays";
        public string Url { get; set; } = string.Empty;
        public string Companies { get; set; } = string.Empty;
    }

    public class CardProgress
    {
        public bool Starred { get; set; }
        public SrsState Srs { get; set; } = new();
    }

    public class CustomCard
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Subject { get; set; } = "OS";
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class JobApplication
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Company { get; set; } = string.Empty;
        public string Role { get; set; } = "SDE";
        public string Ctc { get; set; } = string.Empty;
        public Stage Stage { get; set; } = Stage.Applied;
        public string NextEventTitle { get; set; } = string.Empty;
        public DateTime? NextEventDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<MailEvent> Emails { get; set; } = new();
        /// <summary>"Email" (from inbox), "Campus" (placement cell mail) or empty for manual entries.</summary>
        public string Source { get; set; } = string.Empty;
        /// <summary>Restoring from the archive restarts the no-response clock.</summary>
        public DateTime? RestoredAt { get; set; }
        /// <summary>Furthest stage reached before a rejection (e.g. HR / final for "rejected after the final interview").</summary>
        public Stage? ReachedStage { get; set; }

        /// <summary>Changes stage, remembering how far it got when it ends in a rejection.</summary>
        public void MoveTo(Stage next)
        {
            if (next == Stage.Rejected && Stage != Stage.Rejected)
                ReachedStage = Stage <= Stage.Wishlist ? Stage.Applied : ReachedStage is { } r && r > Stage ? r : Stage;
            Stage = next;
        }

        /// <summary>The stage to show on the journey: current stage, or the furthest reached if rejected.</summary>
        public Stage ProgressStage => Stage == Stage.Rejected ? ReachedStage ?? Stage.Applied : Stage;
    }

    /// <summary>A job email that was matched to an application.</summary>
    public class MailEvent
    {
        public DateTime Date { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public Stage? Stage { get; set; }
    }

    public class Story
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Situation { get; set; } = string.Empty;
        public string Task { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public List<string> Themes { get; set; } = new();
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class MockRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime Date { get; set; } = DateTime.Now;
        public string ProblemSlug { get; set; } = string.Empty;
        public string ProblemTitle { get; set; } = string.Empty;
        public Difficulty Difficulty { get; set; }
        public int Minutes { get; set; }
        public int TargetMinutes { get; set; }
        public bool Solved { get; set; }
        public int HintsUsed { get; set; }
        public int Communication { get; set; }
        public int ProblemSolving { get; set; }
        public int CodeQuality { get; set; }
        public int Testing { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class CustomTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public bool Done { get; set; }
    }

    public enum PlanKind { Review, NewProblem, Cards, Event, Task }

    public class DayPlan
    {
        public string Date { get; set; } = string.Empty;
        public List<PlanItem> Items { get; set; } = new();
    }

    public class PlanItem
    {
        public PlanKind Kind { get; set; }
        public string RefId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public int Minutes { get; set; }
        public bool Done { get; set; }
    }

    public class LeetCodeSnapshot
    {
        public DateTime SyncedAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Easy { get; set; }
        public int Medium { get; set; }
        public int Hard { get; set; }
        public int Ranking { get; set; }
        public int Streak { get; set; }
        public int ActiveDays { get; set; }
        public Dictionary<string, int> Calendar { get; set; } = new();
        public Dictionary<string, int> Tags { get; set; } = new();
    }
}
