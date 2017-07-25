using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using JetBrains.Annotations;

namespace MDK.Views
{
    /// <summary>
    /// A base class for dialog view models
    /// </summary>
    public abstract class DialogViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Creates a new instance of <see cref="DialogViewModel"/>
        /// </summary>
        protected DialogViewModel()
        {
            SaveAndCloseCommand = new ModelCommand(SaveAndClose);
            CancelCommand = new ModelCommand(Cancel);
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fired when this model is closing. Any views using this model should close as well.
        /// </summary>
        public event EventHandler<DialogClosingEventArgs> Closing;

        /// <summary>
        /// A command for performing the dialog's operation and then closing.
        /// </summary>
        public ICommand SaveAndCloseCommand { get; }

        /// <summary>
        /// A command for canceling the dialog's operation and then closing.
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// Called whenever a trackable property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property which have changed, or <c>null</c> to indicate a global change.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Upgrades the projects and closes.
        /// </summary>
        public void SaveAndClose()
        {
            if (OnSave())
                OnClosing(true);
        }

        /// <summary>
        /// Override this method to perform the required operation.
        /// </summary>
        /// <returns></returns>
        protected abstract bool OnSave();

        /// <summary>
        /// Cancels the operation and closes.
        /// </summary>
        protected void Cancel()
        {
            if (OnCancel())
                OnClosing(false);
        }

        /// <summary>
        /// Override this method if you need to be able to perform cleanups or stop a cancel.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnCancel()
        {
            return true;
        }

        /// <summary>
        /// Called when the model is closing. Any view using this model should close as well.
        /// </summary>
        protected virtual void OnClosing(bool? state)
        {
            Closing?.Invoke(this, new DialogClosingEventArgs(state));
        }
    }
}
