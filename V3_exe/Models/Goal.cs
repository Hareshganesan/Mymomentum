using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Represents a goal with progress tracking (e.g., "Finish Java", "Workout").
    /// Progress is calculated based on current/target values.
    /// </summary>
    public class Goal : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = string.Empty;
        private int _currentProgress;
        private int _targetProgress = 100;
        private bool _isCompleted;
        private DateTime _createdAt = DateTime.Now;

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

        public int CurrentProgress
        {
            get => _currentProgress;
            set 
            { 
                _currentProgress = Math.Max(0, Math.Min(value, _targetProgress));
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressColor));
                
                // Auto-complete when progress reaches target
                if (_currentProgress >= _targetProgress)
                    IsCompleted = true;
            }
        }

        public int TargetProgress
        {
            get => _targetProgress;
            set 
            { 
                _targetProgress = Math.Max(1, value);
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressColor)); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Progress as percentage (0-100)
        /// </summary>
        public double ProgressPercentage => TargetProgress > 0 ? (double)CurrentProgress / TargetProgress * 100 : 0;

        /// <summary>
        /// Color based on progress:
        /// Green = 70%+, Yellow = 30-70%, Red = <30%
        /// </summary>
        public string ProgressColor
        {
            get
            {
                if (IsCompleted) return "Green";
                if (ProgressPercentage >= 70) return "Green";
                if (ProgressPercentage >= 30) return "Yellow";
                return "Red";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
