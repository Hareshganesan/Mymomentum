using System;
using System.Collections.Generic;

namespace MyTodo.Models
{
    /// <summary>
    /// Container for all app data, used for JSON serialization.
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
        /// Weekly completion data - stores completion percentage for each day
        /// Key: Date string (yyyy-MM-dd), Value: completion percentage
        /// </summary>
        public Dictionary<string, double> DailyCompletionHistory { get; set; } = new();

        // ===== Streak Protection =====
        
        /// <summary>
        /// If true, streak is paused (user missed a day but can still save it)
        /// </summary>
        public bool StreakPaused { get; set; }

        /// <summary>
        /// The streak value that was saved when paused
        /// </summary>
        public int PausedStreakValue { get; set; }

        // ===== Daily Reflections =====
        
        /// <summary>
        /// Dictionary of daily morning/evening reflections (key = yyyy-MM-dd)
        /// </summary>
        public Dictionary<string, DailyReflection> Reflections { get; set; } = new();

        // ===== Daily Highlight =====
        
        /// <summary>
        /// ID of today's highlighted task ("One Thing")
        /// </summary>
        public string? DailyHighlightTaskId { get; set; }
    }
}

