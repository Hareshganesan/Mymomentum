using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Momentum.Core;
using Momentum.Models;
using Momentum.Services;

namespace Momentum.ViewModels
{
    /// <summary>Editable wrapper around a <see cref="JobApplication"/>; edits write straight through and autosave.</summary>
    public sealed class ApplicationCard : ObservableObject
    {
        public ApplicationCard(JobApplication model) => Model = model;

        public JobApplication Model { get; }

        private void Changed(string? name = null)
        {
            OnPropertyChanged(name);
            OnPropertyChanged(nameof(EventText));
            OnPropertyChanged(nameof(IsSoon));
            OnPropertyChanged(nameof(HasEvent));
            AppStore.Instance.ScheduleSave();
        }

        public string Company { get => Model.Company; set { Model.Company = value; Changed(); OnPropertyChanged(nameof(PrepText)); } }
        public string Role { get => Model.Role; set { Model.Role = value; Changed(); } }
        public string Ctc { get => Model.Ctc; set { Model.Ctc = value; Changed(); } }
        public string NextEventTitle { get => Model.NextEventTitle; set { Model.NextEventTitle = value; Changed(); } }
        public DateTime? NextEventDate { get => Model.NextEventDate; set { Model.NextEventDate = value; Changed(); } }
        public string Url { get => Model.Url; set { Model.Url = value; Changed(); } }
        public string Notes { get => Model.Notes; set { Model.Notes = value; Changed(); } }
        public Stage Stage { get => Model.Stage; set { Model.Stage = value; Changed(); } }

        public bool HasEvent => Model.NextEventDate.HasValue && Model.NextEventDate.Value.Date >= DateTime.Today;
        public bool IsSoon => HasEvent && (Model.NextEventDate!.Value.Date - DateTime.Today).Days <= 3;
        public string EventText
        {
            get
            {
                if (!Model.NextEventDate.HasValue) return string.Empty;
                var d = (Model.NextEventDate.Value.Date - DateTime.Today).Days;
                var title = string.IsNullOrWhiteSpace(Model.NextEventTitle) ? "Next round" : Model.NextEventTitle;
                return d < 0 ? $"{title} · {Model.NextEventDate:d MMM} (past)" : d == 0 ? $"{title} · today" : d == 1 ? $"{title} · tomorrow" : $"{title} · in {d} days";
            }
        }

        public string PrepText
        {
            get
            {
                var tagged = AppStore.Instance.SheetProblems.Where(p => p.Def.Companies.Contains(Model.Company.Trim(), StringComparer.OrdinalIgnoreCase)).ToList();
                return tagged.Count == 0 ? "No sheet problems tagged with this company yet." : $"{tagged.Count(p => p.IsSolved)} of {tagged.Count} tagged sheet problems solved";
            }
        }

        public void Refresh() => RaiseAll();
    }

    public sealed class StageColumn
    {
        public Stage Stage { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Hint { get; init; } = string.Empty;
        public ObservableCollection<ApplicationCard> Items { get; } = new();
    }

    public sealed class PipelineViewModel : PageViewModel
    {
        private ApplicationCard? _selected;
        private bool _showArchived;

        public PipelineViewModel()
        {
            Columns = new List<StageColumn>
            {
                new() { Stage = Stage.Wishlist, Name = "Wishlist", Hint = "Planning to apply" },
                new() { Stage = Stage.Applied, Name = "Applied", Hint = "Waiting to hear back" },
                new() { Stage = Stage.OA, Name = "Online test", Hint = "OA / coding round" },
                new() { Stage = Stage.Interview, Name = "Interviews", Hint = "Technical rounds" },
                new() { Stage = Stage.HR, Name = "HR / final", Hint = "Managerial & HR" },
                new() { Stage = Stage.Offer, Name = "Offer", Hint = "Congratulations" },
            };

            AddCommand = new RelayCommand(Add);
            SelectCommand = new RelayCommand<ApplicationCard>(c => Selected = c);
            CloseCommand = new RelayCommand(() => { Selected = null; OnShown(); });
            NextCommand = new RelayCommand<ApplicationCard>(c => Move(c, +1));
            PrevCommand = new RelayCommand<ApplicationCard>(c => Move(c, -1));
            ArchiveCommand = new RelayCommand(() => { if (Selected == null) return; Selected.Stage = Stage.Rejected; Selected = null; OnShown(); Shell.ShowToast("Archived. Every rejection is interview practice."); });
            DeleteCommand = new RelayCommand(Delete);
            PracticeCommand = new RelayCommand(() => { if (Selected != null) Shell.Navigate<ProblemsViewModel>("problems").ShowCompany(Selected.Company); });
            MockCommand = new RelayCommand(() =>
            {
                if (Selected == null) return;
                var mock = Shell.Navigate<MockViewModel>("mock");
                mock.Company = Selected.Company;
                mock.Source = "Company";
            });
            OpenUrlCommand = new RelayCommand(() => Links.Open(Selected?.Url));
            ToggleArchivedCommand = new RelayCommand(() => ShowArchived = !ShowArchived);
            RestoreCommand = new RelayCommand<ApplicationCard>(c => { c.Stage = Stage.Applied; OnShown(); });
        }

        public List<StageColumn> Columns { get; }
        public ObservableCollection<ApplicationCard> Archived { get; } = new();
        public IReadOnlyList<Stage> StageChoices { get; } = Enum.GetValues<Stage>();

        public ICommand AddCommand { get; }
        public ICommand SelectCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand NextCommand { get; }
        public ICommand PrevCommand { get; }
        public ICommand ArchiveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PracticeCommand { get; }
        public ICommand MockCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand ToggleArchivedCommand { get; }
        public ICommand RestoreCommand { get; }

        public ApplicationCard? Selected { get => _selected; set { Set(ref _selected, value); OnPropertyChanged(nameof(HasSelection)); } }
        public bool HasSelection => Selected != null;
        public bool ShowArchived { get => _showArchived; set => Set(ref _showArchived, value); }

        public int ActiveCount { get; private set; }
        public int UpcomingCount { get; private set; }
        public int OfferCount { get; private set; }
        public bool IsEmpty { get; private set; }

        public override void OnShown()
        {
            var apps = Store.State.Applications;
            foreach (var col in Columns)
            {
                col.Items.Clear();
                foreach (var a in apps.Where(a => a.Stage == col.Stage).OrderBy(a => a.NextEventDate ?? DateTime.MaxValue))
                    col.Items.Add(Selected?.Model == a ? Selected : new ApplicationCard(a));
            }
            Archived.Clear();
            foreach (var a in apps.Where(a => a.Stage == Stage.Rejected)) Archived.Add(new ApplicationCard(a));

            ActiveCount = apps.Count(a => a.Stage is not (Stage.Rejected or Stage.Offer or Stage.Wishlist));
            UpcomingCount = PlanService.UpcomingEvents(Store.State, 7).Count();
            OfferCount = apps.Count(a => a.Stage == Stage.Offer);
            IsEmpty = apps.Count == 0;
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(UpcomingCount));
            OnPropertyChanged(nameof(OfferCount));
            OnPropertyChanged(nameof(IsEmpty));
        }

        private void Add()
        {
            var app = new JobApplication { Company = "New company", Stage = Stage.Applied };
            Store.State.Applications.Add(app);
            Store.NotifyChanged();
            var card = new ApplicationCard(app);
            Selected = card;
            OnShown();
        }

        private void Move(ApplicationCard card, int delta)
        {
            var next = (int)card.Stage + delta;
            if (next < (int)Stage.Wishlist || next > (int)Stage.Offer) return;
            card.Stage = (Stage)next;
            if (card.Stage == Stage.Offer) Shell.ShowToast($"Offer from {card.Company}! Well earned.");
            Store.NotifyChanged();
            OnShown();
        }

        private void Delete()
        {
            if (Selected == null) return;
            if (MessageBox.Show($"Delete {Selected.Company}? This can't be undone.", "Momentum", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            Store.State.Applications.Remove(Selected.Model);
            Selected = null;
            Store.NotifyChanged();
            OnShown();
        }
    }
}
