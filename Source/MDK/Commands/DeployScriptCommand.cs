using System;
using System.Threading.Tasks;
using EnvDTE;
using MDK.Build;
using MDK.Views;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Command = MDK.VisualStudio.Command;

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
            if (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "Visual studio is busy, please try again later...", "Busy...", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var scriptOptions = new ScriptOptionsDialogModel(package, dte);
            if (!scriptOptions.HasValidProjects)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "There are no valid MDK projects in this solution.", "No MDK Projects", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            var tcs = new TaskCompletionSource<int>();

            void BuildEventsOnOnBuildDone(vsBuildScope scope, vsBuildAction action) => tcs.SetResult(dte.Solution.SolutionBuild.LastBuildInfo);

            dte.Events.BuildEvents.OnBuildDone += BuildEventsOnOnBuildDone;
            dte.Solution.SolutionBuild.Build();
            var failedProjects = await tcs.Task;
            dte.Events.BuildEvents.OnBuildDone -= BuildEventsOnOnBuildDone;

            if (failedProjects > 0)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "Build failed.", "Deploy Rejected", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
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
