using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class ReviewView : UserControl
    {
        public static readonly IValueConverter NonZero = new ViewHelpers.Lambda(v => v is int n && n > 0);

        public ReviewView()
        {
            InitializeComponent();
            Loaded += (_, _) => Focus();
            PreviewKeyDown += OnKey;
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            // Don't steal keys while the user is typing notes.
            if (Keyboard.FocusedElement is TextBox || DataContext is not ReviewViewModel vm || !vm.IsActive) return;
            ICommand? command = e.Key switch
            {
                Key.Space or Key.Enter => vm.IsRevealed ? null : vm.RevealCommand,
                Key.D1 or Key.NumPad1 => vm.AgainCommand,
                Key.D2 or Key.NumPad2 => vm.HardCommand,
                Key.D3 or Key.NumPad3 => vm.GoodCommand,
                Key.D4 or Key.NumPad4 => vm.EasyCommand,
                Key.S => vm.SkipCommand,
                _ => null,
            };
            if (command != null && command.CanExecute(null))
            {
                command.Execute(null);
                e.Handled = true;
                Focus();
            }
        }
    }
}
