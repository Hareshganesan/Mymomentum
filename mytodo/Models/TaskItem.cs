using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Represents a single task item with optional due date.
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
            set { _isCompleted = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusColor)); }
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
