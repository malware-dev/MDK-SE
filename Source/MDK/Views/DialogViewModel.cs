using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JetBrains.Annotations;

namespace MDK.Views
{
    /// <summary>
    /// A base class for dialog view models
    /// </summary>
    public abstract class DialogViewModel : Model, INotifyDataErrorInfo
    {
        Dictionary<string, HashSet<string>> _propertyErrors = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        bool _isValid;

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
        ///     Occurs when the validation errors have changed for a property or for the entire entity.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// A command for performing the dialog's operation and then closing.
        /// </summary>
        public ModelCommand SaveAndCloseCommand { get; }

        /// <summary>
        /// A command for canceling the dialog's operation and then closing.
        /// </summary>
        public ModelCommand CancelCommand { get; }

        /// <summary>
        /// Determines whether the current state of this model is considered "valid" for the dialog, meaning
        /// that it can be saved and closed.
        /// </summary>
        public bool IsValid
        {
            get => _isValid;
            set
            {
                if (value == _isValid)
                    return;
                _isValid = value;
                OnIsValidChanged();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Called when the <see cref="IsValid"/> property changes.
        /// </summary>
        protected virtual void OnIsValidChanged()
        {
            SaveAndCloseCommand.IsEnabled = IsValid;
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
        protected virtual bool OnCancel() => true;

        /// <summary>
        /// Called when the model is closing. Any view using this model should close as well.
        /// </summary>
        protected virtual void OnClosing(bool? state)
        {
            Closing?.Invoke(this, new DialogClosingEventArgs(state));
        }

        /// <summary>
        /// Clears the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">
        ///     The name of the property to clear validation errors for; or null or System.String.Empty,
        ///     to clear entity-level errors.
        /// </param>
        public void ClearErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                foreach (var errors in _propertyErrors.Values)
                    errors.Clear();
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
                IsValid = true;
            }
            else
            {
                if (_propertyErrors.TryGetValue(propertyName, out var errors))
                    errors.Clear();
                IsValid = !_propertyErrors.Values.Any(e => e.Count > 0);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Adds an error to a given property.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <param name="error"></param>
        public void AddError([NotNull] string propertyName, [NotNull] string error)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));
            if (error == null)
                throw new ArgumentNullException(nameof(error));
            if (!_propertyErrors.TryGetValue(propertyName, out var errors))
                _propertyErrors[propertyName] = errors = new HashSet<string>();
            errors.Add(error);
            IsValid = false;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        ///     Gets the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">
        ///     The name of the property to retrieve validation errors for; or null or System.String.Empty,
        ///     to retrieve entity-level errors.
        /// </param>
        /// <returns>The validation errors for the property or entity.</returns>
        public IEnumerable<string> GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                foreach (var errors in _propertyErrors.Values)
                    foreach (var error in errors)
                        yield return error;
            }
            else
            {
                if (_propertyErrors.TryGetValue(propertyName, out var errors))
                {
                    foreach (var error in errors)
                        yield return error;
                }
            }
        }

        bool INotifyDataErrorInfo.HasErrors => !IsValid;

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName) => GetErrors(propertyName);
    }
}
