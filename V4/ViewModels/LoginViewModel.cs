using System.Windows.Input;
using Momentum.Core;
using Momentum.Services;

namespace Momentum.ViewModels
{
    public sealed class LoginViewModel : ObservableObject
    {
        private bool _isCreating;
        private bool _isResetting;
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
            ToggleModeCommand = new RelayCommand(() => { if (IsResetting) IsResetting = false; else IsCreating = !IsCreating; Error = string.Empty; });
            ForgotCommand = new RelayCommand(() => { IsResetting = true; Error = string.Empty; });
            PickCommand = new RelayCommand<string>(u => { Username = u; IsCreating = false; Error = string.Empty; });
        }

        /// <summary>Raised after a successful sign-in or account creation; the active account's folder is already selected.</summary>
        public event Action? SignedIn;

        public List<string> Accounts { get; }
        public bool HasAccounts => Accounts.Count > 0;
        public ICommand SubmitCommand { get; }
        public ICommand ToggleModeCommand { get; }
        public ICommand ForgotCommand { get; }
        public ICommand PickCommand { get; }

        // PasswordBox can't bind; the view pushes these in.
        public string Password { get; set; } = string.Empty;
        public string Confirm { get; set; } = string.Empty;
        public string RecoveryCode { get; set; } = string.Empty;

        public bool IsCreating
        {
            get => _isCreating;
            set { Set(ref _isCreating, value); RaiseMode(); }
        }

        public bool IsResetting
        {
            get => _isResetting;
            set { Set(ref _isResetting, value); RaiseMode(); }
        }

        public bool NeedsConfirm => IsCreating || IsResetting;
        public bool CanForget => HasAccounts && !IsCreating && !IsResetting;

        private void RaiseMode()
        {
            foreach (var n in new[] { nameof(Title), nameof(Subtitle), nameof(SubmitText), nameof(ToggleText), nameof(PasswordLabel), nameof(NeedsConfirm), nameof(CanForget) })
                OnPropertyChanged(n);
        }

        public string Title => IsResetting ? "Reset your password" : IsCreating ? "Create your account" : "Welcome back";
        public string Subtitle => IsResetting
            ? "First confirm it's you with Windows Hello or your recovery code. Your progress stays exactly as it is."
            : IsCreating
            ? "Your progress is saved to this account on this PC. Friends can make their own."
            : "Sign in to pick up where you left off.";
        public string SubmitText => IsResetting ? "Set new password" : IsCreating ? "Create account" : "Sign in";
        public string ToggleText => IsResetting ? "Back to sign in" : IsCreating ? "Already have an account? Sign in" : "New here? Create an account";
        public string PasswordLabel => IsResetting ? "New password" : "Password";

        public string Username { get => _username; set => Set(ref _username, value); }
        public string DisplayName { get => _displayName; set => Set(ref _displayName, value); }
        public string LeetCode { get => _leetCode; set => Set(ref _leetCode, value); }
        public bool Remember { get => _remember; set => Set(ref _remember, value); }
        public string Error { get => _error; private set => Set(ref _error, value); }

        private async void Submit()
        {
            string? error;
            if (IsResetting)
            {
                if (Password != Confirm) { Error = "Passwords don't match."; return; }
                if (AccountService.Find(AccountService.Load(), Username) == null) { Error = "No account with that username on this PC."; return; }
                if (Password.Length < 6) { Error = "Password needs at least 6 characters."; return; }
                if (RecoveryCode.Trim().Length > 0)
                {
                    if (!AccountService.VerifyRecoveryCode(Username, RecoveryCode)) { Error = "That recovery code isn't right."; return; }
                }
                else
                {
                    Error = string.Empty;
                    var hello = await WindowsHello.VerifyAsync($"Confirm it's you to reset the Momentum password for {Username.Trim()}.");
                    if (hello == null) { Error = "Windows Hello isn't set up on this PC. Enter your recovery code instead."; return; }
                    if (hello == false) { Error = "Windows couldn't confirm it's you, so the password wasn't changed."; return; }
                }
                error = AccountService.ResetPassword(Username, Password) ?? AccountService.SignIn(Username, Password, Remember);
            }
            else if (IsCreating)
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
