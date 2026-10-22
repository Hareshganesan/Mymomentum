using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    public enum CsSubject
    {
        OperatingSystems,
        DBMS,
        ComputerNetworks,
        OOPAndDesignPatterns,
        SystemDesignBasics
    }

    /// <summary>
    /// Represents a high-yield Core CS interview topic or flashcard.
    /// </summary>
    public class CoreCsTopic : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private CsSubject _subject = CsSubject.OperatingSystems;
        private string _question = string.Empty;
        private string _shortAnswer = string.Empty;
        private string _detailedExplanation = string.Empty;
        private string _highYieldKeywords = string.Empty;
        private bool _isMastered;
        private int _reviewCount;
        private DateTime? _lastReviewedAt;
        private string _frequency = "Very High"; // Very High, High, Medium
        private bool _isFlipped; // Show answer state

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public CsSubject Subject
        {
            get => _subject;
            set { _subject = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubjectDisplay)); OnPropertyChanged(nameof(SubjectColor)); }
        }

        public string Question
        {
            get => _question;
            set { _question = value; OnPropertyChanged(); }
        }

        public string ShortAnswer
        {
            get => _shortAnswer;
            set { _shortAnswer = value; OnPropertyChanged(); }
        }

        public string DetailedExplanation
        {
            get => _detailedExplanation;
            set { _detailedExplanation = value; OnPropertyChanged(); }
        }

        public string HighYieldKeywords
        {
            get => _highYieldKeywords;
            set { _highYieldKeywords = value; OnPropertyChanged(); }
        }

        public bool IsMastered
        {
            get => _isMastered;
            set { _isMastered = value; OnPropertyChanged(); OnPropertyChanged(nameof(MasteredDisplay)); }
        }

        public int ReviewCount
        {
            get => _reviewCount;
            set { _reviewCount = value; OnPropertyChanged(); }
        }

        public DateTime? LastReviewedAt
        {
            get => _lastReviewedAt;
            set { _lastReviewedAt = value; OnPropertyChanged(); }
        }

        public string Frequency
        {
            get => _frequency;
            set { _frequency = value; OnPropertyChanged(); }
        }

        public bool IsFlipped
        {
            get => _isFlipped;
            set { _isFlipped = value; OnPropertyChanged(); }
        }

        public string MasteredDisplay => IsMastered ? "Mastered ✓" : "Needs Review";

        public string SubjectDisplay => Subject switch
        {
            CsSubject.OperatingSystems => "Operating Systems",
            CsSubject.DBMS => "DBMS & SQL",
            CsSubject.ComputerNetworks => "Computer Networks",
            CsSubject.OOPAndDesignPatterns => "OOP & Design Patterns",
            CsSubject.SystemDesignBasics => "System Design / LLD",
            _ => Subject.ToString()
        };

        public string SubjectEmoji => Subject switch
        {
            CsSubject.OperatingSystems => "🖥️",
            CsSubject.DBMS => "🗄️",
            CsSubject.ComputerNetworks => "🌐",
            CsSubject.OOPAndDesignPatterns => "🧱",
            CsSubject.SystemDesignBasics => "🏗️",
            _ => "📚"
        };

        public string SubjectColor => Subject switch
        {
            CsSubject.OperatingSystems => "#38BDF8",       // Sky Blue
            CsSubject.DBMS => "#A78BFA",                   // Purple
            CsSubject.ComputerNetworks => "#F472B6",       // Pink
            CsSubject.OOPAndDesignPatterns => "#34D399",   // Emerald
            CsSubject.SystemDesignBasics => "#FBBF24",     // Amber
            _ => "#D4AF37"
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
