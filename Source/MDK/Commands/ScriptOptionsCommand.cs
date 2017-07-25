using System;
using MDK.Views;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class ScriptOptionsCommand : Command
    {
        public ScriptOptionsCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = 0x101;

        protected override void OnExecute()
        {
            var scriptOptions = new ScriptOptionsDialogModel((MDKPackage)Package, Package.DTE);
            if (!scriptOptions.HasValidProjects)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "There are no valid MDK projects in this solution.", "No MDK Projects", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            ScriptOptionsDialog.ShowDialog(scriptOptions);
        }
    }
}
