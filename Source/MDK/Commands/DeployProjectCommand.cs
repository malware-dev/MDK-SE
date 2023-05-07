using System;
using EnvDTE;
using MDK.Resources;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class DeployProjectCommand : ProjectDependentCommand
    {
        public DeployProjectCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.DeployProject;

        protected override async void OnExecute()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!TryGetValidProject(out Project project, out _))
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, Text.ProjectOptionsCommand_OnExecute_NoMDKProjectsDescription, Text.ProjectOptionsCommand_OnExecute_NoMDKProjects, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            var package = (MDKPackage)Package;
            await package.DeployAsync(project);
        }
    }
}
