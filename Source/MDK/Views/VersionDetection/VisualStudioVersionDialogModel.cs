using System;
using MDK.Views.Options;

namespace MDK.Views.VersionDetection
{
    /// <summary>
    ///     The view model for <see cref="Options.ScriptOptionsDialog" />
    /// </summary>
    public class VisualStudioVersionDialogModel : DialogViewModel
    {
        bool _suppressMessage;

        /// <summary>
        ///     Creates a new instance of <see cref="ScriptOptionsDialogModel" />
        /// </summary>
        /// <param name="version">A valid version available for download, or <c>null</c> if no new version is available.</param>
        public VisualStudioVersionDialogModel(Version version) => Version = version;

        /// <summary>
        ///     Gets the required Visual Studio version
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Determines whether this dialog should be suppressed in the future.
        /// </summary>
        public bool SuppressMessage
        {
            get => _suppressMessage;
            set
            {
                if (value == _suppressMessage)
                    return;
                _suppressMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave() => true;
    }
}
