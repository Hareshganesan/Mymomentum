using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            // The view is reused after sign-out; clear the boxes so a stale password isn't shown but never sent.
            DataContextChanged += (_, _) => { PasswordBox.Clear(); ConfirmBox.Clear(); RecoveryBox.Clear(); };
            Loaded += (_, _) => { if (string.IsNullOrEmpty(UserBox.Text)) UserBox.Focus(); else PasswordBox.Focus(); };
        }

        private LoginViewModel? Vm => DataContext as LoginViewModel;

        private void Password_Changed(object sender, RoutedEventArgs e) { if (Vm != null) Vm.Password = PasswordBox.Password; }
        private void Recovery_Changed(object sender, RoutedEventArgs e) { if (Vm != null) Vm.RecoveryCode = RecoveryBox.Password; }
        private void Confirm_Changed(object sender, RoutedEventArgs e) { if (Vm != null) Vm.Confirm = ConfirmBox.Password; }

        private void Field_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Vm != null) Vm.SubmitCommand.Execute(null);
        }
    }
}
