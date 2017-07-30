using System;
using EnvDTE;
using JetBrains.Annotations;
using MDK.Services;

namespace MDK.Views
{
    /// <summary>
    /// The view model for <see cref="ScriptOptionsDialog"/>
    /// </summary>
    public class RefreshWhitelistCacheDialogModel : DialogViewModel
    {
        MDKPackage _package;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="dte"></param>
        public RefreshWhitelistCacheDialogModel([NotNull] MDKPackage package, [NotNull] DTE dte)
        {
            if (dte == null)
                throw new ArgumentNullException(nameof(dte));
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave()
        {
            var cache = new WhitelistCache();
            cache.Refresh(_package.InstallPath.FullName);
            return true;
        }
    }
}
