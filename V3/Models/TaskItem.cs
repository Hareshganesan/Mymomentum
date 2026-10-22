using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Energy levels for tasks - replaces traditional priority
    /// Deep = requires focus, Quick = fast wins, Low = low energy tasks
    /// </summary>
    public enum EnergyTag
    {
        Quick,      // Fast wins, low effort
        Deep,       // Requires focus and concentration
        LowEnergy   // Can do when tired
    }

    /// <summary>
    /// Represents a single task item with energy tags and highlight support.
    /// Implements INotifyPropertyChanged for UI binding.
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = string.Empty;
        private bool _isCompleted;
        private DateTime? _dueDate;
        private DateTime _createdAt = DateTime.Now;
        private bool _isAnimating;
        private EnergyTag _energyTag = EnergyTag.Quick;
        private bool _isDailyHighlight;
        private DateTime? _completedAt;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set 
            { 
                _isCompleted = value; 
                if (value) _completedAt = DateTime.Now;
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StatusColor)); 
            }
        }

        public DateTime? DueDate
        {
            get => _dueDate;
            set { _dueDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(IsOverdue)); OnPropertyChanged(nameof(IsDueToday)); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When the task was completed
        /// </summary>
        public DateTime? CompletedAt
        {
            get => _completedAt;
            set { _completedAt = value; OnPropertyChanged(); OnPropertyChanged(nameof(CompletedAtFormatted)); }
        }

        /// <summary>
        /// Formatted completion time for display
        /// </summary>
        public string CompletedAtFormatted
        {
            get
            {
                if (!CompletedAt.HasValue) return string.Empty;
                
                var timespan = DateTime.Now - CompletedAt.Value;
                if (timespan.TotalMinutes < 1) return "Just now";
                if (timespan.TotalHours < 1) return $"{(int)timespan.TotalMinutes}m ago";
                if (timespan.TotalDays < 1) return $"{(int)timespan.TotalHours}h ago";
                if (timespan.TotalDays < 7) return $"{(int)timespan.TotalDays}d ago";
                
                return CompletedAt.Value.ToString("MMM dd");
            }
        }

        /// <summary>
        /// Energy tag: Quick, Deep, or LowEnergy
        /// </summary>
        public EnergyTag EnergyTag
        {
            get => _energyTag;
            set { _energyTag = value; OnPropertyChanged(); OnPropertyChanged(nameof(EnergyEmoji)); OnPropertyChanged(nameof(EnergyLabel)); }
        }

        /// <summary>
        /// Emoji representation of energy tag
        /// </summary>
        public string EnergyEmoji => EnergyTag switch
        {
            EnergyTag.Deep => "Deep",
            EnergyTag.Quick => "Quick",
            EnergyTag.LowEnergy => "Light",
            _ => "Standard"
        };

        /// <summary>
        /// Label for energy tag
        /// </summary>
        public string EnergyLabel => EnergyTag switch
        {
            EnergyTag.Deep => "Deep Work",
            EnergyTag.Quick => "Quick Win",
            EnergyTag.LowEnergy => "Low Energy",
            _ => "Quick Win"
        };

        /// <summary>
        /// Whether this task is today's highlighted "One Thing"
        /// </summary>
        public bool IsDailyHighlight
        {
            get => _isDailyHighlight;
            set { _isDailyHighlight = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Used to trigger animations in UI
        /// </summary>
        public bool IsAnimating
        {
            get => _isAnimating;
            set { _isAnimating = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Returns true if task is overdue (past due date and not completed)
        /// </summary>
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && !IsCompleted;

        /// <summary>
        /// Returns true if task is due today
        /// </summary>
        public bool IsDueToday => DueDate.HasValue && DueDate.Value.Date == DateTime.Today;

        /// <summary>
        /// Color indicator based on task status:
        /// Green = completed, Yellow = due today, Red = overdue, Default = normal
        /// </summary>
        public string StatusColor
        {
            get
            {
                if (IsCompleted) return "Green";
                if (IsOverdue) return "Red";
                if (IsDueToday) return "Yellow";
                return "Default";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
