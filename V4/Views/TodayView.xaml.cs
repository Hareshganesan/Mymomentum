using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class TodayView : UserControl
    {
        public TodayView()
        {
            InitializeComponent();
            SizeChanged += (_, _) => UpdateLayoutForWidth();
        }

        /// <summary>On narrow windows the supporting panel moves below the plan instead of squeezing it.</summary>
        private void UpdateLayoutForWidth()
        {
            bool narrow = ActualWidth < 1000;
            Grid.SetColumn(RightPanel, narrow ? 0 : 2);
            Grid.SetRow(RightPanel, narrow ? 1 : 0);
            Grid.SetColumnSpan(RightPanel, narrow ? 3 : 1);
            RightPanel.Margin = narrow ? new Thickness(0, 16, 0, 0) : new Thickness(0);
            GapCol.Width = new GridLength(narrow ? 0 : 16);
            SideCol.Width = new GridLength(narrow ? 0 : 320);
        }

        private void TaskBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is TodayViewModel vm && vm.AddTaskCommand.CanExecute(null))
                vm.AddTaskCommand.Execute(null);
        }
    }
}
