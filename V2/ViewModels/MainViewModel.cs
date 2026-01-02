using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using MyTodo.Models;
using MyTodo.Services;

namespace MyTodo.ViewModels
{
    /// <summary>
    /// Available pages in the app
    /// </summary>
    public enum AppPage
    {
        Home,
        Achievements,
        History,
        Settings
    }

    /// <summary>
    /// Main ViewModel handling all app logic.
    /// Uses MVVM-lite pattern - simple and clean.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        #region Private Fields

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

        // Focus Mode fields
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
        private EnergyTag _newTaskEnergy = EnergyTag.Quick;

        // Navigation fields
        private AppPage _currentPage = AppPage.Home;
        private bool _isFullScreen;
        private bool _showSettings;

        #endregion

        #region Collections

        public ObservableCollection<TaskItem> Tasks { get; } = new();
        public ObservableCollection<Goal> Goals { get; } = new();

        /// <summary>
        /// Filtered view showing only today's tasks
        /// </summary>
        public ObservableCollection<TaskItem> TodayTasks { get; } = new();

        /// <summary>
        /// Completed tasks for history view
        /// </summary>
        public ObservableCollection<TaskItem> CompletedTasks { get; } = new();

        /// <summary>
        /// All achievement badges for display
        /// </summary>
        public ObservableCollection<BadgeInfo> AllBadges { get; } = new();

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

        #region Properties

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
            set { _currentStreak = value; OnPropertyChanged(); }
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

        #region Focus Mode Properties

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

        #region Daily Highlight Properties

        public TaskItem? DailyHighlight
        {
            get => _dailyHighlight;
            set 
            { 
                // Clear old highlight
                if (_dailyHighlight != null)
                    _dailyHighlight.IsDailyHighlight = false;
                
                _dailyHighlight = value;
                
                // Set new highlight
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

        #region Streak Protection Properties

        public bool StreakPaused
        {
            get => _streakPaused;
            set { _streakPaused = value; OnPropertyChanged(); SaveData(); }
        }

        public int PausedStreakValue
        {
            get => _pausedStreakValue;
            set { _pausedStreakValue = value; OnPropertyChanged(); }
        }

        public string StreakStatusText => StreakPaused 
            ? $"⏸️ Streak paused ({PausedStreakValue} days saved)" 
            : $"🔥 {CurrentStreak} day streak";

        #endregion

        #region Check-in Properties

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

        public string CheckInPrompt
        {
            get
            {
                var hour = DateTime.Now.Hour;
                if (hour < 12) return "What's your main focus for today?";
                if (hour >= 18) return "What did you accomplish today?";
                return string.Empty;
            }
        }

        public bool ShouldShowCheckIn
        {
            get
            {
                var hour = DateTime.Now.Hour;
                var today = DateTime.Today.ToString("yyyy-MM-dd");
                
                // Morning check-in (6 AM - 12 PM) if not done
                if (hour >= 6 && hour < 12)
                {
                    return _todayReflection == null || !_todayReflection.HasMorning;
                }
                // Evening check-in (6 PM - 11 PM) if not done
                if (hour >= 18 && hour < 23)
                {
                    return _todayReflection == null || !_todayReflection.HasEvening;
                }
                return false;
            }
        }

        public bool IsMorningTime => DateTime.Now.Hour >= 6 && DateTime.Now.Hour < 12;
        public bool IsEveningTime => DateTime.Now.Hour >= 18 && DateTime.Now.Hour < 23;

        #endregion

        #region Energy Tag Properties

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

        public int DeepWorkTaskCount => TodayTasks.Count(t => !t.IsCompleted && t.EnergyTag == EnergyTag.Deep);
        public int QuickWinTaskCount => TodayTasks.Count(t => !t.IsCompleted && t.EnergyTag == EnergyTag.Quick);
        public int LowEnergyTaskCount => TodayTasks.Count(t => !t.IsCompleted && t.EnergyTag == EnergyTag.LowEnergy);

        public string EnergySuggestion
        {
            get
            {
                var hour = DateTime.Now.Hour;
                var quickWins = QuickWinTaskCount;
                var deepWork = DeepWorkTaskCount;
                var lowEnergy = LowEnergyTaskCount;

                // Morning - suggest deep work
                if (hour >= 6 && hour < 12 && deepWork > 0)
                    return $"🧠 Morning focus time! {deepWork} deep work task{(deepWork > 1 ? "s" : "")} waiting.";
                
                // After lunch slump - suggest quick wins
                if (hour >= 13 && hour < 15 && quickWins > 0)
                    return $"⚡ Post-lunch? Knock out {quickWins} quick win{(quickWins > 1 ? "s" : "")}!";
                
                // Evening - suggest low energy
                if (hour >= 18 && lowEnergy > 0)
                    return $"😴 Wind down with {lowEnergy} low-energy task{(lowEnergy > 1 ? "s" : "")}.";
                
                // General suggestion
                if (quickWins > 0)
                    return $"⚡ {quickWins} quick win{(quickWins > 1 ? "s" : "")} available!";
                
                return string.Empty;
            }
        }

        #endregion

        /// <summary>
        /// Overall progress percentage for today's tasks
        /// </summary>
        public double TodayProgress
        {
            get
            {
                var todayTasks = TodayTasks.ToList();
                if (todayTasks.Count == 0) return 0;
                return (double)todayTasks.Count(t => t.IsCompleted) / todayTasks.Count * 100;
            }
        }

        /// <summary>
        /// Count of incomplete tasks for today
        /// </summary>
        public int RemainingTodayTasks => TodayTasks.Count(t => !t.IsCompleted);

        /// <summary>
        /// Total tasks for today
        /// </summary>
        public int TotalTodayTasks => TodayTasks.Count;

        /// <summary>
        /// Completed tasks for today
        /// </summary>
        public int CompletedTodayTasks => TodayTasks.Count(t => t.IsCompleted);

        /// <summary>
        /// True if all today's tasks are complete
        /// </summary>
        public bool AllTodayTasksComplete => TodayTasks.Any() && TodayTasks.All(t => t.IsCompleted);

        /// <summary>
        /// True if there are no tasks at all
        /// </summary>
        public bool HasNoTasks => !Tasks.Any();

        /// <summary>
        /// True if there are tasks to show
        /// </summary>
        public bool HasTasks => Tasks.Any();

        /// <summary>
        /// Progress color based on completion rate
        /// </summary>
        public string ProgressColor
        {
            get
            {
                if (TodayProgress >= 70) return "Green";
                if (TodayProgress >= 30) return "Yellow";
                return "Red";
            }
        }

        /// <summary>
        /// Weekly completion data for the chart (last 7 days)
        /// </summary>
        public List<double> WeeklyData
        {
            get
            {
                var data = new List<double>();
                var today = DateTime.Today;
                
                // Get Monday of current week
                var dayOfWeek = (int)today.DayOfWeek;
                var monday = today.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
                
                for (int i = 0; i < 7; i++)
                {
                    var date = monday.AddDays(i);
                    var key = date.ToString("yyyy-MM-dd");
                    
                    if (_dailyCompletionHistory.TryGetValue(key, out var value))
                    {
                        data.Add(value);
                    }
                    else if (date == DateTime.Today)
                    {
                        data.Add(TodayProgress);
                    }
                    else
                    {
                        data.Add(0);
                    }
                }
                
                return data;
            }
        }

        /// <summary>
        /// Average completion rate this week
        /// </summary>
        public double WeeklyAverage
        {
            get
            {
                var data = WeeklyData.Where(d => d > 0).ToList();
                return data.Any() ? data.Average() : 0;
            }
        }

        /// <summary>
        /// Overall completion rate (all time)
        /// </summary>
        public double OverallCompletionRate
        {
            get
            {
                var total = Tasks.Count;
                if (total == 0) return 0;
                return (double)Tasks.Count(t => t.IsCompleted) / total * 100;
            }
        }

        /// <summary>
        /// Current time greeting
        /// </summary>
        public string TimeGreeting
        {
            get
            {
                var hour = DateTime.Now.Hour;
                if (hour < 12) return "Good Morning";
                if (hour < 17) return "Good Afternoon";
                return "Good Evening";
            }
        }

        /// <summary>
        /// Today's date formatted
        /// </summary>
        public string TodayDateFormatted => DateTime.Today.ToString("dddd, MMMM d");

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
                OnPropertyChanged(nameof(IsAchievementsPage));
                OnPropertyChanged(nameof(IsHistoryPage));
                OnPropertyChanged(nameof(IsSettingsPage));
            }
        }

        public bool IsHomePage => CurrentPage == AppPage.Home;
        public bool IsAchievementsPage => CurrentPage == AppPage.Achievements;
        public bool IsHistoryPage => CurrentPage == AppPage.History;
        public bool IsSettingsPage => CurrentPage == AppPage.Settings;

        public bool IsFullScreen
        {
            get => _isFullScreen;
            set { _isFullScreen = value; OnPropertyChanged(); }
        }

        public bool ShowSettings
        {
            get => _showSettings;
            set { _showSettings = value; OnPropertyChanged(); }
        }

        #endregion

        #region Achievement Properties

        public int TotalDaysActive => _dailyCompletionHistory.Count;
        
        public int PerfectDays => _dailyCompletionHistory.Count(d => d.Value >= 100);
        
        public int PerfectDaysCount => PerfectDays;
        
        public int TotalGoalsCompleted => Goals.Count(g => g.IsCompleted);

        public bool HasCompletedTasks => CompletedTasks.Any();

        public bool HasFirstTaskBadge => TotalTasksCompleted >= 1;
        public bool HasTenTasksBadge => TotalTasksCompleted >= 10;
        public bool HasFiftyTasksBadge => TotalTasksCompleted >= 50;
        public bool HasHundredTasksBadge => TotalTasksCompleted >= 100;
        public bool HasWeekStreakBadge => BestStreak >= 7;
        public bool HasMonthStreakBadge => BestStreak >= 30;
        public bool HasPerfectDayBadge => PerfectDays >= 1;
        public bool HasTenPerfectDaysBadge => PerfectDays >= 10;
        public bool HasEarlyBirdBadge => _reflections.Any(r => r.Value.HasMorning);
        public bool HasNightOwlBadge => _reflections.Any(r => r.Value.HasEvening);
        public bool HasGoalCrusherBadge => TotalGoalsCompleted >= 1;
        public bool HasFocusMasterBadge => TotalTasksCompleted >= 25; // Completed 25 tasks in focus mode (simplified)

        public int UnlockedBadgesCount
        {
            get
            {
                int count = 0;
                if (HasFirstTaskBadge) count++;
                if (HasTenTasksBadge) count++;
                if (HasFiftyTasksBadge) count++;
                if (HasHundredTasksBadge) count++;
                if (HasWeekStreakBadge) count++;
                if (HasMonthStreakBadge) count++;
                if (HasPerfectDayBadge) count++;
                if (HasTenPerfectDaysBadge) count++;
                if (HasEarlyBirdBadge) count++;
                if (HasNightOwlBadge) count++;
                if (HasGoalCrusherBadge) count++;
                if (HasFocusMasterBadge) count++;
                return count;
            }
        }

        public int TotalBadges => 12;

        #endregion

        #region Commands

        public ICommand AddTaskCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand AddGoalCommand { get; }
        public ICommand IncrementGoalCommand { get; }
        public ICommand DeleteGoalCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        // Navigation Commands
        public ICommand NavigateToHomeCommand { get; }
        public ICommand NavigateToAchievementsCommand { get; }
        public ICommand NavigateToHistoryCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        // Focus Mode Commands
        public ICommand EnterFocusModeCommand { get; }
        public ICommand ExitFocusModeCommand { get; }
        public ICommand StartPomodoroCommand { get; }
        public ICommand PausePomodoroCommand { get; }
        public ICommand ResetPomodoroCommand { get; }
        public ICommand CompleteFocusTaskCommand { get; }

        // Daily Highlight Commands
        public ICommand SetDailyHighlightCommand { get; }
        public ICommand ClearDailyHighlightCommand { get; }

        // Streak Protection Commands
        public ICommand PauseStreakCommand { get; }
        public ICommand ResumeStreakCommand { get; }

        // Check-in Commands
        public ICommand SaveMorningCheckInCommand { get; }
        public ICommand SaveEveningCheckInCommand { get; }
        public ICommand DismissCheckInCommand { get; }

        // Energy Tag Commands
        public ICommand CycleNewTaskEnergyCommand { get; }

        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Initialize basic commands
            AddTaskCommand = new RelayCommand(AddTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
            CompleteTaskCommand = new RelayCommand<TaskItem>(CompleteTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
            AddGoalCommand = new RelayCommand(AddGoal, () => !string.IsNullOrWhiteSpace(NewGoalTitle));
            IncrementGoalCommand = new RelayCommand<Goal>(IncrementGoal);
            DeleteGoalCommand = new RelayCommand<Goal>(DeleteGoal);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // Navigation commands
            NavigateToHomeCommand = new RelayCommand(() => CurrentPage = AppPage.Home);
            NavigateToAchievementsCommand = new RelayCommand(() => CurrentPage = AppPage.Achievements);
            NavigateToHistoryCommand = new RelayCommand(() => { CurrentPage = AppPage.History; UpdateCompletedTasks(); });
            NavigateToSettingsCommand = new RelayCommand(() => CurrentPage = AppPage.Settings);
            ToggleFullScreenCommand = new RelayCommand(() => IsFullScreen = !IsFullScreen);

            // Focus Mode commands
            EnterFocusModeCommand = new RelayCommand<TaskItem>(EnterFocusMode);
            ExitFocusModeCommand = new RelayCommand(ExitFocusMode);
            StartPomodoroCommand = new RelayCommand(StartPomodoro);
            PausePomodoroCommand = new RelayCommand(PausePomodoro);
            ResetPomodoroCommand = new RelayCommand(ResetPomodoro);
            CompleteFocusTaskCommand = new RelayCommand(CompleteFocusTask);

            // Daily Highlight commands
            SetDailyHighlightCommand = new RelayCommand<TaskItem>(SetDailyHighlight);
            ClearDailyHighlightCommand = new RelayCommand(ClearDailyHighlight);

            // Streak Protection commands
            PauseStreakCommand = new RelayCommand(PauseStreak);
            ResumeStreakCommand = new RelayCommand(ResumeStreak);

            // Check-in commands
            SaveMorningCheckInCommand = new RelayCommand(SaveMorningCheckIn);
            SaveEveningCheckInCommand = new RelayCommand(SaveEveningCheckIn);
            DismissCheckInCommand = new RelayCommand(DismissCheckIn);

            // Energy Tag commands
            CycleNewTaskEnergyCommand = new RelayCommand(CycleNewTaskEnergy);

            // Load saved data
            LoadData();

            // Initialize badges
            InitializeBadges();

            // Update motivational message
            UpdateMotivationalMessage();

            // Initialize daily highlight
            InitializeDailyHighlight();

            // Check if we should show check-in prompt
            CheckForCheckInPrompt();
        }

        #endregion

        #region Task Methods

        private void AddTask()
        {
            if (string.IsNullOrWhiteSpace(NewTaskTitle)) return;

            var task = new TaskItem
            {
                Title = NewTaskTitle.Trim(),
                DueDate = NewTaskDueDate ?? DateTime.Today,
                EnergyTag = NewTaskEnergy,
                IsAnimating = true // Trigger add animation
            };

            Tasks.Insert(0, task);
            UpdateTodayTasks();

            // Clear input
            NewTaskTitle = string.Empty;
            NewTaskDueDate = null;
            NewTaskEnergy = EnergyTag.Quick;

            SaveData();
            UpdateMotivationalMessage();
            NotifyProgressChanged();
        }

        private void CompleteTask(TaskItem? task)
        {
            if (task == null) return;

            var wasCompleted = task.IsCompleted;
            task.IsCompleted = !task.IsCompleted;
            task.IsAnimating = true;

            // Track completion time
            if (task.IsCompleted && !wasCompleted)
            {
                task.CompletedAt = DateTime.Now;
            }
            else
            {
                task.CompletedAt = null;
            }

            // Update stats if completing a task
            if (task.IsCompleted && !wasCompleted)
            {
                TotalTasksCompleted++;
                UpdateStreak();

                // Extra celebration for Daily Highlight!
                if (task.IsDailyHighlight)
                {
                    TriggerConfetti();
                    MotivationalMessage = "🌟 Daily Highlight COMPLETE! Amazing work! 🌟";
                }
                // Show confetti if all today's tasks are complete
                else if (AllTodayTasksComplete)
                {
                    TriggerConfetti();
                }
            }
            else if (!task.IsCompleted && wasCompleted)
            {
                // Uncompleting a task
                TotalTasksCompleted = Math.Max(0, TotalTasksCompleted - 1);
            }

            // Update today's completion history
            UpdateDailyHistory();

            SaveData();
            UpdateMotivationalMessage();
            NotifyProgressChanged();
            NotifyEnergyChanged();
            RefreshBadges();
        }

        private void DeleteTask(TaskItem? task)
        {
            if (task == null) return;

            // Clear daily highlight if deleting highlighted task
            if (task.IsDailyHighlight)
            {
                _dailyHighlight = null;
                OnPropertyChanged(nameof(DailyHighlight));
                OnPropertyChanged(nameof(HasDailyHighlight));
            }

            Tasks.Remove(task);
            UpdateTodayTasks();
            UpdateDailyHistory();
            SaveData();
            UpdateMotivationalMessage();
            NotifyProgressChanged();
            NotifyEnergyChanged();
        }

        private void UpdateTodayTasks()
        {
            TodayTasks.Clear();
            foreach (var task in Tasks.Where(t => t.DueDate?.Date == DateTime.Today || t.DueDate == null))
            {
                TodayTasks.Add(task);
            }
            OnPropertyChanged(nameof(HasNoTasks));
            OnPropertyChanged(nameof(HasTasks));
        }

        private void UpdateCompletedTasks()
        {
            CompletedTasks.Clear();
            foreach (var task in Tasks.Where(t => t.IsCompleted).OrderByDescending(t => t.CompletedAt ?? t.CreatedAt))
            {
                CompletedTasks.Add(task);
            }
            OnPropertyChanged(nameof(HasCompletedTasks));
        }

        private void UpdateDailyHistory()
        {
            var key = DateTime.Today.ToString("yyyy-MM-dd");
            _dailyCompletionHistory[key] = TodayProgress;
            OnPropertyChanged(nameof(WeeklyData));
            OnPropertyChanged(nameof(WeeklyAverage));
        }

        #endregion

        #region Goal Methods

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

        private void IncrementGoal(Goal? goal)
        {
            if (goal == null) return;

            goal.CurrentProgress += 10;

            if (goal.IsCompleted)
            {
                TriggerConfetti();
            }

            SaveData();
        }

        private void DeleteGoal(Goal? goal)
        {
            if (goal == null) return;

            Goals.Remove(goal);
            SaveData();
        }

        #endregion

        #region Streak & Motivation

        private void UpdateStreak()
        {
            var today = DateTime.Today;

            // If streak is paused, resume it
            if (StreakPaused && PausedStreakValue > 0)
            {
                CurrentStreak = PausedStreakValue;
                StreakPaused = false;
                PausedStreakValue = 0;
                _lastCompletedDate = today;
                OnPropertyChanged(nameof(StreakStatusText));
                return;
            }

            if (_lastCompletedDate == null)
            {
                CurrentStreak = 1;
            }
            else if (_lastCompletedDate.Value.Date == today.AddDays(-1))
            {
                // Consecutive day
                CurrentStreak++;
            }
            else if (_lastCompletedDate.Value.Date != today)
            {
                // Streak would be broken - but we have gentle protection
                // If more than 2 days missed, reset
                var daysMissed = (today - _lastCompletedDate.Value.Date).Days;
                if (daysMissed > 2)
                {
                    CurrentStreak = 1;
                }
                // Otherwise keep the streak (gentle protection)
            }

            _lastCompletedDate = today;

            // Update best streak
            if (CurrentStreak > BestStreak)
            {
                BestStreak = CurrentStreak;
            }

            OnPropertyChanged(nameof(StreakStatusText));
        }

        private void UpdateMotivationalMessage()
        {
            var progress = TodayProgress;
            var remaining = RemainingTodayTasks;
            var hour = DateTime.Now.Hour;

            if (HasNoTasks)
            {
                MotivationalMessage = "No tasks yet. Add one and start winning! 🎯";
            }
            else if (AllTodayTasksComplete)
            {
                var messages = new[]
                {
                    "All done! You crushed it today! 🎉",
                    "Perfect day! You're unstoppable! 🏆",
                    "100% complete! Time to celebrate! 🥳",
                    "Nailed it! Take a well-deserved break! ☕"
                };
                MotivationalMessage = messages[DateTime.Now.Second % messages.Length];
            }
            else if (progress >= 80)
            {
                MotivationalMessage = "Almost there! Final push! 🚀";
            }
            else if (progress >= 70)
            {
                MotivationalMessage = "You're ahead today! Keep it up! 💫";
            }
            else if (remaining == 1)
            {
                MotivationalMessage = "Just ONE task left. You got this! 💪";
            }
            else if (progress >= 50)
            {
                MotivationalMessage = "Halfway there! Momentum is building! ⚡";
            }
            else if (progress > 0)
            {
                MotivationalMessage = "Great start! Keep the flow going! 🔥";
            }
            else if (hour < 12)
            {
                MotivationalMessage = "Fresh day, fresh start! Let's go! 🌅";
            }
            else if (hour < 17)
            {
                MotivationalMessage = "Afternoon power hour! Time to focus! ☀️";
            }
            else
            {
                MotivationalMessage = "Evening sprint! Finish strong! 🌙";
            }
        }

        private async void TriggerConfetti()
        {
            ShowConfetti = true;
            await System.Threading.Tasks.Task.Delay(3000);
            ShowConfetti = false;
        }

        private void InitializeBadges()
        {
            AllBadges.Clear();
            AllBadges.Add(new BadgeInfo { Emoji = "🎯", Name = "First Task", Description = "Complete your first task", IsUnlocked = HasFirstTaskBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "⚡", Name = "Productive", Description = "Complete 10 tasks", IsUnlocked = HasTenTasksBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "💪", Name = "Committed", Description = "Complete 50 tasks", IsUnlocked = HasFiftyTasksBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🏅", Name = "Elite", Description = "Complete 100 tasks", IsUnlocked = HasHundredTasksBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🔥", Name = "Week Warrior", Description = "7-day streak", IsUnlocked = HasWeekStreakBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🚀", Name = "Month Master", Description = "30-day streak", IsUnlocked = HasMonthStreakBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "💯", Name = "Perfect Day", Description = "100% completion", IsUnlocked = HasPerfectDayBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "✨", Name = "Perfect Week", Description = "10 perfect days", IsUnlocked = HasTenPerfectDaysBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🌅", Name = "Early Bird", Description = "Morning check-in", IsUnlocked = HasEarlyBirdBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🌙", Name = "Night Owl", Description = "Evening check-in", IsUnlocked = HasNightOwlBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🎖️", Name = "Goal Crusher", Description = "Complete a goal", IsUnlocked = HasGoalCrusherBadge });
            AllBadges.Add(new BadgeInfo { Emoji = "🧘", Name = "Focus Master", Description = "Complete 25 tasks", IsUnlocked = HasFocusMasterBadge });
        }

        private void RefreshBadges()
        {
            for (int i = 0; i < AllBadges.Count; i++)
            {
                var badge = AllBadges[i];
                badge.IsUnlocked = i switch
                {
                    0 => HasFirstTaskBadge,
                    1 => HasTenTasksBadge,
                    2 => HasFiftyTasksBadge,
                    3 => HasHundredTasksBadge,
                    4 => HasWeekStreakBadge,
                    5 => HasMonthStreakBadge,
                    6 => HasPerfectDayBadge,
                    7 => HasTenPerfectDaysBadge,
                    8 => HasEarlyBirdBadge,
                    9 => HasNightOwlBadge,
                    10 => HasGoalCrusherBadge,
                    11 => HasFocusMasterBadge,
                    _ => false
                };
            }
            OnPropertyChanged(nameof(AllBadges));
            OnPropertyChanged(nameof(UnlockedBadgesCount));
        }

        #endregion

        #region Focus Mode Methods

        private void EnterFocusMode(TaskItem? task)
        {
            if (task == null || task.IsCompleted) return;

            FocusTask = task;
            IsFocusMode = true;
            ResetPomodoro();
        }

        private void ExitFocusMode()
        {
            IsFocusMode = false;
            PausePomodoro();
            FocusTask = null;
        }

        private void StartPomodoro()
        {
            if (_pomodoroTimer == null)
            {
                _pomodoroTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _pomodoroTimer.Tick += PomodoroTick;
            }

            IsPomodoroRunning = true;
            _pomodoroTimer.Start();
        }

        private void PausePomodoro()
        {
            IsPomodoroRunning = false;
            _pomodoroTimer?.Stop();
        }

        private void ResetPomodoro()
        {
            PausePomodoro();
            PomodoroMinutes = 25;
            PomodoroSeconds = 0;
        }

        private void PomodoroTick(object? sender, EventArgs e)
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
                // Timer complete!
                PausePomodoro();
                MotivationalMessage = "🍅 Pomodoro complete! Take a break or keep going!";
            }
        }

        private void CompleteFocusTask()
        {
            if (FocusTask != null)
            {
                CompleteTask(FocusTask);
                ExitFocusMode();
            }
        }

        #endregion

        #region Daily Highlight Methods

        private void InitializeDailyHighlight()
        {
            // Try to restore today's highlight from saved data
            var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            
            // Find the task that was marked as daily highlight
            var existingHighlight = Tasks.FirstOrDefault(t => t.IsDailyHighlight && !t.IsCompleted);
            if (existingHighlight != null)
            {
                _dailyHighlight = existingHighlight;
                OnPropertyChanged(nameof(DailyHighlight));
                OnPropertyChanged(nameof(HasDailyHighlight));
                return;
            }

            // Auto-select if no highlight set
            AutoSelectDailyHighlight();
        }

        private void AutoSelectDailyHighlight()
        {
            // Priority: 
            // 1. Incomplete task from yesterday (carried over)
            // 2. Earliest due task today
            // 3. First task added today

            var yesterday = DateTime.Today.AddDays(-1);
            
            // Check for carried over task
            var carriedOver = Tasks.FirstOrDefault(t => 
                !t.IsCompleted && 
                t.DueDate?.Date == yesterday);
            
            if (carriedOver != null)
            {
                SetDailyHighlight(carriedOver);
                return;
            }

            // Earliest due task today
            var earliestToday = TodayTasks
                .Where(t => !t.IsCompleted && t.DueDate.HasValue)
                .OrderBy(t => t.DueDate)
                .FirstOrDefault();

            if (earliestToday != null)
            {
                SetDailyHighlight(earliestToday);
                return;
            }

            // First incomplete task
            var firstIncomplete = TodayTasks.FirstOrDefault(t => !t.IsCompleted);
            if (firstIncomplete != null)
            {
                SetDailyHighlight(firstIncomplete);
            }
        }

        private void SetDailyHighlight(TaskItem? task)
        {
            if (task == null) return;
            DailyHighlight = task;
            SaveData();
        }

        private void ClearDailyHighlight()
        {
            DailyHighlight = null;
            SaveData();
        }

        #endregion

        #region Streak Protection Methods

        private void PauseStreak()
        {
            if (CurrentStreak > 0 && !StreakPaused)
            {
                PausedStreakValue = CurrentStreak;
                StreakPaused = true;
                MotivationalMessage = $"⏸️ Streak paused! Your {PausedStreakValue} day streak is safe.";
                OnPropertyChanged(nameof(StreakStatusText));
                SaveData();
            }
        }

        private void ResumeStreak()
        {
            if (StreakPaused)
            {
                CurrentStreak = PausedStreakValue;
                StreakPaused = false;
                PausedStreakValue = 0;
                _lastCompletedDate = DateTime.Today;
                MotivationalMessage = $"🔥 Streak resumed! You're back at {CurrentStreak} days!";
                OnPropertyChanged(nameof(StreakStatusText));
                SaveData();
            }
        }

        #endregion

        #region Check-in Methods

        private void CheckForCheckInPrompt()
        {
            var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            _todayReflection = _reflections.GetValueOrDefault(todayKey);

            if (ShouldShowCheckIn)
            {
                if (IsMorningTime && (_todayReflection == null || !_todayReflection.HasMorning))
                {
                    ShowMorningCheckIn = true;
                }
                else if (IsEveningTime && (_todayReflection == null || !_todayReflection.HasEvening))
                {
                    ShowEveningCheckIn = true;
                }
            }
        }

        private void SaveMorningCheckIn()
        {
            if (string.IsNullOrWhiteSpace(MorningReflection)) return;

            var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            
            if (_todayReflection == null)
            {
                _todayReflection = new DailyReflection { Date = DateTime.Today };
                _reflections[todayKey] = _todayReflection;
            }

            _todayReflection.MorningReflection = MorningReflection;
            _todayReflection.UpdatedAt = DateTime.Now;

            ShowMorningCheckIn = false;
            MorningReflection = string.Empty;
            MotivationalMessage = "☀️ Morning intention set! Let's make it happen!";
            SaveData();
        }

        private void SaveEveningCheckIn()
        {
            if (string.IsNullOrWhiteSpace(EveningReflection)) return;

            var todayKey = DateTime.Today.ToString("yyyy-MM-dd");
            
            if (_todayReflection == null)
            {
                _todayReflection = new DailyReflection { Date = DateTime.Today };
                _reflections[todayKey] = _todayReflection;
            }

            _todayReflection.EveningReflection = EveningReflection;
            _todayReflection.UpdatedAt = DateTime.Now;

            ShowEveningCheckIn = false;
            EveningReflection = string.Empty;
            MotivationalMessage = "🌙 Great reflection! Rest well, champion!";
            SaveData();
        }

        private void DismissCheckIn()
        {
            ShowMorningCheckIn = false;
            ShowEveningCheckIn = false;
        }

        #endregion

        #region Energy Tag Methods

        private void CycleNewTaskEnergy()
        {
            NewTaskEnergy = NewTaskEnergy switch
            {
                EnergyTag.Quick => EnergyTag.Deep,
                EnergyTag.Deep => EnergyTag.LowEnergy,
                EnergyTag.LowEnergy => EnergyTag.Quick,
                _ => EnergyTag.Quick
            };
        }

        private void NotifyEnergyChanged()
        {
            OnPropertyChanged(nameof(DeepWorkTaskCount));
            OnPropertyChanged(nameof(QuickWinTaskCount));
            OnPropertyChanged(nameof(LowEnergyTaskCount));
            OnPropertyChanged(nameof(EnergySuggestion));
        }

        #endregion

        #region Theme

        private void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        #endregion

        #region Data Persistence

        private void LoadData()
        {
            var data = JsonStorageService.Load();

            foreach (var task in data.Tasks)
                Tasks.Add(task);

            foreach (var goal in data.Goals)
                Goals.Add(goal);

            CurrentStreak = data.CurrentStreak;
            BestStreak = data.BestStreak;
            TotalTasksCompleted = data.TotalTasksCompleted;
            _lastCompletedDate = data.LastCompletedDate;
            _dailyCompletionHistory = data.DailyCompletionHistory ?? new();
            IsDarkMode = data.IsDarkMode;

            // Load streak protection
            _streakPaused = data.StreakPaused;
            _pausedStreakValue = data.PausedStreakValue;

            // Load reflections
            _reflections = data.Reflections ?? new();

            UpdateTodayTasks();
        }

        private void SaveData()
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
                Reflections = _reflections
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
            OnPropertyChanged(nameof(ProgressColor));
            OnPropertyChanged(nameof(HasNoTasks));
            OnPropertyChanged(nameof(HasTasks));
            OnPropertyChanged(nameof(WeeklyData));
            OnPropertyChanged(nameof(WeeklyAverage));
            OnPropertyChanged(nameof(OverallCompletionRate));
        }

        #endregion
    }

    #region RelayCommand Implementation

    /// <summary>
    /// Simple ICommand implementation for MVVM binding
    /// </summary>
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

    /// <summary>
    /// Generic RelayCommand with parameter support
    /// </summary>
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
