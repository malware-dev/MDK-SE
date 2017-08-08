using System;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using MDK.Build;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Command = MDK.VisualStudio.Command;
using Task = System.Threading.Tasks.Task;

namespace MDK.Commands
{
    abstract class DeployCommand : Command
    {
        protected DeployCommand(MDKPackage package, int id) : base(package)
        {
            Id = id;
        }

        public sealed override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public sealed override int Id { get; }

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected sealed override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            var dte = package.DTE;
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

            await OnExecute(package, dte);
        }

        protected abstract Task OnExecute(MDKPackage package, DTE dte);

        public async Task Deploy(string projectFileName = null)
        {
            var package = (MDKPackage)Package;
            var dte = package.DTE;

            string title;
            if (projectFileName != null)
                title = $"Deploying MDK Script {Path.GetFileName(projectFileName)}...";
            else
                title = "Deploying All MDK Scripts...";
            int deploymentCount;
            using (var statusBar = new StatusBarProgressBar(ServiceProvider, title, 100))
            using (new StatusBarAnimation(ServiceProvider, Animation.Deploy))
            {
                var buildModule = new BuildModule(package, dte.Solution.FileName, projectFileName, statusBar);
                deploymentCount = await buildModule.Run();
            }

            if (deploymentCount > 0)
                VsShellUtilities.ShowMessageBox(ServiceProvider, "Your script(s) should now be available in the ingame local workshop.", "Deployment Complete", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            else
                VsShellUtilities.ShowMessageBox(ServiceProvider, "There were no deployable scripts in this solution.", "Deployment Cancelled", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}