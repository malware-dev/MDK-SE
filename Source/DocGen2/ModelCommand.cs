using System;
using System.Windows.Input;

namespace Mal.DocGen2
{
    public class ModelCommand : ICommand
    {
        readonly Action _action;
        bool _isEnabled = true;

        public ModelCommand(Action action, bool isEnabled = true)
        {
            _action = action;
            IsEnabled = isEnabled;
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return IsEnabled;
        }

        void ICommand.Execute(object parameter)
        {
            Execute();
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add => CanExecuteChanged += value;
            remove => CanExecuteChanged -= value;
        }

        public void Execute()
        {
            if (IsEnabled)
                _action?.Invoke();
        }

        event EventHandler CanExecuteChanged;
    }
}