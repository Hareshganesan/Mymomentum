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
    }
}
