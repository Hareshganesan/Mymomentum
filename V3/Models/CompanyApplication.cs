using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    public enum DriveStage
    {
        Wishlist,
        Applied,
        OnlineAssessment,
        TechnicalRound,
        HrRound,
        OfferReceived,
        Rejected
    }

    /// <summary>
    /// Represents a campus placement drive or off-campus job application in the Kanban pipeline.
    /// </summary>
    public class CompanyApplication : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _companyName = string.Empty;
        private string _role = "Software Development Engineer (SDE)";
        private string _packageCtc = string.Empty;
        private DriveStage _stage = DriveStage.Applied;
        private DateTime? _upcomingEventDate;
        private string _upcomingEventTitle = string.Empty;
        private string _location = "On-Campus / Bangalore";
        private string _focusTopics = string.Empty;
        private string _roundNotes = string.Empty;
        private DateTime _createdAt = DateTime.Now;
        private string _platformUrl = string.Empty;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string CompanyName
        {
            get => _companyName;
            set { _companyName = value; OnPropertyChanged(); }
        }

        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public string PackageCtc
        {
            get => _packageCtc;
            set { _packageCtc = value; OnPropertyChanged(); }
        }

        public DriveStage Stage
        {
            get => _stage;
            set 
            { 
                _stage = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(StageDisplay));
                OnPropertyChanged(nameof(StageColor));
                OnPropertyChanged(nameof(CanMoveForward));
                OnPropertyChanged(nameof(CanMoveBackward));
            }
        }

        public DateTime? UpcomingEventDate
        {
            get => _upcomingEventDate;
            set { _upcomingEventDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(EventCountdown)); }
        }

        public string UpcomingEventTitle
        {
            get => _upcomingEventTitle;
            set { _upcomingEventTitle = value; OnPropertyChanged(); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(); }
        }

        public string FocusTopics
        {
            get => _focusTopics;
            set { _focusTopics = value; OnPropertyChanged(); }
        }

        public string RoundNotes
        {
            get => _roundNotes;
            set { _roundNotes = value; OnPropertyChanged(); }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set { _createdAt = value; OnPropertyChanged(); }
        }

        public string PlatformUrl
        {
            get => _platformUrl;
            set { _platformUrl = value; OnPropertyChanged(); }
        }

        public string EventCountdown
        {
            get
            {
                if (!UpcomingEventDate.HasValue) return string.Empty;
                var days = (UpcomingEventDate.Value.Date - DateTime.Today).Days;
                if (days < 0) return "Passed";
                if (days == 0) return "TODAY! 🚀";
                if (days == 1) return "Tomorrow ⚡";
                return $"In {days} days";
            }
        }

        public string StageDisplay => Stage switch
        {
            DriveStage.Wishlist => "Wishlist",
            DriveStage.Applied => "Applied",
            DriveStage.OnlineAssessment => "Online Assessment (OA)",
            DriveStage.TechnicalRound => "Technical Interview",
            DriveStage.HrRound => "HR / Leadership Round",
            DriveStage.OfferReceived => "Offer Received 🎉",
            DriveStage.Rejected => "Archived",
            _ => Stage.ToString()
        };

        public string StageColor => Stage switch
        {
            DriveStage.Wishlist => "#94A3B8",        // Slate
            DriveStage.Applied => "#38BDF8",         // Sky Blue
            DriveStage.OnlineAssessment => "#FBBF24",// Amber
            DriveStage.TechnicalRound => "#A78BFA",  // Purple
            DriveStage.HrRound => "#EC4899",         // Pink
            DriveStage.OfferReceived => "#4ADE80",   // Green / Gold
            DriveStage.Rejected => "#64748B",        // Dark Slate
            _ => "#D4AF37"
        };

        public bool CanMoveForward => Stage != DriveStage.OfferReceived && Stage != DriveStage.Rejected;
        public bool CanMoveBackward => Stage != DriveStage.Wishlist;

        public void MoveNextStage()
        {
            if (Stage == DriveStage.Wishlist) Stage = DriveStage.Applied;
            else if (Stage == DriveStage.Applied) Stage = DriveStage.OnlineAssessment;
            else if (Stage == DriveStage.OnlineAssessment) Stage = DriveStage.TechnicalRound;
            else if (Stage == DriveStage.TechnicalRound) Stage = DriveStage.HrRound;
            else if (Stage == DriveStage.HrRound) Stage = DriveStage.OfferReceived;
        }

        public void MovePreviousStage()
        {
            if (Stage == DriveStage.OfferReceived || Stage == DriveStage.Rejected) Stage = DriveStage.HrRound;
            else if (Stage == DriveStage.HrRound) Stage = DriveStage.TechnicalRound;
            else if (Stage == DriveStage.TechnicalRound) Stage = DriveStage.OnlineAssessment;
            else if (Stage == DriveStage.OnlineAssessment) Stage = DriveStage.Applied;
            else if (Stage == DriveStage.Applied) Stage = DriveStage.Wishlist;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
