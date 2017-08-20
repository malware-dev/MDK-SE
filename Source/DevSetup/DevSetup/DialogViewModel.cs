using System;
using System.Windows.Input;

namespace Malware.DevSetup
{
    /// <summary>
    /// A base class for dialog view models
    /// </summary>
    public abstract class DialogViewModel : Model
    {
        /// <summary>
        /// Creates a new instance of <see cref="DialogViewModel"/>
        /// </summary>
        protected DialogViewModel()
        {
            SaveAndCloseCommand = new ModelCommand(SaveAndClose);
            CancelCommand = new ModelCommand(Cancel);
        }

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
