using System;
using System.Collections.Generic;

namespace MyTodo.Models
{
    /// <summary>
    /// Represents parsed response data from LeetCode public API or authenticated session.
    /// </summary>
    public class LeetCodeProfileResult
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public int Ranking { get; set; }
        public int TotalSolved { get; set; }
        public int EasySolved { get; set; }
        public int MediumSolved { get; set; }
        public int HardSolved { get; set; }
        public HashSet<string> SolvedTitles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> SolvedSlugs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public DateTime SyncedAt { get; set; } = DateTime.Now;
    }
}
