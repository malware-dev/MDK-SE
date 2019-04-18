using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Shell;

namespace MDK.Views.ProjectHealth.Fixes
{
    /// <summary>
    /// Describes the status of the current fixing process.
    /// </summary>
    public class FixStatus : Model
    {
        string _description;
        bool _failed;

        /// <summary>
        /// Description of the current status
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (value == _description)
                    return;
                _description = value;
                PostPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this fixing process failed
        /// </summary>
        public bool Failed
        {
            get => _failed;
            set
            {
                if (value == _failed)
                    return;
                _failed = value;
                PostPropertyChanged();
            }
        }

        /// <summary>
        /// Sends a property change event in a threadsafe manner.
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected async void PostPropertyChanged([CallerMemberName] string propertyName = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            OnPropertyChanged(propertyName);
        }
    }
}