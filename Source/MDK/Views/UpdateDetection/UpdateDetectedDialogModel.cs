using System;
using System.Threading.Tasks;
using MDK.Views.Options;

namespace MDK.Views.UpdateDetection
{
    /// <summary>
    /// The view model for <see cref="Options.ScriptOptionsDialog"/>
    /// </summary>
    public class UpdateDetectedDialogModel : DialogViewModel
    {
        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="version">A valid version available for download, or <c>null</c> if no new version is available.</param>
        public UpdateDetectedDialogModel(Version version)
        {
            Version = version;
        }

        /// <summary>
        /// Determines whether there's a new version to download.
        /// </summary>
        public bool HasNewVersion => Version != null;

        /// <summary>
        /// Gets the currently detected extension version
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave()
        {
            System.Diagnostics.Process.Start(MDKPackage.ReleasePageUrl);
            return true;
        }
    }
}
