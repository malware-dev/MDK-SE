using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mal.DocGen2
{
    public class AsyncModelCommand : ICommand
    {
        readonly Func<Task> _asyncAction;
        bool _isEnabled = true;

        public AsyncModelCommand(Func<Task> action, bool isEnabled = true)
        {
            _asyncAction = action;
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

        async void ICommand.Execute(object parameter)
        {
            await ExecuteAsync();
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add => CanExecuteChanged += value;
            remove => CanExecuteChanged -= value;
        }

        public async Task ExecuteAsync()
        {
            if (!IsEnabled || _asyncAction == null)
                return;
            await _asyncAction?.Invoke();
        }

        event EventHandler CanExecuteChanged;
    }
}