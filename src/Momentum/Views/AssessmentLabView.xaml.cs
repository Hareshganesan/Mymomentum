using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Momentum.ViewModels;

namespace Momentum.Views
{
    public partial class AssessmentLabView : UserControl
    {
        public AssessmentLabView()
        {
            InitializeComponent();
            Loaded += AssessmentLabView_Loaded;
        }

        private void AssessmentLabView_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLineNumbers();
            UpdateCaretStatus();

            // Hook scroll synchronization between CodeEditorTextBox and LineNumbersScrollViewer
            CodeEditorTextBox.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(OnEditorScrollChanged));
        }

        private void OnEditorScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0 && LineNumbersScrollViewer != null)
            {
                LineNumbersScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
            }
        }

        private void OnCodeEditorTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLineNumbers();
            UpdateCaretStatus();
        }

        private void OnCodeEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateCaretStatus();
        }

        private void UpdateLineNumbers()
        {
            if (LineNumbersText == null || CodeEditorTextBox == null) return;

            var text = CodeEditorTextBox.Text ?? string.Empty;
            int lineCount = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') lineCount++;
            }

            var sb = new StringBuilder();
            for (int i = 1; i <= Math.Max(1, lineCount); i++)
            {
                if (i > 1) sb.Append('\n');
                sb.Append(i);
            }
            LineNumbersText.Text = sb.ToString();

            if (LineCountText != null)
            {
                LineCountText.Text = $"{lineCount} lines, {text.Length} chars";
            }
        }

        private void UpdateCaretStatus()
        {
            if (CaretPositionText == null || CodeEditorTextBox == null) return;

            var text = CodeEditorTextBox.Text ?? string.Empty;
            int caret = CodeEditorTextBox.CaretIndex;
            if (caret > text.Length) caret = text.Length;

            int line = 1;
            int lastLineStart = 0;
            for (int i = 0; i < caret; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    lastLineStart = i + 1;
                }
            }
            int col = caret - lastLineStart + 1;

            CaretPositionText.Text = $"Ln {line}, Col {col}";
        }

        private void OnCodeEditorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Keyboard shortcut: Ctrl+Shift+Enter -> Submit, Ctrl+Enter -> Run Tests
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Return)
            {
                e.Handled = true;
                if (DataContext is AssessmentLabViewModel vm)
                {
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                    {
                        if (vm.SubmitAssessmentCommand.CanExecute(null))
                            vm.SubmitAssessmentCommand.Execute(null);
                    }
                    else
                    {
                        if (vm.RunTestsCommand.CanExecute(null))
                            vm.RunTestsCommand.Execute(null);
                    }
                }
                return;
            }

            // Tab key: insert 4 spaces
            if (e.Key == Key.Tab && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
            {
                e.Handled = true;
                int caret = CodeEditorTextBox.CaretIndex;
                CodeEditorTextBox.SelectedText = "    ";
                CodeEditorTextBox.CaretIndex = caret + 4;
                return;
            }

            // Enter key: smart auto-indentation for Python
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                int caret = CodeEditorTextBox.CaretIndex;
                string text = CodeEditorTextBox.Text ?? string.Empty;

                int lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
                string currentLine = caret > lineStart ? text.Substring(lineStart, caret - lineStart) : string.Empty;

                // Detect current indentation
                int indentLen = currentLine.TakeWhile(c => c == ' ').Count();
                string indent = new string(' ', indentLen);

                // If line ends with ':' add 4 spaces
                string trimmed = currentLine.TrimEnd();
                if (trimmed.EndsWith(":"))
                {
                    indent += "    ";
                }

                string insertText = "\n" + indent;
                CodeEditorTextBox.SelectedText = insertText;
                CodeEditorTextBox.CaretIndex = caret + insertText.Length;
                return;
            }

            // Backspace key: smart 4-space un-indent
            if (e.Key == Key.Back && CodeEditorTextBox.SelectionLength == 0)
            {
                int caret = CodeEditorTextBox.CaretIndex;
                string text = CodeEditorTextBox.Text ?? string.Empty;
                if (caret >= 4)
                {
                    int lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
                    int col = caret - lineStart;
                    if (col >= 4 && col % 4 == 0)
                    {
                        string preceding = text.Substring(caret - 4, 4);
                        if (preceding == "    ")
                        {
                            e.Handled = true;
                            CodeEditorTextBox.Text = text.Remove(caret - 4, 4);
                            CodeEditorTextBox.CaretIndex = caret - 4;
                            return;
                        }
                    }
                }
            }
        }
    }
}
