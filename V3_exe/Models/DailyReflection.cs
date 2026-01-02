using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Stores daily morning and evening reflections/check-ins.
    /// Morning: "What would make today successful?"
    /// Evening: "Did today move you forward?"
    /// </summary>
    public class DailyReflection : INotifyPropertyChanged
    {
        private DateTime _date;
        private string _morningReflection = string.Empty;
        private string _eveningReflection = string.Empty;
        private DateTime _createdAt = DateTime.Now;
        private DateTime _updatedAt = DateTime.Now;

        /// <summary>
        /// The date this reflection is for
        /// </summary>
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Morning check-in: "What would make today successful?"
        /// </summary>
        public string MorningReflection
        {
            get => _morningReflection;
            set { _morningReflection = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasMorning)); }
        }

        /// <summary>
        /// Evening check-in: "Did today move you forward?"
        /// </summary>
        public string EveningReflection
        {
            get => _eveningReflection;
            set { _eveningReflection = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasEvening)); }
        }

        /// <summary>
        /// When this reflection was created
        /// </summary>
        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// When this reflection was last updated
        /// </summary>
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set { _updatedAt = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Whether a morning reflection exists
        /// </summary>
        public bool HasMorning => !string.IsNullOrWhiteSpace(MorningReflection);

        /// <summary>
        /// Whether an evening reflection exists
        /// </summary>
        public bool HasEvening => !string.IsNullOrWhiteSpace(EveningReflection);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
