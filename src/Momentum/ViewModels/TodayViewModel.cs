using System.Collections.ObjectModel;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class PlanItemViewModel : ObservableObject
    {
        private bool _isGrading;

        public PlanItemViewModel(PlanItem model, ProblemItem? problem)
        {
            Model = model;
            Problem = problem;
        }

        public PlanItem Model { get; }
        public ProblemItem? Problem { get; }

        public PlanKind Kind => Model.Kind;
        public string Title => Model.Title;
        public string Subtitle => Model.Subtitle;
        public string MinutesText => Model.Minutes > 0 ? $"{Model.Minutes} min" : string.Empty;
        public bool IsNewProblem => Kind == PlanKind.NewProblem;
        public bool IsReview => Kind == PlanKind.Review;
        public bool IsCards => Kind == PlanKind.Cards;
        public bool IsManual => Kind is PlanKind.Event or PlanKind.Task;
        public bool IsAuto => !IsManual;
        public bool IsTask => Kind == PlanKind.Task;
        public bool HasDifficulty => Problem != null;
        public string Glyph => Kind switch
        {
            PlanKind.Review => "",
            PlanKind.NewProblem => "",
            PlanKind.Cards => "",
            PlanKind.Event => "",
            _ => "",
        };

        public bool IsDone
        {
            get => PlanService.IsSatisfied(AppStore.Instance, Model);
            set
            {
                if (!IsManual) return;
                Model.Done = value;
                if (Kind == PlanKind.Task)
                {
                    var task = AppStore.Instance.State.Tasks.FirstOrDefault(t => t.Id == Model.RefId);
                    if (task != null) task.Done = value;
                }
                AppStore.Instance.ScheduleSave();
                OnPropertyChanged();
            }
        }

        public bool IsGrading { get => _isGrading; set => Set(ref _isGrading, value); }

        public void Refresh() => RaiseAll();
    }

    public sealed class UpcomingEvent
    {
        public string Company { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string When { get; init; } = string.Empty;
        public bool IsSoon { get; init; }
    }

    public sealed class PatternProgress
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int Solved { get; init; }
        public int Total { get; init; }
        public bool IsFocus { get; init; }
        public double Ratio => Total == 0 ? 0 : (double)Solved / Total;
        public string Count => $"{Solved}/{Total}";
    }

    public sealed class TodayViewModel : PageViewModel
    {
        public List<PatternProgress> Patterns { get; private set; } = new();
        public ICommand OpenPatternCommand { get; }

        private string _newTaskTitle = string.Empty;

        public TodayViewModel()
        {
            OpenProblemCommand = new RelayCommand<PlanItemViewModel>(OpenProblem);
            StartGradingCommand = new RelayCommand<PlanItemViewModel>(i => i.IsGrading = !i.IsGrading);
            GradeAgainCommand = new RelayCommand<PlanItemViewModel>(i => ApplyGrade(i, Models.Grade.Again));
            GradeHardCommand = new RelayCommand<PlanItemViewModel>(i => ApplyGrade(i, Models.Grade.Hard));
            GradeGoodCommand = new RelayCommand<PlanItemViewModel>(i => ApplyGrade(i, Models.Grade.Good));
            GradeEasyCommand = new RelayCommand<PlanItemViewModel>(i => ApplyGrade(i, Models.Grade.Easy));
            ReviewItemCommand = new RelayCommand<PlanItemViewModel>(i => Shell.Navigate<ReviewViewModel>("review").StartProblems(i.Problem));
            StartReviewCommand = new RelayCommand(() => Shell.Navigate<ReviewViewModel>("review").StartMixed());
            StudyCardsCommand = new RelayCommand(() => Shell.Navigate<ReviewViewModel>("review").StartCards());
            RegenerateCommand = new RelayCommand(() => { PlanService.GetOrBuildToday(Store, forceRebuild: true); AppendTasks(Store.State.Plan!); OnShown(); Shell.ShowToast("Plan rebuilt from your latest progress."); });
            AddTaskCommand = new RelayCommand(AddTask, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
            RemoveTaskCommand = new RelayCommand<PlanItemViewModel>(RemoveTask);
            NextBestCommand = new RelayCommand(() => { if (NextBestNav.Length > 0) Shell.Navigate(NextBestNav); });
            FocusPatternCommand = new RelayCommand(() => { if (FocusPatternKey.Length > 0) Shell.Navigate<ProblemsViewModel>("problems").ShowPattern(FocusPatternKey); });
            OpenPipelineCommand = new RelayCommand(() => Shell.Navigate("pipeline"));
            OpenPatternCommand = new RelayCommand<string>(k => Shell.Navigate<ProblemsViewModel>("problems").ShowPattern(k));
        }

        public ObservableCollection<PlanItemViewModel> Plan { get; } = new();
        public ObservableCollection<UpcomingEvent> Upcoming { get; } = new();

        public ICommand OpenProblemCommand { get; }
        public ICommand StartGradingCommand { get; }
        public ICommand GradeAgainCommand { get; }
        public ICommand GradeHardCommand { get; }
        public ICommand GradeGoodCommand { get; }
        public ICommand GradeEasyCommand { get; }
        public ICommand ReviewItemCommand { get; }
        public ICommand StartReviewCommand { get; }
        public ICommand StudyCardsCommand { get; }
        public ICommand RegenerateCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand RemoveTaskCommand { get; }
        public ICommand NextBestCommand { get; }
        public ICommand FocusPatternCommand { get; }
        public ICommand OpenPipelineCommand { get; }

        public string Greeting { get; private set; } = string.Empty;
        public string DateText => DateTime.Now.ToString("dddd, d MMMM");
        public string CountdownText { get; private set; } = string.Empty;
        public string CountdownSub { get; private set; } = string.Empty;

        public double ReadinessScore { get; private set; }
        public string ReadinessLevel { get; private set; } = string.Empty;
        public string NextBestText { get; private set; } = string.Empty;
        public string NextBestNav { get; private set; } = string.Empty;

        public int SheetSolved { get; private set; }
        public int SheetTotal { get; private set; }
        public double SheetProgress => SheetTotal == 0 ? 0 : (double)SheetSolved / SheetTotal;
        public string PaceText { get; private set; } = string.Empty;
        public string PaceStatus { get; private set; } = string.Empty;
        public bool IsOnTrack { get; private set; }

        public int DueProblems { get; private set; }
        public int DueCards { get; private set; }
        public int DueTotal => DueProblems + DueCards;
        public string DueDetail { get; private set; } = string.Empty;

        public int PlanDone { get; private set; }
        public int PlanTotal { get; private set; }
        public double PlanProgress => PlanTotal == 0 ? 0 : (double)PlanDone / PlanTotal;
        public string PlanSummary { get; private set; } = string.Empty;
        public bool PlanComplete => PlanTotal > 0 && PlanDone == PlanTotal;

        public string FocusPatternKey { get; private set; } = string.Empty;
        public string FocusPatternName { get; private set; } = string.Empty;
        public string FocusPatternCue { get; private set; } = string.Empty;
        public string FocusPatternStats { get; private set; } = string.Empty;
        public double FocusPatternProgress { get; private set; }

        public Dictionary<string, int> Activity { get; private set; } = new();
        public string ActivitySummary { get; private set; } = string.Empty;

        public string NewTaskTitle { get => _newTaskTitle; set => Set(ref _newTaskTitle, value); }

        public override void OnShown()
        {
            var state = Store.State;
            var name = state.Profile.Name;
            var hour = DateTime.Now.Hour;
            var part = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
            Greeting = string.IsNullOrWhiteSpace(name) ? part : $"{part}, {name}";

            var target = state.Profile.TargetDate;
            if (target.HasValue)
            {
                var days = (target.Value.Date - DateTime.Today).Days;
                CountdownText = days > 0 ? $"{days} days to go" : days == 0 ? "It's today" : "Season is live";
                CountdownSub = target.Value.ToString("d MMM yyyy");
            }
            else
            {
                CountdownText = "No target date";
                CountdownSub = "Set one in Settings";
            }

            var r = ReadinessService.Compute(Store);
            ReadinessScore = r.Score;
            ReadinessLevel = r.Level;
            NextBestText = r.NextBest?.Action ?? string.Empty;
            NextBestNav = r.NextBest?.NavKey ?? string.Empty;

            var sheet = Store.SheetProblems.ToList();
            SheetSolved = sheet.Count(p => p.IsSolved);
            SheetTotal = sheet.Count;
            var need = PlanService.RequiredPerDay(Store);
            var recent = Store.SolvedInLastDays(7) / 7.0;
            IsOnTrack = need == 0 || recent >= need * 0.9;
            PaceText = need == 0 ? "Sheet complete — keep reviewing." : $"Need {need:0.#}/day · last 7 days {recent:0.#}/day";
            PaceStatus = need == 0 ? "Done" : IsOnTrack ? "On track" : "Behind pace";

            DueProblems = Store.DueProblems.Count();
            DueCards = Store.DueCards.Count();
            var minutes = DueProblems * PlanService.MinutesPerReview + (int)Math.Ceiling(DueCards * PlanService.MinutesPerCard);
            DueDetail = DueTotal == 0 ? "Nothing due. Your memory is fresh." : $"{DueProblems} problems · {DueCards} cards · ~{minutes} min";

            var plan = PlanService.GetOrBuildToday(Store);
            AppendTasks(plan);
            Plan.Clear();
            foreach (var item in plan.Items)
            {
                var vm = new PlanItemViewModel(item, Store.ProblemsBySlug.GetValueOrDefault(item.RefId));
                vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(PlanItemViewModel.IsDone)) UpdatePlanStats(); };
                Plan.Add(vm);
            }
            UpdatePlanStats();

            var focus = PlanService.FocusPattern(Store);
            FocusPatternKey = focus?.Key ?? string.Empty;
            FocusPatternName = focus?.Name ?? "Every pattern is 60%+ done";
            FocusPatternCue = focus?.Cue ?? "Keep up your reviews and run mock interviews.";
            if (focus != null)
            {
                var items = sheet.Where(p => p.PatternKey == focus.Key).ToList();
                FocusPatternStats = $"{items.Count(p => p.IsSolved)} of {items.Count} solved";
                FocusPatternProgress = items.Count == 0 ? 0 : (double)items.Count(p => p.IsSolved) / items.Count;
            }

            Upcoming.Clear();
            foreach (var app in PlanService.UpcomingEvents(state, 21).Take(4))
            {
                var d = (app.NextEventDate!.Value.Date - DateTime.Today).Days;
                Upcoming.Add(new UpcomingEvent
                {
                    Company = app.Company,
                    Title = string.IsNullOrWhiteSpace(app.NextEventTitle) ? app.Stage.ToString() : app.NextEventTitle,
                    When = d == 0 ? "Today" : d == 1 ? "Tomorrow" : $"In {d} days",
                    IsSoon = d <= 3,
                });
            }

            Patterns = ProblemCatalog.Patterns.Select(pt =>
            {
                var items = sheet.Where(x => x.PatternKey == pt.Key).ToList();
                return new PatternProgress { Key = pt.Key, Name = pt.Name, Solved = items.Count(x => x.IsSolved), Total = items.Count, IsFocus = pt.Key == FocusPatternKey };
            }).ToList();

            Activity = Store.MergedActivity();
            var activeDays = Enumerable.Range(0, 28).Count(i => Activity.GetValueOrDefault(DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd")) > 0);
            ActivitySummary = $"Active {activeDays} of the last 28 days";

            RaiseAll();
        }

        /// <summary>Custom tasks for today live alongside the generated plan.</summary>
        private void AppendTasks(DayPlan plan)
        {
            foreach (var task in Store.State.Tasks.Where(t => t.Date == plan.Date))
            {
                if (plan.Items.Any(i => i.Kind == PlanKind.Task && i.RefId == task.Id)) continue;
                plan.Items.Add(new PlanItem { Kind = PlanKind.Task, RefId = task.Id, Title = task.Title, Subtitle = "Your task", Done = task.Done });
            }
        }

        private void UpdatePlanStats()
        {
            PlanTotal = Plan.Count;
            PlanDone = Plan.Count(p => p.IsDone);
            var left = Plan.Where(p => !p.IsDone).Sum(p => p.Model.Minutes);
            PlanSummary = PlanTotal == 0 ? "Nothing planned" : PlanDone == PlanTotal ? "All done — great day." : $"{PlanDone} of {PlanTotal} done · ~{left} min left";
            OnPropertyChanged(nameof(PlanTotal));
            OnPropertyChanged(nameof(PlanDone));
            OnPropertyChanged(nameof(PlanProgress));
            OnPropertyChanged(nameof(PlanSummary));
            OnPropertyChanged(nameof(PlanComplete));
        }

        private void OpenProblem(PlanItemViewModel item)
        {
            if (item.Problem == null) return;
            Links.Open(item.Problem.Url);
            if (item.Problem.IsTodo)
            {
                Store.SetStatus(item.Problem, ProblemStatus.Attempted);
            }
        }

        private void ApplyGrade(PlanItemViewModel item, Grade grade)
        {
            if (item.Problem == null) return;
            Store.MarkSolved(item.Problem, grade);
            item.IsGrading = false;
            item.Refresh();
            Shell.ShowToast($"{item.Problem.Title} solved · first review in {Srs.FormatInterval(item.Problem.Progress.Srs.Interval)}");
            OnShown();
        }

        private void AddTask()
        {
            var task = new CustomTask { Title = NewTaskTitle.Trim(), Date = DateTime.Today.ToString("yyyy-MM-dd") };
            Store.State.Tasks.Add(task);
            NewTaskTitle = string.Empty;
            Store.ScheduleSave();
            OnShown();
        }

        private void RemoveTask(PlanItemViewModel item)
        {
            Store.State.Tasks.RemoveAll(t => t.Id == item.Model.RefId);
            Store.State.Plan?.Items.Remove(item.Model);
            Store.ScheduleSave();
            OnShown();
        }
    }
}
