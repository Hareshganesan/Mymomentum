using System.Windows;
using System.Windows.Controls;
using Momentum.Models;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class PipelineView : UserControl
    {
        public PipelineView() => InitializeComponent();

        // PasswordBoxes can't bind; hand secrets straight to the view model and clear them.
        private void ConnectMail_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PipelineViewModel vm) vm.ConnectMail(MailPwd.Password);
            MailPwd.Clear();
        }

        private void GoogleSignIn_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is PipelineViewModel vm) vm.SignInWithGoogle(GoogleSecret.Password);
            GoogleSecret.Clear();
        }

        private async void MergeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MergeBox.SelectedItem is not JobApplication target || DataContext is not PipelineViewModel vm || vm.Selected == null) return;
            MergeBox.SelectedItem = null;
            var ok = await ShellViewModel.Current.ConfirmAsync("Merge applications", $"Merge \"{vm.Selected.Company}\" into \"{target.Company}\"? Its emails and notes move over.", "Merge");
            if (ok) vm.MergeIntoCommand.Execute(target);
        }
    }
}
