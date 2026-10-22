using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MyTodo.Models
{
    /// <summary>
    /// Represents an HR / Behavioral question answered using the STAR framework.
    /// (Situation, Task, Action, Result)
    /// </summary>
    public class BehavioralQuestion : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _questionTitle = string.Empty;
        private string _category = "Leadership";
        private string _situation = string.Empty;
        private string _task = string.Empty;
        private string _action = string.Empty;
        private string _result = string.Empty;
        private string _keyTakeaway = string.Empty;
        private bool _isExpanded;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        public string QuestionTitle
        {
            get => _questionTitle;
            set { _questionTitle = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public string Situation
        {
            get => _situation;
            set { _situation = value; OnPropertyChanged(); }
        }

        public string Task
        {
            get => _task;
            set { _task = value; OnPropertyChanged(); }
        }

        public string Action
        {
            get => _action;
            set { _action = value; OnPropertyChanged(); }
        }

        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        public string KeyTakeaway
        {
            get => _keyTakeaway;
            set { _keyTakeaway = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
