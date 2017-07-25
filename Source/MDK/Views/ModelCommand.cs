using System;
using System.Windows.Input;

namespace MDK.Views
{
    /// <summary>
    /// A generic view model command. Connects buttons and other UI command invokers to methods in the view model.
    /// </summary>
    public class ModelCommand : ICommand
    {
        readonly Action _action;
        bool _isEnabled;

        /// <summary>
        /// Creates a new instance of <see cref="ModelCommand"/>
        /// </summary>
        /// <param name="action">The action to call when this command is invoked.</param>
        /// <param name="isEnabled">The <see cref="IsEnabled"/> state of this command. Defaults to <c>true</c>.</param>
        public ModelCommand(Action action, bool isEnabled = true)
        {
            _action = action;
            IsEnabled = isEnabled;
        }

        event EventHandler CanExecuteChanged;

        event EventHandler ICommand.CanExecuteChanged
        {
            add => CanExecuteChanged += value;
            remove => CanExecuteChanged -= value;
        }

        /// <summary>
        /// Determines whether this command is currently available. The associated action will only be invoked if this value is <c>true</c>.
        /// </summary>
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
            return _isEnabled;
        }

        void ICommand.Execute(object parameter)
        {
            if (!_isEnabled || _action == null)
                return;
            _action();
        }
    }
}
