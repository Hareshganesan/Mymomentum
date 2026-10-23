using System.Windows;
using System.Windows.Controls;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
            // PasswordBox can't bind, so sync it by hand.
            DataContextChanged += (_, _) => { if (DataContext is SettingsViewModel vm) SessionBox.Password = vm.LeetCodeSession; };
        }

        private void SessionBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && vm.LeetCodeSession != SessionBox.Password) vm.LeetCodeSession = SessionBox.Password;
        }
    }
}
