using System;
using MDK.Resources;
using MDK.Views.Whitelist;
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

        public override int Id { get; } = CommandIds.RefreshWhitelistCache;

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected override void OnExecute()
        {
            var package = (MDKPackage)Package;

            if (RefreshWhitelistCacheDialog.ShowDialog(new RefreshWhitelistCacheDialogModel(package, package.DTE)) == true)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, Text.RefreshWhitelistCacheCommand_OnExecute_UpdatedWhitelistsDescription, Text.RefreshWhitelistCacheCommand_OnExecute_UpdatedWhitelistsTitle, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }
    }
}
