using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    public enum ProblemDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public enum ProblemStatus
    {
        Unsolved,
        Solving,
        Solved,
        NeedsRevision
    }

    public enum DsaTopic
    {
        ArraysAndHashing,
        TwoPointers,
        SlidingWindow,
        StackAndQueue,
        BinarySearch,
        LinkedList,
        TreesAndBST,
        HeapsAndPriorityQueue,
        Backtracking,
        Graphs,
        DynamicProgramming,
        GreedyAndIntervals,
        BitManipulation,
        SystemDesignLLD
    }

    /// <summary>
    /// Represents a Data Structures & Algorithms problem with spaced repetition metadata.
    /// </summary>
    public class DsaProblem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _title = string.Empty;
        private ProblemDifficulty _difficulty = ProblemDifficulty.Medium;
        private DsaTopic _topic = DsaTopic.ArraysAndHashing;
        private string _pattern = string.Empty;
        private string _leetCodeUrl = string.Empty;
        private ProblemStatus _status = ProblemStatus.Unsolved;
        private string _timeComplexity = "O(n)";
        private string _spaceComplexity = "O(1)";
        private string _intuitionNotes = string.Empty;
        private string _codeSnippet = string.Empty;
        private int _confidenceLevel = 3; // 1 to 5
        private DateTime? _lastSolvedDate;
        private DateTime? _nextReviewDate;
        private int _reviewCount;
        private bool _isBookmarked;
        private string _companyTags = string.Empty;
        private bool _isExpanded;

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

        public ProblemDifficulty Difficulty
        {
            get => _difficulty;
            set { _difficulty = value; OnPropertyChanged(); OnPropertyChanged(nameof(DifficultyColor)); OnPropertyChanged(nameof(DifficultyDisplay)); }
        }

        public DsaTopic Topic
        {
            get => _topic;
            set { _topic = value; OnPropertyChanged(); OnPropertyChanged(nameof(TopicDisplay)); }
        }

        public string Pattern
        {
            get => _pattern;
            set { _pattern = value; OnPropertyChanged(); }
        }

        public string LeetCodeUrl
        {
            get => _leetCodeUrl;
            set { _leetCodeUrl = value; OnPropertyChanged(); }
        }

        public ProblemStatus Status
        {
            get => _status;
            set 
            { 
                _status = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(IsSolved));
                OnPropertyChanged(nameof(StatusDisplay));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(IsDueForReview));
            }
        }

        public bool IsSolved => _status == ProblemStatus.Solved;

        public string TimeComplexity
        {
            get => _timeComplexity;
            set { _timeComplexity = value; OnPropertyChanged(); }
        }

        public string SpaceComplexity
        {
            get => _spaceComplexity;
            set { _spaceComplexity = value; OnPropertyChanged(); }
        }

        public string IntuitionNotes
        {
            get => _intuitionNotes;
            set { _intuitionNotes = value; OnPropertyChanged(); }
        }

        public string CodeSnippet
        {
            get => _codeSnippet;
            set { _codeSnippet = value; OnPropertyChanged(); }
        }

        public int ConfidenceLevel
        {
            get => _confidenceLevel;
            set { _confidenceLevel = Math.Clamp(value, 1, 5); OnPropertyChanged(); OnPropertyChanged(nameof(ConfidenceStars)); }
        }

        public DateTime? LastSolvedDate
        {
            get => _lastSolvedDate;
            set { _lastSolvedDate = value; OnPropertyChanged(); }
        }

        public DateTime? NextReviewDate
        {
            get => _nextReviewDate;
            set { _nextReviewDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsDueForReview)); OnPropertyChanged(nameof(ReviewDueText)); }
        }

        public int ReviewCount
        {
            get => _reviewCount;
            set { _reviewCount = value; OnPropertyChanged(); }
        }

        public bool IsBookmarked
        {
            get => _isBookmarked;
            set { _isBookmarked = value; OnPropertyChanged(); }
        }

        public string CompanyTags
        {
            get => _companyTags;
            set { _companyTags = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        // Computed Helpers
        public bool IsDueForReview => NextReviewDate.HasValue && NextReviewDate.Value.Date <= DateTime.Today && Status == ProblemStatus.Solved;

        public string ReviewDueText
        {
            get
            {
                if (!NextReviewDate.HasValue) return "No review scheduled";
                var days = (NextReviewDate.Value.Date - DateTime.Today).Days;
                if (days < 0) return $"Overdue by {Math.Abs(days)}d";
                if (days == 0) return "Due Today 🎯";
                if (days == 1) return "Due Tomorrow";
                return $"Due in {days}d";
            }
        }

        public string ConfidenceStars => ConfidenceLevel switch
        {
            1 => "★☆☆☆☆ (Weak)",
            2 => "★★☆☆☆ (Developing)",
            3 => "★★★☆☆ (Comfortable)",
            4 => "★★★★☆ (Strong)",
            5 => "★★★★★ (Mastered)",
            _ => "★★★☆☆"
        };

        public string DifficultyDisplay => Difficulty switch
        {
            ProblemDifficulty.Easy => "Easy",
            ProblemDifficulty.Medium => "Medium",
            ProblemDifficulty.Hard => "Hard",
            _ => "Medium"
        };

        public string DifficultyColor => Difficulty switch
        {
            ProblemDifficulty.Easy => "#4ADE80",   // Green
            ProblemDifficulty.Medium => "#FBBF24", // Yellow/Amber
            ProblemDifficulty.Hard => "#EF4444",   // Red
            _ => "#D4AF37"
        };

        public string StatusDisplay => Status switch
        {
            ProblemStatus.Unsolved => "Unsolved",
            ProblemStatus.Solving => "In Progress",
            ProblemStatus.Solved => "Solved",
            ProblemStatus.NeedsRevision => "Needs Review",
            _ => "Unsolved"
        };

        public string StatusColor => Status switch
        {
            ProblemStatus.Unsolved => "#606068",
            ProblemStatus.Solving => "#38BDF8",     // Sky Blue
            ProblemStatus.Solved => "#4ADE80",      // Emerald Green
            ProblemStatus.NeedsRevision => "#FB923C",// Orange
            _ => "#606068"
        };

        public string TopicDisplay => Topic switch
        {
            DsaTopic.ArraysAndHashing => "Arrays & Hashing",
            DsaTopic.TwoPointers => "Two Pointers",
            DsaTopic.SlidingWindow => "Sliding Window",
            DsaTopic.StackAndQueue => "Stack & Queue",
            DsaTopic.BinarySearch => "Binary Search",
            DsaTopic.LinkedList => "Linked List",
            DsaTopic.TreesAndBST => "Trees & BST",
            DsaTopic.HeapsAndPriorityQueue => "Heap / Priority Queue",
            DsaTopic.Backtracking => "Backtracking",
            DsaTopic.Graphs => "Graphs & Topological Sort",
            DsaTopic.DynamicProgramming => "Dynamic Programming",
            DsaTopic.GreedyAndIntervals => "Greedy & Intervals",
            DsaTopic.BitManipulation => "Bit Manipulation & Math",
            DsaTopic.SystemDesignLLD => "System Design / LLD",
            _ => Topic.ToString()
        };

        /// <summary>
        /// Update spaced repetition interval based on review feedback.
        /// </summary>
        public void RecordReview(int confidenceRating)
        {
            ConfidenceLevel = confidenceRating;
            ReviewCount++;
            LastSolvedDate = DateTime.Now;
            Status = ProblemStatus.Solved;

            // Spaced repetition schedule based on confidence:
            // Rating 1-2 (Hard): review tomorrow
            // Rating 3 (Good): review in 3-4 days
            // Rating 4 (Strong): review in 7-10 days
            // Rating 5 (Mastered): review in 18-24 days
            int daysToAdd = confidenceRating switch
            {
                1 => 1,
                2 => 2,
                3 => Math.Min(3 + ReviewCount, 7),
                4 => Math.Min(7 + (ReviewCount * 3), 14),
                5 => Math.Min(18 + (ReviewCount * 5), 30),
                _ => 3
            };

            NextReviewDate = DateTime.Today.AddDays(daysToAdd);
            OnPropertyChanged(nameof(ReviewDueText));
            OnPropertyChanged(nameof(IsDueForReview));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
