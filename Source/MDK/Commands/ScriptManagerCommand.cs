using System;
using Malware.MDKServices;
using MDK.Resources;
using MDK.Views.BlueprintManager;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class BlueprintManagerCommand : ProjectDependentCommand
    {
        public BlueprintManagerCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.BlueprintManager;

        protected override void OnExecute()
        {
            if (!TryGetValidProject(out MDKProjectProperties projectInfo))
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, Text.BlueprintManagerCommand_OnExecute_NoMDKProjectsDescription, Text.BlueprintManagerCommand_OnExecute_NoMDKProjects, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            var model = new BlueprintManagerDialogModel
            {
                BlueprintPath = Environment.ExpandEnvironmentVariables(projectInfo.Paths.OutputPath)
            };
            BlueprintManagerDialog.ShowDialog(model);
        }
    }
}
