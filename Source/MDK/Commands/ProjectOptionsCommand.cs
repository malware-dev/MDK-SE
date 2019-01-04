using System;
using System.Collections;
using System.Linq;
using EnvDTE;
using Malware.MDKServices;
using MDK.Resources;
using MDK.Views.Options;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Command = MDK.VisualStudio.Command;

namespace MDK.Commands
{
    sealed class ProjectOptionsCommand : ProjectDependentCommand
    {
        public ProjectOptionsCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.ProjectOptions;

        protected override void OnExecute()
        {
            if (!TryGetValidProject(out MDKProjectProperties projectInfo))
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, Text.ProjectOptionsCommand_OnExecute_NoMDKProjectsDescription, Text.ProjectOptionsCommand_OnExecute_NoMDKProjects, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            var scriptOptions = new ScriptOptionsDialogModel((MDKPackage)Package, projectInfo);
            ScriptOptionsDialog.ShowDialog(scriptOptions);
        }
    }
}
