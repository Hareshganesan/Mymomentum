using System.Windows.Input;
using Momentum.Core;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class LoginViewModel : ObservableObject
    {
        private bool _isCreating;
        private string _username = string.Empty;
        private string _displayName = string.Empty;
        private string _leetCode = string.Empty;
        private bool _remember = true;
        private string _error = string.Empty;

        public LoginViewModel()
        {
            var file = AccountService.Load();
            Accounts = file.Accounts.OrderByDescending(a => a.LastLogin).Select(a => a.Username).ToList();
            _isCreating = Accounts.Count == 0;
            _username = Accounts.FirstOrDefault() ?? string.Empty;
            SubmitCommand = new RelayCommand(Submit);
            ToggleModeCommand = new RelayCommand(() => { IsCreating = !IsCreating; Error = string.Empty; });
            PickCommand = new RelayCommand<string>(u => { Username = u; IsCreating = false; Error = string.Empty; });
        }

        /// <summary>Raised after a successful sign-in or account creation; the active account's folder is already selected.</summary>
        public event Action? SignedIn;

        public List<string> Accounts { get; }
        public bool HasAccounts => Accounts.Count > 0;
        public ICommand SubmitCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand PickCommand { get; }

        // PasswordBox can't bind; the view pushes these in.
        public string Password { get; set; } = string.Empty;
        public string Confirm { get; set; } = string.Empty;

        public bool IsCreating
        {
            get => _isCreating;
            set { Set(ref _isCreating, value); OnPropertyChanged(nameof(Title)); OnPropertyChanged(nameof(Subtitle)); OnPropertyChanged(nameof(SubmitText)); OnPropertyChanged(nameof(ToggleText)); }
        }

        public string Title => IsCreating ? "Create your account" : "Welcome back";
        public string Subtitle => IsCreating
            ? "Your progress is saved to this account on this PC. Friends can make their own."
            : "Sign in to pick up where you left off.";
        public string SubmitText => IsCreating ? "Create account" : "Sign in";
        public string ToggleText => IsCreating ? "Already have an account? Sign in" : "New here? Create an account";

        public string Username { get => _username; set => Set(ref _username, value); }
        public string DisplayName { get => _displayName; set => Set(ref _displayName, value); }
        public string LeetCode { get => _leetCode; set => Set(ref _leetCode, value); }
        public bool Remember { get => _remember; set => Set(ref _remember, value); }
        public string Error { get => _error; private set => Set(ref _error, value); }

        private void Submit()
        {
            string? error;
            if (IsCreating)
            {
                if (Password != Confirm) { Error = "Passwords don't match."; return; }
                error = AccountService.Create(Username, Password, string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName, LeetCode, Remember);
            }
            else
            {
                error = AccountService.SignIn(Username, Password, Remember);
            }

            if (error != null) { Error = error; return; }
            Error = string.Empty;
            SignedIn?.Invoke();
        }
    }
}
