using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Malware.DevSetup
{
    /// <summary>
    /// A base class for all models
    /// </summary>
    public abstract class Model : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called whenever a trackable property changes.
        /// </summary>
        /// <param name="propertyName">The name of the property which have changed, or <c>null</c> to indicate a global change.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}