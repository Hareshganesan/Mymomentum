using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Records a timed Mock Online Assessment or Coding Sprint session.
    /// </summary>
    public class MockSession : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = string.Empty;
        private string _problemTitle = string.Empty;
        private int _targetMinutes = 45;
        private int _timeTakenSeconds;
        private DateTime _completedAt = DateTime.Now;
        private bool _passedAllTests = true;
        private string _language = "C++";
        private string _timeComplexityAchieved = "O(n)";
        private string _spaceComplexityAchieved = "O(1)";
        private string _notes = string.Empty;
        private string _code = string.Empty;

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

        public string ProblemTitle
        {
            get => _problemTitle;
            set { _problemTitle = value; OnPropertyChanged(); }
        }

        public int TargetMinutes
        {
            get => _targetMinutes;
            set { _targetMinutes = value; OnPropertyChanged(); }
        }

        public int TimeTakenSeconds
        {
            get => _timeTakenSeconds;
            set { _timeTakenSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimeTakenFormatted)); }
        }

        public DateTime CompletedAt
        {
            get => _completedAt;
            set { _completedAt = value; OnPropertyChanged(); }
        }

        public bool PassedAllTests
        {
            get => _passedAllTests;
            set { _passedAllTests = value; OnPropertyChanged(); }
        }

        public string Language
        {
            get => _language;
            set { _language = value; OnPropertyChanged(); }
        }

        public string TimeComplexityAchieved
        {
            get => _timeComplexityAchieved;
            set { _timeComplexityAchieved = value; OnPropertyChanged(); }
        }

        public string SpaceComplexityAchieved
        {
            get => _spaceComplexityAchieved;
            set { _spaceComplexityAchieved = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public string Code
        {
            get => _code;
            set { _code = value; OnPropertyChanged(); }
        }

        public string TimeTakenFormatted
        {
            get
            {
                var mins = TimeTakenSeconds / 60;
                var secs = TimeTakenSeconds % 60;
                return $"{mins}m {secs}s";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
