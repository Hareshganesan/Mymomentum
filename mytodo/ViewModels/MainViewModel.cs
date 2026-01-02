using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MyTodo.Models;
using MyTodo.Services;

namespace MyTodo.ViewModels
{
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

        #endregion

        #region Collections

        public ObservableCollection<TaskItem> Tasks { get; } = new();
        public ObservableCollection<Goal> Goals { get; } = new();

        /// <summary>
        /// Filtered view showing only today's tasks
        /// </summary>
        public ObservableCollection<TaskItem> TodayTasks { get; } = new();

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

        #region Commands

        public ICommand AddTaskCommand { get; }
        public ICommand CompleteTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand AddGoalCommand { get; }
        public ICommand IncrementGoalCommand { get; }
        public ICommand DeleteGoalCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        #endregion

        #region Constructor

        public MainViewModel()
        {
            // Initialize commands
            AddTaskCommand = new RelayCommand(AddTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
            CompleteTaskCommand = new RelayCommand<TaskItem>(CompleteTask);
            DeleteTaskCommand = new RelayCommand<TaskItem>(DeleteTask);
            AddGoalCommand = new RelayCommand(AddGoal, () => !string.IsNullOrWhiteSpace(NewGoalTitle));
            IncrementGoalCommand = new RelayCommand<Goal>(IncrementGoal);
            DeleteGoalCommand = new RelayCommand<Goal>(DeleteGoal);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            // Load saved data
            LoadData();

            // Update motivational message
            UpdateMotivationalMessage();
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
                IsAnimating = true // Trigger add animation
            };

            Tasks.Insert(0, task);
            UpdateTodayTasks();

            // Clear input
            NewTaskTitle = string.Empty;
            NewTaskDueDate = null;

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

            // Update stats if completing a task
            if (task.IsCompleted && !wasCompleted)
            {
                TotalTasksCompleted++;
                UpdateStreak();

                // Show confetti if all today's tasks are complete
                if (AllTodayTasksComplete)
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
        }

        private void DeleteTask(TaskItem? task)
        {
            if (task == null) return;

            Tasks.Remove(task);
            UpdateTodayTasks();
            UpdateDailyHistory();
            SaveData();
            UpdateMotivationalMessage();
            NotifyProgressChanged();
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
                // Streak broken
                CurrentStreak = 1;
            }

            _lastCompletedDate = today;

            // Update best streak
            if (CurrentStreak > BestStreak)
            {
                BestStreak = CurrentStreak;
            }
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
                IsDarkMode = IsDarkMode
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
