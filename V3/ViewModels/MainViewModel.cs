using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using MyTodo.Models;
using MyTodo.Services;

namespace MyTodo.ViewModels
{
    /// <summary>
    /// Available navigation pages in the Placement Cockpit
    /// </summary>
    public enum AppPage
    {
        Home,         // 🏠 Momentum & Daily Placement Cockpit
        DsaVault,     // 🧩 DSA Problem Bank, Pattern Matrix & Spaced Repetition
        CoreCs,       // 📚 Core CS & System Design Flashcards
        Companies,    // 💼 Placement Drive Kanban Pipeline
        MockArena,    // ⏱️ Mock OA & Timed Code Sprint Arena
        Behavioral,   // 👔 STAR Behavioral & HR Interview Vault
        Analytics,    // 📊 Placement Readiness Matrix & Heatmap
        Achievements, // 🏆 SDE Placement Badges
        History,      // 📖 History & Reflections
        Settings      // ⚙ Settings
    }

    /// <summary>
    /// Main ViewModel handling all DSA, Core CS, Placement Drive, and Momentum logic.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields - General & Momentum

        private string _newTaskTitle = string.Empty;
        private DateTime? _newTaskDueDate;
        private string _newGoalTitle = string.Empty;
        private bool _isDarkMode = true;
        private int _currentStreak;
        private int _bestStreak;
        private int _totalTasksCompleted;
        private DateTime? _lastCompletedDate;
        private bool _showConfetti;
        private string _motivationalMessage = string.Empty;
        private Dictionary<string, double> _dailyCompletionHistory = new();

        // Focus Mode / Pomodoro fields
        private bool _isFocusMode;
        private TaskItem? _focusTask;
        private int _pomodoroMinutes = 25;
        private int _pomodoroSeconds = 0;
        private bool _isPomodoroRunning;
        private DispatcherTimer? _pomodoroTimer;

        // Daily Highlight fields
        private TaskItem? _dailyHighlight;

        // Streak Protection fields
        private bool _streakPaused;
        private int _pausedStreakValue;

        // Check-in fields
        private bool _showMorningCheckIn;
        private bool _showEveningCheckIn;
        private string _morningReflection = string.Empty;
        private string _eveningReflection = string.Empty;
        private DailyReflection? _todayReflection;
        private Dictionary<string, DailyReflection> _reflections = new();

        // Energy Tag fields
        private EnergyTag _newTaskEnergy = EnergyTag.Deep;

        // Navigation fields
        private AppPage _currentPage = AppPage.Home;
        private bool _isFullScreen;

        #endregion

        #region Private Fields - DSA & Placement Vault

        // DSA Search & Filters
        private string _dsaSearchText = string.Empty;
        private string _selectedDsaTopicFilter = "All Topics";
        private string _selectedDsaDifficultyFilter = "All";
        private string _selectedDsaStatusFilter = "All";
        private bool _onlyDueForReviewFilter;

        // Add DSA Problem fields
        private string _newDsaTitle = string.Empty;
        private ProblemDifficulty _newDsaDifficulty = ProblemDifficulty.Medium;
        private DsaTopic _newDsaTopic = DsaTopic.ArraysAndHashing;
        private string _newDsaPattern = string.Empty;
        private string _newDsaUrl = string.Empty;
        private string _newDsaCompanyTags = string.Empty;

        // Core CS Fields
        private string _coreCsSearchText = string.Empty;
        private string _selectedCoreCsSubjectFilter = "All Subjects";

        // Company Application Fields
        private string _newCompanyName = string.Empty;
        private string _newCompanyRole = "Software Development Engineer (SDE-1)";
        private string _newCompanyPackage = "18 LPA";
        private DriveStage _newCompanyStage = DriveStage.Applied;
        private DateTime? _newCompanyDate = DateTime.Today.AddDays(7);
        private string _newCompanyEvent = "Online Assessment (OA)";
        private string _newCompanyFocus = "DP, Graphs, OS, SQL";

        // Mock Arena Fields
        private int _mockTargetMinutes = 45;
        private int _mockRemainingSeconds = 45 * 60;
        private bool _isMockRunning;
        private DispatcherTimer? _mockTimer;
        private DsaProblem? _currentMockProblem;
        private string _mockCodeScratchpad = "// Write your solution code or pseudocode here...\n#include <iostream>\nusing namespace std;\n\n";
        private string _mockNotesScratchpad = "- Edge cases checked:\n- Approach logic:\n- Time & Space target:";
        private string _mockLanguage = "C++";
        private string _mockTimeComplexity = "O(n)";
        private string _mockSpaceComplexity = "O(1)";
        private int _mockElapsedSeconds;

        #endregion

        #region Collections

        public ObservableCollection<TaskItem> Tasks { get; } = new();
        public ObservableCollection<Goal> Goals { get; } = new();
        public ObservableCollection<TaskItem> TodayTasks { get; } = new();
        public ObservableCollection<TaskItem> CompletedTasks { get; } = new();
        public ObservableCollection<BadgeInfo> AllBadges { get; } = new();

        // Placement Collections
        public ObservableCollection<DsaProblem> DsaProblems { get; } = new();
        public ObservableCollection<DsaProblem> FilteredDsaProblems { get; } = new();
        public ObservableCollection<DsaProblem> DueReviewProblems { get; } = new();

        public ObservableCollection<CoreCsTopic> CoreCsQuestions { get; } = new();
        public ObservableCollection<CoreCsTopic> FilteredCoreCsQuestions { get; } = new();

        public ObservableCollection<CompanyApplication> CompanyApplications { get; } = new();
        public ObservableCollection<BehavioralQuestion> BehavioralQuestions { get; } = new();
        public ObservableCollection<MockSession> MockHistory { get; } = new();

        #endregion

        #region Badge Info Class

        public class BadgeInfo
        {
            public string Emoji { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public bool IsUnlocked { get; set; }
        }

        #endregion

        #region Constructor

        public MainViewModel()
        {
            InitializeCommands();
            LoadData();
            UpdateTodayTasks();
            ApplyDsaFilter();
            ApplyCoreCsFilter();
            UpdateDueReviewProblems();
            UpdateMotivationalMessage();
            CheckStreakStatus();
            CheckDailyCheckInStatus();
            UpdateBadges();
            RecalculateReadinessScore();
        }

        #endregion

        #region Navigation Properties

        public AppPage CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHomePage));
                OnPropertyChanged(nameof(IsDsaVaultPage));
                OnPropertyChanged(nameof(IsCoreCsPage));
                OnPropertyChanged(nameof(IsCompaniesPage));
                OnPropertyChanged(nameof(IsMockArenaPage));
                OnPropertyChanged(nameof(IsBehavioralPage));
                OnPropertyChanged(nameof(IsAnalyticsPage));
                OnPropertyChanged(nameof(IsAchievementsPage));
                OnPropertyChanged(nameof(IsHistoryPage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(ShowRightPanel));
            }
        }

        public bool IsHomePage => CurrentPage == AppPage.Home;
        public bool IsDsaVaultPage => CurrentPage == AppPage.DsaVault;
        public bool IsCoreCsPage => CurrentPage == AppPage.CoreCs;
        public bool IsCompaniesPage => CurrentPage == AppPage.Companies;
        public bool IsMockArenaPage => CurrentPage == AppPage.MockArena;
        public bool IsBehavioralPage => CurrentPage == AppPage.Behavioral;
        public bool IsAnalyticsPage => CurrentPage == AppPage.Analytics;
        public bool IsAchievementsPage => CurrentPage == AppPage.Achievements;
        public bool IsHistoryPage => CurrentPage == AppPage.History;
        public bool IsSettingsPage => CurrentPage == AppPage.Settings;

        /// <summary>
        /// Only show the standard quick-add/goals right panel on Home page to give maximum workspace to tables/kanban.
        /// </summary>
        public bool ShowRightPanel => CurrentPage == AppPage.Home;

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set { _isFullScreen = value; OnPropertyChanged(); }
        }

        #endregion

        #region Placement Readiness & Matrix Properties

        public double PlacementReadinessScore
        {
            get
            {
                // DSA Score (up to 45 points): 30+ problems = full score
                int solvedCount = DsaProblems.Count(p => p.Status == ProblemStatus.Solved);
                double dsaRatio = Math.Min(1.0, (double)solvedCount / 30.0);
                double dsaPoints = dsaRatio * 45.0;

                // Core CS Score (up to 25 points): 12+ mastered = full score
                int csMastered = CoreCsQuestions.Count(q => q.IsMastered);
                double csRatio = Math.Min(1.0, (double)csMastered / 12.0);
                double csPoints = csRatio * 25.0;

                // Mock OA Score (up to 15 points): 3+ mock sessions = full score
                double mockRatio = Math.Min(1.0, (double)MockHistory.Count / 3.0);
                double mockPoints = mockRatio * 15.0;

                // Applications Score (up to 15 points): 4+ applications tracked = full score
                double appRatio = Math.Min(1.0, (double)CompanyApplications.Count / 4.0);
                double appPoints = appRatio * 15.0;

                return Math.Round(dsaPoints + csPoints + mockPoints + appPoints);
            }
        }

        public string ReadinessRank => PlacementReadinessScore switch
        {
            >= 85 => "Dream Offer Material 👑",
            >= 70 => "SDE Interview Ready 🚀",
            >= 50 => "OA Ready Contender ⚡",
            >= 25 => "Pattern Apprentice 🎯",
            _ => "Novice Coder 🐣"
        };

        public int TotalDsaSolved => DsaProblems.Count(p => p.Status == ProblemStatus.Solved);
        public int TotalDsaCount => DsaProblems.Count;
        public int EasyDsaSolved => DsaProblems.Count(p => p.Status == ProblemStatus.Solved && p.Difficulty == ProblemDifficulty.Easy);
        public int MediumDsaSolved => DsaProblems.Count(p => p.Status == ProblemStatus.Solved && p.Difficulty == ProblemDifficulty.Medium);
        public int HardDsaSolved => DsaProblems.Count(p => p.Status == ProblemStatus.Solved && p.Difficulty == ProblemDifficulty.Hard);

        public int TotalCoreCsMastered => CoreCsQuestions.Count(q => q.IsMastered);
        public int TotalCoreCsCount => CoreCsQuestions.Count;

        public int ActiveCompaniesCount => CompanyApplications.Count(a => a.Stage != DriveStage.Rejected && a.Stage != DriveStage.OfferReceived);
        public int OffersCount => CompanyApplications.Count(a => a.Stage == DriveStage.OfferReceived);

        public int DueReviewCount => DueReviewProblems.Count;

        public string UpcomingDriveAlert
        {
            get
            {
                var upcoming = CompanyApplications
                    .Where(a => a.UpcomingEventDate.HasValue && a.UpcomingEventDate.Value.Date >= DateTime.Today && a.Stage != DriveStage.Rejected)
                    .OrderBy(a => a.UpcomingEventDate!.Value)
                    .FirstOrDefault();

                if (upcoming != null && upcoming.UpcomingEventDate.HasValue)
                {
                    var days = (upcoming.UpcomingEventDate.Value.Date - DateTime.Today).Days;
                    if (days == 0) return $"⚡ {upcoming.CompanyName} {upcoming.UpcomingEventTitle} is TODAY!";
                    if (days == 1) return $"⚡ {upcoming.CompanyName} {upcoming.UpcomingEventTitle} is Tomorrow!";
                    return $"⚡ {upcoming.CompanyName} {upcoming.UpcomingEventTitle} in {days} days";
                }
                return "🎯 Consistent daily practice unlocks the dream offer!";
            }
        }

        public Dictionary<string, double> HeatmapActivityData => _dailyCompletionHistory;

        #endregion

        #region General & Task Properties

        public string NewTaskTitle
        {
            get => _newTaskTitle;
            set { _newTaskTitle = value; OnPropertyChanged(); }
        }

        public DateTime? NewTaskDueDate
        {
            get => _newTaskDueDate;
            set { _newTaskDueDate = value; OnPropertyChanged(); }
        }

        public string NewGoalTitle
        {
            get => _newGoalTitle;
            set { _newGoalTitle = value; OnPropertyChanged(); }
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set { _isDarkMode = value; OnPropertyChanged(); SaveData(); }
        }

        public int CurrentStreak
        {
            get => _currentStreak;
            set { _currentStreak = value; OnPropertyChanged(); OnPropertyChanged(nameof(StreakStatusText)); }
        }

        public int BestStreak
        {
            get => _bestStreak;
            set { _bestStreak = value; OnPropertyChanged(); }
        }

        public int TotalTasksCompleted
        {
            get => _totalTasksCompleted;
            set { _totalTasksCompleted = value; OnPropertyChanged(); }
        }

        public bool ShowConfetti
        {
            get => _showConfetti;
            set { _showConfetti = value; OnPropertyChanged(); }
        }

        public string MotivationalMessage
        {
            get => _motivationalMessage;
            set { _motivationalMessage = value; OnPropertyChanged(); }
        }

        public EnergyTag NewTaskEnergy
        {
            get => _newTaskEnergy;
            set { _newTaskEnergy = value; OnPropertyChanged(); OnPropertyChanged(nameof(NewTaskEnergyEmoji)); }
        }

        public string NewTaskEnergyEmoji => NewTaskEnergy switch
        {
            EnergyTag.Deep => "🧠",
            EnergyTag.Quick => "⚡",
            EnergyTag.LowEnergy => "😴",
            _ => "⚡"
        };

        public string EnergySuggestion
        {
            get
            {
                var hour = DateTime.Now.Hour;
                var deepWork = TodayTasks.Count(t => !t.IsCompleted && t.EnergyTag == EnergyTag.Deep);
                var quickWins = TodayTasks.Count(t => !t.IsCompleted && t.EnergyTag == EnergyTag.Quick);

                if (hour >= 6 && hour < 13 && deepWork > 0)
                    return $"🧠 Morning focus: {deepWork} deep work problem{(deepWork > 1 ? "s" : "")} waiting!";
                if (hour >= 13 && hour < 17 && quickWins > 0)
                    return $"⚡ Midday momentum: Knock out {quickWins} quick win{(quickWins > 1 ? "s" : "")}!";
                if (hour >= 18)
                    return "🌙 Evening reflection: Revise 3 Core CS cards before wrapping up!";
                return "🔥 Keep the streak burning! One step closer to placement!";
            }
        }

        public double TodayProgress
        {
            get
            {
                var todayList = TodayTasks.ToList();
                if (todayList.Count == 0) return 0;
                return (double)todayList.Count(t => t.IsCompleted) / todayList.Count * 100;
            }
        }

        public int RemainingTodayTasks => TodayTasks.Count(t => !t.IsCompleted);
        public int TotalTodayTasks => TodayTasks.Count;
        public int CompletedTodayTasks => TodayTasks.Count(t => t.IsCompleted);
        public bool AllTodayTasksComplete => TodayTasks.Any() && TodayTasks.All(t => t.IsCompleted);
        public bool HasNoTasks => TodayTasks.Count == 0;
        public bool HasTasks => TodayTasks.Count > 0;
        public bool HasCompletedTasks => CompletedTasks.Count > 0;

        public string TimeGreeting
        {
            get
            {
                var hour = DateTime.Now.Hour;
                if (hour < 12) return "Good Morning, SDE Aspirant! ☀️";
                if (hour < 17) return "Good Afternoon, Future Engineer! ⚡";
                return "Good Evening, Code Crusher! 🌙";
            }
        }

        public string TodayDateFormatted => DateTime.Now.ToString("dddd, MMMM d");

        #endregion

        #region Daily Highlight Properties

        public TaskItem? DailyHighlight
        {
            get => _dailyHighlight;
            set
            {
                if (_dailyHighlight != null)
                    _dailyHighlight.IsDailyHighlight = false;

                _dailyHighlight = value;

                if (_dailyHighlight != null)
                    _dailyHighlight.IsDailyHighlight = true;

                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDailyHighlight));
                OnPropertyChanged(nameof(DailyHighlightTitle));
            }
        }

        public bool HasDailyHighlight => DailyHighlight != null && !DailyHighlight.IsCompleted;
        public string DailyHighlightTitle => DailyHighlight?.Title ?? "No highlight set";

        #endregion

        #region Streak Protection & Reflections

        public bool StreakPaused
        {
            get => _streakPaused;
            set { _streakPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(StreakStatusText)); SaveData(); }
        }

        public int PausedStreakValue
        {
            get => _pausedStreakValue;
            set { _pausedStreakValue = value; OnPropertyChanged(); }
        }

        public string StreakStatusText => StreakPaused
            ? $"⏸️ Streak paused ({PausedStreakValue} days saved)"
            : $"🔥 {CurrentStreak} day streak";

        public bool ShowMorningCheckIn
        {
            get => _showMorningCheckIn;
            set { _showMorningCheckIn = value; OnPropertyChanged(); }
        }

        public bool ShowEveningCheckIn
        {
            get => _showEveningCheckIn;
            set { _showEveningCheckIn = value; OnPropertyChanged(); }
        }

        public string MorningReflection
        {
            get => _morningReflection;
            set { _morningReflection = value; OnPropertyChanged(); }
        }

        public string EveningReflection
        {
            get => _eveningReflection;
            set { _eveningReflection = value; OnPropertyChanged(); }
        }

        #endregion

        #region Focus Mode / Pomodoro Properties

        public bool IsFocusMode
        {
            get => _isFocusMode;
            set { _isFocusMode = value; OnPropertyChanged(); }
        }

        public TaskItem? FocusTask
        {
            get => _focusTask;
            set { _focusTask = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasFocusTask)); }
        }

        public bool HasFocusTask => FocusTask != null;

        public int PomodoroMinutes
        {
            get => _pomodoroMinutes;
            set { _pomodoroMinutes = value; OnPropertyChanged(); OnPropertyChanged(nameof(PomodoroDisplay)); }
        }

        public int PomodoroSeconds
        {
            get => _pomodoroSeconds;
            set { _pomodoroSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PomodoroDisplay)); }
        }

        public bool IsPomodoroRunning
        {
            get => _isPomodoroRunning;
            set { _isPomodoroRunning = value; OnPropertyChanged(); }
        }

        public string PomodoroDisplay => $"{PomodoroMinutes:D2}:{PomodoroSeconds:D2}";

        #endregion

        #region DSA Sheet & Filter Properties

        public string DsaSearchText
        {
            get => _dsaSearchText;
            set { _dsaSearchText = value; OnPropertyChanged(); ApplyDsaFilter(); }
        }

        public string SelectedDsaTopicFilter
        {
            get => _selectedDsaTopicFilter;
            set { _selectedDsaTopicFilter = value; OnPropertyChanged(); ApplyDsaFilter(); }
        }

        public string SelectedDsaDifficultyFilter
        {
            get => _selectedDsaDifficultyFilter;
            set { _selectedDsaDifficultyFilter = value; OnPropertyChanged(); ApplyDsaFilter(); }
        }

        public string SelectedDsaStatusFilter
        {
            get => _selectedDsaStatusFilter;
            set { _selectedDsaStatusFilter = value; OnPropertyChanged(); ApplyDsaFilter(); }
        }

        public bool OnlyDueForReviewFilter
        {
            get => _onlyDueForReviewFilter;
            set { _onlyDueForReviewFilter = value; OnPropertyChanged(); ApplyDsaFilter(); }
        }

        // Add DSA Fields
        public string NewDsaTitle
        {
            get => _newDsaTitle;
            set { _newDsaTitle = value; OnPropertyChanged(); }
        }

        public ProblemDifficulty NewDsaDifficulty
        {
            get => _newDsaDifficulty;
            set { _newDsaDifficulty = value; OnPropertyChanged(); }
        }

        public DsaTopic NewDsaTopic
        {
            get => _newDsaTopic;
            set { _newDsaTopic = value; OnPropertyChanged(); }
        }

        public string NewDsaPattern
        {
            get => _newDsaPattern;
            set { _newDsaPattern = value; OnPropertyChanged(); }
        }

        public string NewDsaUrl
        {
            get => _newDsaUrl;
            set { _newDsaUrl = value; OnPropertyChanged(); }
        }

        public string NewDsaCompanyTags
        {
            get => _newDsaCompanyTags;
            set { _newDsaCompanyTags = value; OnPropertyChanged(); }
        }

        #endregion

        #region Core CS Properties

        public string CoreCsSearchText
        {
            get => _coreCsSearchText;
            set { _coreCsSearchText = value; OnPropertyChanged(); ApplyCoreCsFilter(); }
        }

        public string SelectedCoreCsSubjectFilter
        {
            get => _selectedCoreCsSubjectFilter;
            set { _selectedCoreCsSubjectFilter = value; OnPropertyChanged(); ApplyCoreCsFilter(); }
        }

        #endregion

        #region Company Application Form Properties

        public string NewCompanyName
        {
            get => _newCompanyName;
            set { _newCompanyName = value; OnPropertyChanged(); }
        }

        public string NewCompanyRole
        {
            get => _newCompanyRole;
            set { _newCompanyRole = value; OnPropertyChanged(); }
        }

        public string NewCompanyPackage
        {
            get => _newCompanyPackage;
            set { _newCompanyPackage = value; OnPropertyChanged(); }
        }

        public DriveStage NewCompanyStage
        {
            get => _newCompanyStage;
            set { _newCompanyStage = value; OnPropertyChanged(); }
        }

        public DateTime? NewCompanyDate
        {
            get => _newCompanyDate;
            set { _newCompanyDate = value; OnPropertyChanged(); }
        }

        public string NewCompanyEvent
        {
            get => _newCompanyEvent;
            set { _newCompanyEvent = value; OnPropertyChanged(); }
        }

        public string NewCompanyFocus
        {
            get => _newCompanyFocus;
            set { _newCompanyFocus = value; OnPropertyChanged(); }
        }

        // Kanban Pipeline Collections
        public IEnumerable<CompanyApplication> WishlistApplications => CompanyApplications.Where(a => a.Stage == DriveStage.Wishlist);
        public IEnumerable<CompanyApplication> AppliedApplications => CompanyApplications.Where(a => a.Stage == DriveStage.Applied);
        public IEnumerable<CompanyApplication> OaApplications => CompanyApplications.Where(a => a.Stage == DriveStage.OnlineAssessment);
        public IEnumerable<CompanyApplication> TechnicalApplications => CompanyApplications.Where(a => a.Stage == DriveStage.TechnicalRound);
        public IEnumerable<CompanyApplication> HrApplications => CompanyApplications.Where(a => a.Stage == DriveStage.HrRound);
        public IEnumerable<CompanyApplication> OfferedApplications => CompanyApplications.Where(a => a.Stage == DriveStage.OfferReceived);

        #endregion

        #region Mock Arena Properties

        public int MockTargetMinutes
        {
            get => _mockTargetMinutes;
            set
            {
                _mockTargetMinutes = value;
                if (!IsMockRunning)
                {
                    MockRemainingSeconds = value * 60;
                }
                OnPropertyChanged();
            }
        }

        public int MockRemainingSeconds
        {
            get => _mockRemainingSeconds;
            set { _mockRemainingSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(MockDisplayTime)); }
        }

        public string MockDisplayTime
        {
            get
            {
                var mins = MockRemainingSeconds / 60;
                var secs = MockRemainingSeconds % 60;
                return $"{mins:D2}:{secs:D2}";
            }
        }

        public bool IsMockRunning
        {
            get => _isMockRunning;
            set { _isMockRunning = value; OnPropertyChanged(); }
        }

        public DsaProblem? CurrentMockProblem
        {
            get => _currentMockProblem;
            set { _currentMockProblem = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasMockProblem)); }
        }

        public bool HasMockProblem => CurrentMockProblem != null;

        public string MockCodeScratchpad
        {
            get => _mockCodeScratchpad;
            set { _mockCodeScratchpad = value; OnPropertyChanged(); }
        }

        public string MockNotesScratchpad
        {
            get => _mockNotesScratchpad;
            set { _mockNotesScratchpad = value; OnPropertyChanged(); }
        }

        public string MockLanguage
        {
            get => _mockLanguage;
            set { _mockLanguage = value; OnPropertyChanged(); }
        }

        public string MockTimeComplexity
        {
            get => _mockTimeComplexity;
            set { _mockTimeComplexity = value; OnPropertyChanged(); }
        }

        public string MockSpaceComplexity
        {
            get => _mockSpaceComplexity;
            set { _mockSpaceComplexity = value; OnPropertyChanged(); }
        }

        #endregion

        #region Commands Declarations

        // Navigation Commands
        public ICommand NavigateToHomeCommand { get; private set; } = null!;
        public ICommand NavigateToDsaVaultCommand { get; private set; } = null!;
        public ICommand NavigateToCoreCsCommand { get; private set; } = null!;
        public ICommand NavigateToCompaniesCommand { get; private set; } = null!;
        public ICommand NavigateToMockArenaCommand { get; private set; } = null!;
        public ICommand NavigateToBehavioralCommand { get; private set; } = null!;
        public ICommand NavigateToAnalyticsCommand { get; private set; } = null!;
        public ICommand NavigateToAchievementsCommand { get; private set; } = null!;
        public ICommand NavigateToHistoryCommand { get; private set; } = null!;
        public ICommand NavigateToSettingsCommand { get; private set; } = null!;
        public ICommand ToggleFullScreenCommand { get; private set; } = null!;
        public ICommand ToggleThemeCommand { get; private set; } = null!;

        // Task & General Commands
        public ICommand AddTaskCommand { get; private set; } = null!;
        public ICommand CompleteTaskCommand { get; private set; } = null!;
        public ICommand DeleteTaskCommand { get; private set; } = null!;
        public ICommand SetDailyHighlightCommand { get; private set; } = null!;
        public ICommand CycleNewTaskEnergyCommand { get; private set; } = null!;
        public ICommand AddGoalCommand { get; private set; } = null!;
        public ICommand IncrementGoalCommand { get; private set; } = null!;
        public ICommand DeleteGoalCommand { get; private set; } = null!;

        // Focus Mode / Pomodoro Commands
        public ICommand EnterFocusModeCommand { get; private set; } = null!;
        public ICommand ExitFocusModeCommand { get; private set; } = null!;
        public ICommand StartPomodoroCommand { get; private set; } = null!;
        public ICommand PausePomodoroCommand { get; private set; } = null!;
        public ICommand ResetPomodoroCommand { get; private set; } = null!;
        public ICommand IncreasePomodoroTimeCommand { get; private set; } = null!;
        public ICommand DecreasePomodoroTimeCommand { get; private set; } = null!;
        public ICommand CompleteFocusTaskCommand { get; private set; } = null!;

        // Streak & Check-in Commands
        public ICommand PauseStreakCommand { get; private set; } = null!;
        public ICommand ResumeStreakCommand { get; private set; } = null!;
        public ICommand SaveMorningCheckInCommand { get; private set; } = null!;
        public ICommand SaveEveningCheckInCommand { get; private set; } = null!;
        public ICommand DismissCheckInCommand { get; private set; } = null!;

        // DSA Commands
        public ICommand AddDsaProblemCommand { get; private set; } = null!;
        public ICommand ToggleDsaSolvedCommand { get; private set; } = null!;
        public ICommand ToggleDsaExpandCommand { get; private set; } = null!;
        public ICommand ReviewHardCommand { get; private set; } = null!;
        public ICommand ReviewGoodCommand { get; private set; } = null!;
        public ICommand ReviewEasyCommand { get; private set; } = null!;
        public ICommand OpenLeetCodeUrlCommand { get; private set; } = null!;
        public ICommand DeleteDsaProblemCommand { get; private set; } = null!;

        // Core CS Commands
        public ICommand ToggleFlashcardAnswerCommand { get; private set; } = null!;
        public ICommand ToggleCoreCsMasteredCommand { get; private set; } = null!;

        // Kanban Commands
        public ICommand AddCompanyCommand { get; private set; } = null!;
        public ICommand MoveCompanyNextCommand { get; private set; } = null!;
        public ICommand MoveCompanyPrevCommand { get; private set; } = null!;
        public ICommand DeleteCompanyCommand { get; private set; } = null!;

        // Mock Arena Commands
        public ICommand PickRandomMockProblemCommand { get; private set; } = null!;
        public ICommand StartMockSessionCommand { get; private set; } = null!;
        public ICommand PauseMockSessionCommand { get; private set; } = null!;
        public ICommand ResetMockSessionCommand { get; private set; } = null!;
        public ICommand SubmitMockSessionCommand { get; private set; } = null!;

        // Behavioral Commands
        public ICommand ToggleBehavioralExpandCommand { get; private set; } = null!;

        #endregion

        #region Initialize Commands

        private void InitializeCommands()
        {
            // Navigation
            NavigateToHomeCommand = new RelayCommand(() => CurrentPage = AppPage.Home);
            NavigateToDsaVaultCommand = new RelayCommand(() => CurrentPage = AppPage.DsaVault);
            NavigateToCoreCsCommand = new RelayCommand(() => CurrentPage = AppPage.CoreCs);
            NavigateToCompaniesCommand = new RelayCommand(() => CurrentPage = AppPage.Companies);
            NavigateToMockArenaCommand = new RelayCommand(() => CurrentPage = AppPage.MockArena);
            NavigateToBehavioralCommand = new RelayCommand(() => CurrentPage = AppPage.Behavioral);
            NavigateToAnalyticsCommand = new RelayCommand(() => CurrentPage = AppPage.Analytics);
            NavigateToAchievementsCommand = new RelayCommand(() => CurrentPage = AppPage.Achievements);
            NavigateToHistoryCommand = new RelayCommand(() => CurrentPage = AppPage.History);
            NavigateToSettingsCommand = new RelayCommand(() => CurrentPage = AppPage.Settings);

            ToggleFullScreenCommand = new RelayCommand(() => IsFullScreen = !IsFullScreen);
            ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);

            // Tasks
            AddTaskCommand = new RelayCommand(AddTask);
            CompleteTaskCommand = new RelayCommand<TaskItem>(CompleteTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
            SetDailyHighlightCommand = new RelayCommand<TaskItem>(task => DailyHighlight = task);
            CycleNewTaskEnergyCommand = new RelayCommand(() =>
            {
                NewTaskEnergy = NewTaskEnergy switch
                {
                    EnergyTag.Quick => EnergyTag.Deep,
                    EnergyTag.Deep => EnergyTag.LowEnergy,
                    EnergyTag.LowEnergy => EnergyTag.Quick,
                    _ => EnergyTag.Quick
                };
            });

            // Goals
            AddGoalCommand = new RelayCommand(AddGoal);
            IncrementGoalCommand = new RelayCommand<Goal>(goal =>
            {
                if (goal != null)
                {
                    goal.CurrentProgress = Math.Min(goal.TargetProgress, goal.CurrentProgress + 10);
                    SaveData();
                }
            });
            DeleteGoalCommand = new RelayCommand<Goal>(goal =>
            {
                if (goal != null)
                {
                    Goals.Remove(goal);
                    SaveData();
                }
            });

            // Focus Mode / Pomodoro
            EnterFocusModeCommand = new RelayCommand<TaskItem>(task =>
            {
                FocusTask = task ?? DailyHighlight ?? TodayTasks.FirstOrDefault(t => !t.IsCompleted);
                IsFocusMode = true;
                PomodoroMinutes = 25;
                PomodoroSeconds = 0;
            });
            ExitFocusModeCommand = new RelayCommand(() =>
            {
                IsFocusMode = false;
                PausePomodoro();
            });
            StartPomodoroCommand = new RelayCommand(StartPomodoro);
            PausePomodoroCommand = new RelayCommand(PausePomodoro);
            ResetPomodoroCommand = new RelayCommand(() =>
            {
                PausePomodoro();
                PomodoroMinutes = 25;
                PomodoroSeconds = 0;
            });
            IncreasePomodoroTimeCommand = new RelayCommand(() =>
            {
                if (!IsPomodoroRunning && PomodoroMinutes < 90) PomodoroMinutes += 5;
            });
            DecreasePomodoroTimeCommand = new RelayCommand(() =>
            {
                if (!IsPomodoroRunning && PomodoroMinutes > 5) PomodoroMinutes -= 5;
            });
            CompleteFocusTaskCommand = new RelayCommand(() =>
            {
                if (FocusTask != null)
                {
                    CompleteTask(FocusTask);
                    ExitFocusModeCommand.Execute(null);
                }
            });

            // Streak & Reflections
            PauseStreakCommand = new RelayCommand(() =>
            {
                StreakPaused = true;
                PausedStreakValue = CurrentStreak;
            });
            ResumeStreakCommand = new RelayCommand(() => StreakPaused = false);
            SaveMorningCheckInCommand = new RelayCommand(SaveMorningCheckIn);
            SaveEveningCheckInCommand = new RelayCommand(SaveEveningCheckIn);
            DismissCheckInCommand = new RelayCommand(() =>
            {
                ShowMorningCheckIn = false;
                ShowEveningCheckIn = false;
            });

            // DSA Commands
            AddDsaProblemCommand = new RelayCommand(AddDsaProblem);
            ToggleDsaSolvedCommand = new RelayCommand<DsaProblem>(ToggleDsaSolved);
            ToggleDsaExpandCommand = new RelayCommand<DsaProblem>(p => { if (p != null) p.IsExpanded = !p.IsExpanded; });
            ReviewHardCommand = new RelayCommand<DsaProblem>(p =>
            {
                if (p != null)
                {
                    p.RecordReview(1);
                    RecordDailyActivity(20);
                    UpdateDueReviewProblems();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });
            ReviewGoodCommand = new RelayCommand<DsaProblem>(p =>
            {
                if (p != null)
                {
                    p.RecordReview(3);
                    RecordDailyActivity(20);
                    UpdateDueReviewProblems();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });
            ReviewEasyCommand = new RelayCommand<DsaProblem>(p =>
            {
                if (p != null)
                {
                    p.RecordReview(5);
                    RecordDailyActivity(20);
                    UpdateDueReviewProblems();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });
            OpenLeetCodeUrlCommand = new RelayCommand<DsaProblem>(p =>
            {
                if (p != null && !string.IsNullOrWhiteSpace(p.LeetCodeUrl))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = p.LeetCodeUrl,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            });
            DeleteDsaProblemCommand = new RelayCommand<DsaProblem>(p =>
            {
                if (p != null)
                {
                    DsaProblems.Remove(p);
                    ApplyDsaFilter();
                    UpdateDueReviewProblems();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });

            // Core CS Commands
            ToggleFlashcardAnswerCommand = new RelayCommand<CoreCsTopic>(card =>
            {
                if (card != null) card.IsFlipped = !card.IsFlipped;
            });
            ToggleCoreCsMasteredCommand = new RelayCommand<CoreCsTopic>(card =>
            {
                if (card != null)
                {
                    card.IsMastered = !card.IsMastered;
                    if (card.IsMastered) RecordDailyActivity(15);
                    RecalculateReadinessScore();
                    SaveData();
                }
            });

            // Kanban Commands
            AddCompanyCommand = new RelayCommand(AddCompany);
            MoveCompanyNextCommand = new RelayCommand<CompanyApplication>(app =>
            {
                if (app != null)
                {
                    app.MoveNextStage();
                    if (app.Stage == DriveStage.OfferReceived)
                    {
                        TriggerConfetti();
                    }
                    RefreshKanbanProperties();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });
            MoveCompanyPrevCommand = new RelayCommand<CompanyApplication>(app =>
            {
                if (app != null)
                {
                    app.MovePreviousStage();
                    RefreshKanbanProperties();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });
            DeleteCompanyCommand = new RelayCommand<CompanyApplication>(app =>
            {
                if (app != null)
                {
                    CompanyApplications.Remove(app);
                    RefreshKanbanProperties();
                    RecalculateReadinessScore();
                    SaveData();
                }
            });

            // Mock Arena Commands
            PickRandomMockProblemCommand = new RelayCommand(() =>
            {
                var random = new Random();
                var available = DsaProblems.ToList();
                if (available.Count > 0)
                {
                    CurrentMockProblem = available[random.Next(available.Count)];
                }
            });
            StartMockSessionCommand = new RelayCommand(StartMockSession);
            PauseMockSessionCommand = new RelayCommand(PauseMockSession);
            ResetMockSessionCommand = new RelayCommand(ResetMockSession);
            SubmitMockSessionCommand = new RelayCommand(SubmitMockSession);

            // Behavioral Commands
            ToggleBehavioralExpandCommand = new RelayCommand<BehavioralQuestion>(q =>
            {
                if (q != null) q.IsExpanded = !q.IsExpanded;
            });
        }

        public class DsaReviewParam
        {
            public DsaProblem? Problem { get; set; }
            public int Rating { get; set; }
        }

        #endregion

        #region Task & Goal Logic

        private void AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

            var task = new TaskItem
            {
                Title = NewTaskTitle.Trim(),
                DueDate = NewTaskDueDate,
                EnergyTag = NewTaskEnergy
            };

            Tasks.Add(task);
            TodayTasks.Add(task);

            if (DailyHighlight == null && !task.IsCompleted)
            {
                DailyHighlight = task;
            }

            NewTaskTitle = string.Empty;
            NewTaskDueDate = null;

            NotifyProgressChanged();
            SaveData();
        }

        private void CompleteTask(TaskItem? task)
        {
            if (task == null) return;

            task.IsCompleted = !task.IsCompleted;

            if (task.IsCompleted)
            {
                TotalTasksCompleted++;
                CompletedTasks.Insert(0, task);
                RecordDailyActivity(15);
                UpdateStreak();

                if (AllTodayTasksComplete)
                {
                    TriggerConfetti();
                }
            }
            else
            {
                CompletedTasks.Remove(task);
                if (TotalTasksCompleted > 0) TotalTasksCompleted--;
            }

            NotifyProgressChanged();
            UpdateBadges();
            SaveData();
        }

        private void DeleteTask(TaskItem? task)
        {
            if (task == null) return;

            Tasks.Remove(task);
            TodayTasks.Remove(task);
            CompletedTasks.Remove(task);

            if (DailyHighlight == task)
            {
                DailyHighlight = TodayTasks.FirstOrDefault(t => !t.IsCompleted);
            }

            NotifyProgressChanged();
            SaveData();
        }

        private void AddGoal()
        {
            if (string.IsNullOrWhiteSpace(NewGoalTitle)) return;

            var goal = new Goal
            {
                Title = NewGoalTitle.Trim(),
                TargetProgress = 100,
                CurrentProgress = 0
            };

            Goals.Add(goal);
            NewGoalTitle = string.Empty;
            SaveData();
        }

        private void UpdateTodayTasks()
        {
            TodayTasks.Clear();
            var today = DateTime.Today;

            foreach (var task in Tasks)
            {
                if (!task.DueDate.HasValue || task.DueDate.Value.Date <= today)
                {
                    TodayTasks.Add(task);
                }
            }

            CompletedTasks.Clear();
            foreach (var task in Tasks.Where(t => t.IsCompleted).OrderByDescending(t => t.CompletedAt))
            {
                CompletedTasks.Add(task);
            }

            NotifyProgressChanged();
        }

        #endregion

        #region Pomodoro / Focus Mode Logic

        private void StartPomodoro()
        {
            if (_pomodoroTimer == null)
            {
                _pomodoroTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _pomodoroTimer.Tick += PomodoroTimer_Tick;
            }

            _pomodoroTimer.Start();
            IsPomodoroRunning = true;
        }

        private void PausePomodoro()
        {
            _pomodoroTimer?.Stop();
            IsPomodoroRunning = false;
        }

        private void PomodoroTimer_Tick(object? sender, EventArgs e)
        {
            if (PomodoroSeconds > 0)
            {
                PomodoroSeconds--;
            }
            else if (PomodoroMinutes > 0)
            {
                PomodoroMinutes--;
                PomodoroSeconds = 59;
            }
            else
            {
                PausePomodoro();
                TriggerConfetti();
                RecordDailyActivity(30);
            }
        }

        #endregion

        #region DSA Sheet & Spaced Repetition Logic

        public void ApplyDsaFilter()
        {
            FilteredDsaProblems.Clear();

            var query = DsaProblems.AsEnumerable();

            // Text search
            if (!string.IsNullOrWhiteSpace(DsaSearchText))
            {
                var term = DsaSearchText.Trim().ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(term) ||
                    p.Pattern.ToLower().Contains(term) ||
                    p.CompanyTags.ToLower().Contains(term) ||
                    p.IntuitionNotes.ToLower().Contains(term));
            }

            // Topic filter
            if (!string.IsNullOrWhiteSpace(SelectedDsaTopicFilter) && SelectedDsaTopicFilter != "All Topics")
            {
                query = query.Where(p => p.TopicDisplay.Equals(SelectedDsaTopicFilter, StringComparison.OrdinalIgnoreCase) ||
                                         p.Topic.ToString().Equals(SelectedDsaTopicFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Difficulty filter
            if (!string.IsNullOrWhiteSpace(SelectedDsaDifficultyFilter) && SelectedDsaDifficultyFilter != "All")
            {
                if (Enum.TryParse<ProblemDifficulty>(SelectedDsaDifficultyFilter, true, out var diff))
                {
                    query = query.Where(p => p.Difficulty == diff);
                }
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(SelectedDsaStatusFilter) && SelectedDsaStatusFilter != "All")
            {
                if (SelectedDsaStatusFilter == "Solved") query = query.Where(p => p.Status == ProblemStatus.Solved);
                else if (SelectedDsaStatusFilter == "Unsolved") query = query.Where(p => p.Status == ProblemStatus.Unsolved);
                else if (SelectedDsaStatusFilter == "In Progress") query = query.Where(p => p.Status == ProblemStatus.Solving);
                else if (SelectedDsaStatusFilter == "Due for Review") query = query.Where(p => p.IsDueForReview);
            }

            if (OnlyDueForReviewFilter)
            {
                query = query.Where(p => p.IsDueForReview);
            }

            foreach (var problem in query.OrderBy(p => p.Status == ProblemStatus.Solved ? 1 : 0).ThenBy(p => p.Difficulty))
            {
                FilteredDsaProblems.Add(problem);
            }
        }

        public void UpdateDueReviewProblems()
        {
            DueReviewProblems.Clear();
            foreach (var problem in DsaProblems.Where(p => p.IsDueForReview).Take(5))
            {
                DueReviewProblems.Add(problem);
            }
            OnPropertyChanged(nameof(DueReviewCount));
        }

        private void ToggleDsaSolved(DsaProblem? problem)
        {
            if (problem == null) return;

            if (problem.Status == ProblemStatus.Solved)
            {
                problem.Status = ProblemStatus.Unsolved;
            }
            else
            {
                problem.RecordReview(4); // Default strong
                RecordDailyActivity(25);
                UpdateStreak();
                TriggerConfetti();
            }

            ApplyDsaFilter();
            UpdateDueReviewProblems();
            RecalculateReadinessScore();
            UpdateBadges();
            SaveData();
        }

        private void AddDsaProblem()
        {
            if (string.IsNullOrWhiteSpace(NewDsaTitle)) return;

            var problem = new DsaProblem
            {
                Title = NewDsaTitle.Trim(),
                Difficulty = NewDsaDifficulty,
                Topic = NewDsaTopic,
                Pattern = string.IsNullOrWhiteSpace(NewDsaPattern) ? "Pattern" : NewDsaPattern.Trim(),
                LeetCodeUrl = NewDsaUrl.Trim(),
                CompanyTags = NewDsaCompanyTags.Trim()
            };

            DsaProblems.Add(problem);
            NewDsaTitle = string.Empty;
            NewDsaPattern = string.Empty;
            NewDsaUrl = string.Empty;
            NewDsaCompanyTags = string.Empty;

            ApplyDsaFilter();
            RecalculateReadinessScore();
            SaveData();
        }

        #endregion

        #region Core CS Logic

        public void ApplyCoreCsFilter()
        {
            FilteredCoreCsQuestions.Clear();

            var query = CoreCsQuestions.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(CoreCsSearchText))
            {
                var term = CoreCsSearchText.Trim().ToLower();
                query = query.Where(q =>
                    q.Question.ToLower().Contains(term) ||
                    q.ShortAnswer.ToLower().Contains(term) ||
                    q.DetailedExplanation.ToLower().Contains(term) ||
                    q.HighYieldKeywords.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(SelectedCoreCsSubjectFilter) && SelectedCoreCsSubjectFilter != "All Subjects")
            {
                query = query.Where(q => q.SubjectDisplay.Equals(SelectedCoreCsSubjectFilter, StringComparison.OrdinalIgnoreCase) ||
                                         q.Subject.ToString().Equals(SelectedCoreCsSubjectFilter, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var item in query)
            {
                FilteredCoreCsQuestions.Add(item);
            }
        }

        #endregion

        #region Placement Kanban Logic

        private void AddCompany()
        {
            if (string.IsNullOrWhiteSpace(NewCompanyName)) return;

            var app = new CompanyApplication
            {
                CompanyName = NewCompanyName.Trim(),
                Role = string.IsNullOrWhiteSpace(NewCompanyRole) ? "SDE" : NewCompanyRole.Trim(),
                PackageCtc = NewCompanyPackage.Trim(),
                Stage = NewCompanyStage,
                UpcomingEventDate = NewCompanyDate,
                UpcomingEventTitle = NewCompanyEvent.Trim(),
                FocusTopics = NewCompanyFocus.Trim()
            };

            CompanyApplications.Add(app);

            NewCompanyName = string.Empty;
            RefreshKanbanProperties();
            RecalculateReadinessScore();
            SaveData();
        }

        private void RefreshKanbanProperties()
        {
            OnPropertyChanged(nameof(WishlistApplications));
            OnPropertyChanged(nameof(AppliedApplications));
            OnPropertyChanged(nameof(OaApplications));
            OnPropertyChanged(nameof(TechnicalApplications));
            OnPropertyChanged(nameof(HrApplications));
            OnPropertyChanged(nameof(OfferedApplications));
            OnPropertyChanged(nameof(ActiveCompaniesCount));
            OnPropertyChanged(nameof(OffersCount));
            OnPropertyChanged(nameof(UpcomingDriveAlert));
        }

        #endregion

        #region Mock Arena Logic

        private void StartMockSession()
        {
            if (CurrentMockProblem == null)
            {
                PickRandomMockProblemCommand.Execute(null);
            }

            if (_mockTimer == null)
            {
                _mockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                _mockTimer.Tick += (s, e) =>
                {
                    _mockElapsedSeconds++;
                    if (MockRemainingSeconds > 0)
                    {
                        MockRemainingSeconds--;
                    }
                    else
                    {
                        PauseMockSession();
                    }
                };
            }

            _mockTimer.Start();
            IsMockRunning = true;
        }

        private void PauseMockSession()
        {
            _mockTimer?.Stop();
            IsMockRunning = false;
        }

        private void ResetMockSession()
        {
            PauseMockSession();
            MockRemainingSeconds = MockTargetMinutes * 60;
            _mockElapsedSeconds = 0;
        }

        private void SubmitMockSession()
        {
            PauseMockSession();

            var session = new MockSession
            {
                Title = $"Mock OA #{MockHistory.Count + 1}",
                ProblemTitle = CurrentMockProblem?.Title ?? "Custom Problem",
                TargetMinutes = MockTargetMinutes,
                TimeTakenSeconds = _mockElapsedSeconds > 0 ? _mockElapsedSeconds : (MockTargetMinutes * 60 - MockRemainingSeconds),
                PassedAllTests = true,
                Language = MockLanguage,
                TimeComplexityAchieved = MockTimeComplexity,
                SpaceComplexityAchieved = MockSpaceComplexity,
                Notes = MockNotesScratchpad,
                Code = MockCodeScratchpad
            };

            MockHistory.Insert(0, session);

            if (CurrentMockProblem != null)
            {
                CurrentMockProblem.RecordReview(5);
            }

            RecordDailyActivity(40);
            TriggerConfetti();
            RecalculateReadinessScore();
            UpdateBadges();
            SaveData();
            ResetMockSession();
        }

        #endregion

        #region Readiness Calculation & Gamification

        public void RecalculateReadinessScore()
        {
            OnPropertyChanged(nameof(PlacementReadinessScore));
            OnPropertyChanged(nameof(ReadinessRank));
            OnPropertyChanged(nameof(TotalDsaSolved));
            OnPropertyChanged(nameof(TotalDsaCount));
            OnPropertyChanged(nameof(EasyDsaSolved));
            OnPropertyChanged(nameof(MediumDsaSolved));
            OnPropertyChanged(nameof(HardDsaSolved));
            OnPropertyChanged(nameof(TotalCoreCsMastered));
            OnPropertyChanged(nameof(TotalCoreCsCount));
            OnPropertyChanged(nameof(ActiveCompaniesCount));
            OnPropertyChanged(nameof(OffersCount));
            OnPropertyChanged(nameof(DueReviewCount));
            OnPropertyChanged(nameof(UpcomingDriveAlert));
            OnPropertyChanged(nameof(HeatmapActivityData));
        }

        private void UpdateBadges()
        {
            AllBadges.Clear();

            int solved = TotalDsaSolved;
            int csMastered = TotalCoreCsMastered;
            int mockCount = MockHistory.Count;
            int apps = CompanyApplications.Count;
            int offers = OffersCount;
            double score = PlacementReadinessScore;

            AllBadges.Add(new BadgeInfo { Emoji = "🎯", Name = "First Blood", Description = "Complete your first task or problem", IsUnlocked = TotalTasksCompleted > 0 || solved > 0 });
            AllBadges.Add(new BadgeInfo { Emoji = "⚡", Name = "Array & Two Pointers", Description = "Solve 5 Array/Two Pointer problems", IsUnlocked = solved >= 5 });
            AllBadges.Add(new BadgeInfo { Emoji = "🌲", Name = "Tree & Graph Master", Description = "Solve 10 Tree or Graph problems", IsUnlocked = solved >= 10 });
            AllBadges.Add(new BadgeInfo { Emoji = "👑", Name = "DP Conqueror", Description = "Master 15 high-frequency DSA patterns", IsUnlocked = solved >= 15 });
            AllBadges.Add(new BadgeInfo { Emoji = "🧠", Name = "CS Core Scholar", Description = "Master 5 Core CS / OS / SQL cards", IsUnlocked = csMastered >= 5 });
            AllBadges.Add(new BadgeInfo { Emoji = "💼", Name = "Placement Hustler", Description = "Track 3 active campus drives in Kanban", IsUnlocked = apps >= 3 });
            AllBadges.Add(new BadgeInfo { Emoji = "⏱️", Name = "OA Arena Veteran", Description = "Complete 1 timed mock OA sprint", IsUnlocked = mockCount >= 1 });
            AllBadges.Add(new BadgeInfo { Emoji = "🔥", Name = "Week of Steel", Description = "Maintain a 7-day preparation streak", IsUnlocked = BestStreak >= 7 || CurrentStreak >= 7 });
            AllBadges.Add(new BadgeInfo { Emoji = "🚀", Name = "Interview Ready", Description = "Reach 70% Placement Readiness score", IsUnlocked = score >= 70 });
            AllBadges.Add(new BadgeInfo { Emoji = "🏆", Name = "The Dream Offer!", Description = "Secure an SDE placement offer", IsUnlocked = offers >= 1 });
            AllBadges.Add(new BadgeInfo { Emoji = "🎖️", Name = "Unstoppable", Description = "Solve 25+ DSA problems", IsUnlocked = solved >= 25 });
            AllBadges.Add(new BadgeInfo { Emoji = "🧘", Name = "Deep Work Pro", Description = "Complete 50 total tasks & sessions", IsUnlocked = TotalTasksCompleted >= 50 });

            OnPropertyChanged(nameof(UnlockedBadgesCount));
            OnPropertyChanged(nameof(TotalBadges));
        }

        public int UnlockedBadgesCount => AllBadges.Count(b => b.IsUnlocked);
        public int TotalBadges => AllBadges.Count;

        #endregion

        #region Activity, Streaks & Reflections

        private void RecordDailyActivity(double points)
        {
            var key = DateTime.Today.ToString("yyyy-MM-dd");
            if (_dailyCompletionHistory.ContainsKey(key))
            {
                _dailyCompletionHistory[key] = Math.Min(100.0, _dailyCompletionHistory[key] + points);
            }
            else
            {
                _dailyCompletionHistory[key] = points;
            }
            OnPropertyChanged(nameof(HeatmapActivityData));
        }

        private void UpdateStreak()
        {
            var today = DateTime.Today;

            if (!_lastCompletedDate.HasValue)
            {
                CurrentStreak = 1;
            }
            else if (_lastCompletedDate.Value.Date == today)
            {
                // Already counted today
            }
            else if (_lastCompletedDate.Value.Date == today.AddDays(-1))
            {
                CurrentStreak++;
            }
            else if (!StreakPaused)
            {
                CurrentStreak = 1;
            }

            if (CurrentStreak > BestStreak)
            {
                BestStreak = CurrentStreak;
            }

            _lastCompletedDate = today;
        }

        private void CheckStreakStatus()
        {
            if (!_lastCompletedDate.HasValue) return;

            var daysSince = (DateTime.Today - _lastCompletedDate.Value.Date).Days;
            if (daysSince > 1 && !StreakPaused)
            {
                // Streak broken if missed more than 1 day
                CurrentStreak = 0;
            }
        }

        private void CheckDailyCheckInStatus()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            _reflections.TryGetValue(today, out _todayReflection);

            var hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 12 && (_todayReflection == null || !_todayReflection.HasMorning))
            {
                ShowMorningCheckIn = true;
            }
            else if (hour >= 18 && hour < 23 && (_todayReflection == null || !_todayReflection.HasEvening))
            {
                ShowEveningCheckIn = true;
            }
        }

        private void SaveMorningCheckIn()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            _todayReflection ??= new DailyReflection { Date = DateTime.Today };

            _todayReflection.MorningReflection = MorningReflection;
            _todayReflection.UpdatedAt = DateTime.Now;

            _reflections[today] = _todayReflection;
            ShowMorningCheckIn = false;
            RecordDailyActivity(15);
            SaveData();
        }

        private void SaveEveningCheckIn()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            _todayReflection ??= new DailyReflection { Date = DateTime.Today };

            _todayReflection.EveningReflection = EveningReflection;
            _todayReflection.UpdatedAt = DateTime.Now;

            _reflections[today] = _todayReflection;
            ShowEveningCheckIn = false;
            RecordDailyActivity(15);
            SaveData();
        }

        private void TriggerConfetti()
        {
            ShowConfetti = true;
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            timer.Tick += (s, e) =>
            {
                ShowConfetti = false;
                timer.Stop();
            };
            timer.Start();
        }

        private void UpdateMotivationalMessage()
        {
            var quotes = new[]
            {
                "The pain of debugging is temporary, the offer letter is forever 🚀",
                "Consistency beats talent every single time. One problem at a time! 💪",
                "Companies aren't looking for geniuses; they're looking for prepared engineers! ⚡",
                "Master the patterns, not just the solutions! 🎯",
                "Small daily disciplines compound into massive placement victories! 🏆"
            };
            var random = new Random();
            MotivationalMessage = quotes[random.Next(quotes.Length)];
        }

        #endregion

        #region Persistence

        private void LoadData()
        {
            var data = JsonStorageService.Load();

            // Load Tasks
            Tasks.Clear();
            foreach (var task in data.Tasks) Tasks.Add(task);

            // Load Goals
            Goals.Clear();
            foreach (var goal in data.Goals) Goals.Add(goal);

            // Load DSA Problems
            DsaProblems.Clear();
            foreach (var problem in data.DsaProblems) DsaProblems.Add(problem);

            // Load Core CS
            CoreCsQuestions.Clear();
            foreach (var card in data.CoreCsQuestions) CoreCsQuestions.Add(card);

            // Load Applications
            CompanyApplications.Clear();
            foreach (var app in data.CompanyApplications) CompanyApplications.Add(app);

            // Load Behavioral
            BehavioralQuestions.Clear();
            foreach (var q in data.BehavioralQuestions) BehavioralQuestions.Add(q);

            // Load Mock History
            MockHistory.Clear();
            foreach (var m in data.MockHistory) MockHistory.Add(m);

            CurrentStreak = data.CurrentStreak;
            BestStreak = data.BestStreak;
            TotalTasksCompleted = data.TotalTasksCompleted;
            _lastCompletedDate = data.LastCompletedDate;
            _isDarkMode = data.IsDarkMode;
            _dailyCompletionHistory = data.DailyCompletionHistory ?? new();
            _streakPaused = data.StreakPaused;
            _pausedStreakValue = data.PausedStreakValue;
            _reflections = data.Reflections ?? new();

            if (!string.IsNullOrEmpty(data.DailyHighlightTaskId))
            {
                DailyHighlight = Tasks.FirstOrDefault(t => t.Id == data.DailyHighlightTaskId);
            }
        }

        public void SaveData()
        {
            var data = new AppData
            {
                Tasks = Tasks.ToList(),
                Goals = Goals.ToList(),
                CurrentStreak = CurrentStreak,
                BestStreak = BestStreak,
                TotalTasksCompleted = TotalTasksCompleted,
                LastCompletedDate = _lastCompletedDate,
                DailyCompletionHistory = _dailyCompletionHistory,
                IsDarkMode = IsDarkMode,
                StreakPaused = _streakPaused,
                PausedStreakValue = _pausedStreakValue,
                DailyHighlightTaskId = DailyHighlight?.Id,
                Reflections = _reflections,
                DsaProblems = DsaProblems.ToList(),
                CoreCsQuestions = CoreCsQuestions.ToList(),
                CompanyApplications = CompanyApplications.ToList(),
                BehavioralQuestions = BehavioralQuestions.ToList(),
                MockHistory = MockHistory.ToList()
            };

            JsonStorageService.Save(data);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyProgressChanged()
        {
            OnPropertyChanged(nameof(TodayProgress));
            OnPropertyChanged(nameof(RemainingTodayTasks));
            OnPropertyChanged(nameof(TotalTodayTasks));
            OnPropertyChanged(nameof(CompletedTodayTasks));
            OnPropertyChanged(nameof(AllTodayTasksComplete));
            OnPropertyChanged(nameof(HasNoTasks));
            OnPropertyChanged(nameof(HasTasks));
        }

        #endregion
    }

    #region RelayCommand Implementation

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }

    #endregion
}
