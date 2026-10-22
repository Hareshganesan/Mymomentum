using System;
using System.Collections.Generic;

namespace MyTodo.Models
{
    /// <summary>
    /// Container for all app data, including tasks, goals, streaks,
    /// DSA problems, Core CS flashcards, and placement drives.
    /// Used for JSON serialization.
    /// </summary>
    public class AppData
    {
        public List<TaskItem> Tasks { get; set; } = new();
        public List<Goal> Goals { get; set; } = new();
        public int CurrentStreak { get; set; }
        public int BestStreak { get; set; }
        public int TotalTasksCompleted { get; set; }
        public DateTime? LastCompletedDate { get; set; }
        public bool IsDarkMode { get; set; } = true;
        
        /// <summary>
        /// Daily completion history (key = yyyy-MM-dd, value = completion score or activity count)
        /// </summary>
        public Dictionary<string, double> DailyCompletionHistory { get; set; } = new();

        // ===== Streak Protection =====
        public bool StreakPaused { get; set; }
        public int PausedStreakValue { get; set; }

        // ===== Daily Reflections =====
        public Dictionary<string, DailyReflection> Reflections { get; set; } = new();

        // ===== Daily Highlight =====
        public string? DailyHighlightTaskId { get; set; }

        // ===== DSA & Placement Vault =====
        public List<DsaProblem> DsaProblems { get; set; } = new();
        public List<CoreCsTopic> CoreCsQuestions { get; set; } = new();
        public List<CompanyApplication> CompanyApplications { get; set; } = new();
        public List<BehavioralQuestion> BehavioralQuestions { get; set; } = new();
        public List<MockSession> MockHistory { get; set; } = new();

        public int TotalMockSessions { get; set; }
        public int TotalCoreCsMastered { get; set; }
        public DateTime? LastActivityDate { get; set; }

        // ===== LeetCode Integration =====
        public string? LeetCodeUsername { get; set; }
        public string? LeetCodeSessionCookie { get; set; }
        public int LeetCodeTotalSolved { get; set; }
        public int LeetCodeEasySolved { get; set; }
        public int LeetCodeMediumSolved { get; set; }
        public int LeetCodeHardSolved { get; set; }
        public int LeetCodeRanking { get; set; }
        public DateTime? LeetCodeLastSyncedAt { get; set; }
    }
}
