using System;
using MDK.Build;
using MDK.Views;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class DeployScriptCommand : Command
    {
        public DeployScriptCommand(MDKPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = 0x0100;

        protected override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            var dte = package.DTE;
            var scriptOptions = new ScriptOptionsDialogModel(package, dte);
            if (!scriptOptions.HasValidProjects)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "There are no valid MDK projects in this solution.", "No MDK Projects", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            using (var statusBar = new StatusBarProgressBar(ServiceProvider, "Deploying Ingame Scripts...", 100))
            using (new StatusBarAnimation(ServiceProvider, Animation.Deploy))
            {
                var buildModule = new BuildModule(package, dte.Solution.FileName, statusBar);
                await buildModule.Run();
            }

            VsShellUtilities.ShowMessageBox(ServiceProvider, "Deployment Complete", "Your script(s) should now be available in the ingame local workshop.", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
