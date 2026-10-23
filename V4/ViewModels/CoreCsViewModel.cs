using System.Collections.ObjectModel;
using System.Windows.Input;
using Momentum.Content;
using Momentum.Core;
using Momentum.Models;

namespace Momentum.ViewModels
{
    public sealed class SubjectRow : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int Known { get; set; }
        public int Total { get; set; }
        public int Due { get; set; }
        public double Progress => Total == 0 ? 0 : (double)Known / Total;
        public string CountText => $"{Known}/{Total} known" + (Due > 0 ? $" · {Due} due" : string.Empty);
        public void Refresh() => RaiseAll();
    }

    public sealed class CoreCsViewModel : PageViewModel
    {
        private SubjectRow? _selectedSubject;
        private string _search = string.Empty;
        private string _filter = "All";
        private bool _isAdding;
        private string _newSubject = "OS";
        private string _newQuestion = string.Empty;
        private string _newAnswer = string.Empty;

        public CoreCsViewModel()
        {
            Subjects.Add(new SubjectRow { Key = string.Empty, Name = "All subjects" });
            foreach (var (key, name) in CsCatalog.Subjects) Subjects.Add(new SubjectRow { Key = key, Name = name });
            _selectedSubject = Subjects[0];

            ToggleCommand = new RelayCommand<CardItem>(c => c.IsExpanded = !c.IsExpanded);
            KnewCommand = new RelayCommand<CardItem>(c => Grade(c, Models.Grade.Good));
            ShakyCommand = new RelayCommand<CardItem>(c => Grade(c, Models.Grade.Hard));
            ForgotCommand = new RelayCommand<CardItem>(c => Grade(c, Models.Grade.Again));
            StarCommand = new RelayCommand<CardItem>(c => c.Starred = !c.Starred);
            DeleteCommand = new RelayCommand<CardItem>(c => { Store.DeleteCustomCard(c); OnShown(); });
            StudyCommand = new RelayCommand(() => Shell.Navigate<ReviewViewModel>("review").StartCards());
            StartAddCommand = new RelayCommand(() => IsAdding = true);
            CancelAddCommand = new RelayCommand(() => IsAdding = false);
            AddCommand = new RelayCommand(AddCard, () => !string.IsNullOrWhiteSpace(NewQuestion) && !string.IsNullOrWhiteSpace(NewAnswer));
        }

        public ObservableCollection<SubjectRow> Subjects { get; } = new();
        public ObservableCollection<CardItem> Cards { get; } = new();
        public IReadOnlyList<(string Key, string Name)> SubjectChoices => CsCatalog.Subjects;

        public ICommand ToggleCommand { get; }
        public ICommand KnewCommand { get; }
        public ICommand ShakyCommand { get; }
        public ICommand ForgotCommand { get; }
        public ICommand StarCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StudyCommand { get; }
        public ICommand StartAddCommand { get; }
        public ICommand CancelAddCommand { get; }
        public ICommand AddCommand { get; }

        public SubjectRow? SelectedSubject
        {
            get => _selectedSubject;
            set { if (value != null && Set(ref _selectedSubject, value)) ApplyFilter(); }
        }

        public string Search { get => _search; set { if (Set(ref _search, value)) ApplyFilter(); } }
        public string Filter { get => _filter; set { if (Set(ref _filter, value)) ApplyFilter(); } }

        public int DueCount { get; private set; }
        public int KnownCount { get; private set; }
        public int TotalCount { get; private set; }
        public string ResultText { get; private set; } = string.Empty;

        public bool IsAdding { get => _isAdding; set => Set(ref _isAdding, value); }
        public string NewSubject { get => _newSubject; set => Set(ref _newSubject, value); }
        public string NewQuestion { get => _newQuestion; set => Set(ref _newQuestion, value); }
        public string NewAnswer { get => _newAnswer; set => Set(ref _newAnswer, value); }

        public override void OnShown()
        {
            foreach (var row in Subjects)
            {
                var cards = row.Key.Length == 0 ? Store.Cards : Store.Cards.Where(c => c.Subject == row.Key).ToList();
                row.Known = cards.Count(c => c.IsKnown);
                row.Total = cards.Count;
                row.Due = cards.Count(c => c.IsDue);
                row.Refresh();
            }
            DueCount = Store.Cards.Count(c => c.IsDue);
            KnownCount = Store.Cards.Count(c => c.IsKnown);
            TotalCount = Store.Cards.Count;
            OnPropertyChanged(nameof(DueCount));
            OnPropertyChanged(nameof(KnownCount));
            OnPropertyChanged(nameof(TotalCount));
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var key = SelectedSubject?.Key ?? string.Empty;
            var q = Store.Cards.AsEnumerable();
            if (key.Length > 0) q = q.Where(c => c.Subject == key);
            q = Filter switch
            {
                "New" => q.Where(c => c.IsNew),
                "Due" => q.Where(c => c.IsDue),
                "Shaky" => q.Where(c => !c.IsNew && !c.IsKnown),
                "Known" => q.Where(c => c.IsKnown),
                "Starred" => q.Where(c => c.Starred),
                _ => q,
            };
            if (!string.IsNullOrWhiteSpace(Search))
            {
                var s = Search.Trim();
                q = q.Where(c => c.Question.Contains(s, StringComparison.OrdinalIgnoreCase) || c.Answer.Contains(s, StringComparison.OrdinalIgnoreCase));
            }
            var list = q.ToList();
            Cards.Clear();
            foreach (var c in list) Cards.Add(c);
            ResultText = $"{list.Count} card{(list.Count == 1 ? "" : "s")}";
            OnPropertyChanged(nameof(ResultText));
        }

        private void Grade(CardItem card, Grade grade)
        {
            Store.ReviewCard(card, grade);
            card.IsExpanded = false;
            Shell.ShowToast($"Next review in {Services.Srs.FormatInterval(card.Progress.Srs.Interval)}");
            OnShown();
        }

        private void AddCard()
        {
            var item = Store.AddCustomCard(new CustomCard { Subject = NewSubject, Question = NewQuestion.Trim(), Answer = NewAnswer.Trim() });
            NewQuestion = NewAnswer = string.Empty;
            IsAdding = false;
            OnShown();
            Shell.ShowToast("Card added — it'll show up in your reviews.");
        }
    }
}
