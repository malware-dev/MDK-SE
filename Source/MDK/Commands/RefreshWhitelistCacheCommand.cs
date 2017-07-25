using System;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class RefreshWhitelistCacheCommand : Command
    {
        public RefreshWhitelistCacheCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = 0x0102;

        protected override void OnExecute()
        {
            VsShellUtilities.ShowMessageBox(
                ServiceProvider,
                "This operation has not yet been completed in this version. Please check to see if there has been an update to the package yet.",
                "Incomplete operation",
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
