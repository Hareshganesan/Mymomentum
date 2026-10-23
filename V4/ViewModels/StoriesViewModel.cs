using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class ThemeToggle : ObservableObject
    {
        private readonly StoryCard _owner;
        public ThemeToggle(StoryCard owner, string name) { _owner = owner; Name = name; }
        public string Name { get; }
        public bool IsOn
        {
            get => _owner.Model.Themes.Contains(Name);
            set
            {
                if (value && !_owner.Model.Themes.Contains(Name)) _owner.Model.Themes.Add(Name);
                if (!value) _owner.Model.Themes.Remove(Name);
                OnPropertyChanged();
                _owner.Touch(nameof(StoryCard.ThemesText));
            }
        }
    }

    public sealed class StoryCard : ObservableObject
    {
        public StoryCard(Story model)
        {
            Model = model;
            Themes = BehavioralCatalog.Themes.Select(t => new ThemeToggle(this, t)).ToList();
        }

        public Story Model { get; }
        public List<ThemeToggle> Themes { get; }
        public event Action? Edited;

        internal void Touch(string? name)
        {
            Model.UpdatedAt = DateTime.Now;
            OnPropertyChanged(name);
            OnPropertyChanged(nameof(Completeness));
            AppStore.Instance.ScheduleSave();
            Edited?.Invoke();
        }

        public string Title { get => Model.Title; set { Model.Title = value; Touch(nameof(Title)); OnPropertyChanged(nameof(DisplayTitle)); } }
        public string Situation { get => Model.Situation; set { Model.Situation = value; Touch(nameof(Situation)); } }
        public string Task { get => Model.Task; set { Model.Task = value; Touch(nameof(Task)); } }
        public string Action { get => Model.Action; set { Model.Action = value; Touch(nameof(Action)); } }
        public string Result { get => Model.Result; set { Model.Result = value; Touch(nameof(Result)); } }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Model.Title) ? "Untitled story" : Model.Title;
        public string ThemesText => Model.Themes.Count == 0 ? "No themes tagged" : string.Join(" · ", Model.Themes);
        public double Completeness => new[] { Model.Situation, Model.Task, Model.Action, Model.Result }.Count(s => s.Trim().Length >= 20) / 4.0;
    }

    public sealed class QuestionRow
    {
        public BehavioralQuestionDef Def { get; init; } = null!;
        public bool Covered { get; init; }
        public string CoverText { get; init; } = string.Empty;
    }

    public sealed class StoriesViewModel : PageViewModel
    {
        private StoryCard? _selected;
        private QuestionRow? _selectedQuestion;
        private bool _missingOnly;

        public StoriesViewModel()
        {
            NewStoryCommand = new RelayCommand(NewStory);
            SelectCommand = new RelayCommand<StoryCard>(s => Selected = s);
            DeleteCommand = new RelayCommand(Delete, () => Selected != null);
            WriteForQuestionCommand = new RelayCommand(WriteForQuestion);
        }

        public ICommand WriteForQuestionCommand { get; }
        public ObservableCollection<QuestionRow> VisibleQuestions { get; } = new();
        public double CoverageRatio => TotalQuestions == 0 ? 0 : (double)CoveredQuestions / TotalQuestions;

        public QuestionRow? SelectedQuestion { get => _selectedQuestion; set => Set(ref _selectedQuestion, value); }
        public bool MissingOnly { get => _missingOnly; set { if (Set(ref _missingOnly, value)) FilterQuestions(); } }

        private void FilterQuestions()
        {
            var keep = SelectedQuestion?.Def.Id;
            VisibleQuestions.Clear();
            foreach (var q in Questions.Where(q => !MissingOnly || !q.Covered)) VisibleQuestions.Add(q);
            SelectedQuestion = VisibleQuestions.FirstOrDefault(q => q.Def.Id == keep);
        }

        /// <summary>Starts a story already tagged with the selected question's theme.</summary>
        private void WriteForQuestion()
        {
            var q = SelectedQuestion;
            if (q == null) return;
            if (q.Def.Theme == "Pitch") { Shell.ShowToast("Answer this one with the pitch builder at the top."); return; }
            NewStory();
            Selected!.Title = q.Def.Question.TrimEnd('.', '?');
            Selected.Themes.First(t => t.Name == q.Def.Theme).IsOn = true;
        }

        public ObservableCollection<StoryCard> Stories { get; } = new();
        public ObservableCollection<QuestionRow> Questions { get; } = new();

        public ICommand NewStoryCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand DeleteCommand { get; }

        public StoryCard? Selected { get => _selected; set { Set(ref _selected, value); OnPropertyChanged(nameof(HasSelection)); } }
        public bool HasSelection => Selected != null;

        public string PitchPresent { get => Store.State.Profile.PitchPresent; set { Store.State.Profile.PitchPresent = value; PitchChanged(); } }
        public string PitchPast { get => Store.State.Profile.PitchPast; set { Store.State.Profile.PitchPast = value; PitchChanged(); } }
        public string PitchFuture { get => Store.State.Profile.PitchFuture; set { Store.State.Profile.PitchFuture = value; PitchChanged(); } }
        public string PitchLength
        {
            get
            {
                var words = (PitchPresent + " " + PitchPast + " " + PitchFuture).Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                var seconds = (int)Math.Round(words / 2.5);
                return words == 0 ? "Aim for 60–90 seconds spoken (~150–220 words)." : $"{words} words · about {seconds} seconds spoken" + (seconds > 100 ? " — trim it a little." : seconds < 45 ? " — add a proof point." : " — good length.");
            }
        }

        public int CoveredThemes { get; private set; }
        public int TotalThemes => BehavioralCatalog.Themes.Count;
        public int CoveredQuestions { get; private set; }
        public int TotalQuestions => BehavioralCatalog.Questions.Count;

        private void PitchChanged()
        {
            Store.ScheduleSave();
            OnPropertyChanged(nameof(PitchLength));
        }

        public override void OnShown()
        {
            var keep = Selected?.Model;
            Stories.Clear();
            foreach (var s in Store.State.Stories.OrderByDescending(s => s.UpdatedAt))
            {
                var card = new StoryCard(s);
                card.Edited += RefreshCoverage;
                Stories.Add(card);
            }
            Selected = Stories.FirstOrDefault(s => s.Model == keep) ?? Stories.FirstOrDefault();
            RefreshCoverage();
            RaiseAll();
        }

        private void RefreshCoverage()
        {
            var stories = Store.State.Stories;
            bool hasPitch = !string.IsNullOrWhiteSpace(Store.State.Profile.PitchPresent);
            Questions.Clear();
            foreach (var q in BehavioralCatalog.Questions)
            {
                var matching = stories.Where(s => s.Themes.Contains(q.Theme)).Select(s => string.IsNullOrWhiteSpace(s.Title) ? "Untitled" : s.Title).ToList();
                bool covered = q.Theme == "Pitch" ? hasPitch : matching.Count > 0;
                Questions.Add(new QuestionRow
                {
                    Def = q,
                    Covered = covered,
                    CoverText = q.Theme == "Pitch" ? (hasPitch ? "Covered by your pitch" : "Use the pitch builder") : covered ? "Story: " + string.Join(", ", matching) : $"Needs a '{q.Theme}' story",
                });
            }
            CoveredThemes = BehavioralCatalog.Themes.Count(t => stories.Any(s => s.Themes.Contains(t)));
            CoveredQuestions = Questions.Count(q => q.Covered);
            OnPropertyChanged(nameof(CoveredThemes));
            OnPropertyChanged(nameof(CoveredQuestions));
            OnPropertyChanged(nameof(CoverageRatio));
            FilterQuestions();
        }

        private void NewStory()
        {
            var story = new Story();
            Store.State.Stories.Add(story);
            Store.ScheduleSave();
            var card = new StoryCard(story);
            card.Edited += RefreshCoverage;
            Stories.Insert(0, card);
            Selected = card;
        }

        private void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete \"{Selected.DisplayTitle}\"?", "Momentum", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Store.State.Stories.Remove(Selected.Model);
            Stories.Remove(Selected);
            Selected = Stories.FirstOrDefault();
            Store.ScheduleSave();
            RefreshCoverage();
        }
    }
}
