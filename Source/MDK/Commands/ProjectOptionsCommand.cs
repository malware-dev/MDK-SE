using System;
using System.Collections;
using System.Linq;
using EnvDTE;
using MDK.Views;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Command = MDK.VisualStudio.Command;

namespace MDK.Commands
{
    sealed class ProjectOptionsCommand : Command
    {
        public ProjectOptionsCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.ProjectOptions;

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected override void OnExecute()
        {
            var dte2 = (EnvDTE80.DTE2)Package.DTE;
            var selectedProject =  ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (selectedProject == null)
                return;
            var scriptOptions = new ScriptOptionsDialogModel((MDKPackage)Package, Package.DTE, selectedProject);
            if (!scriptOptions.ActiveProject.IsValid)
            {
                VsShellUtilities.ShowMessageBox(ServiceProvider, "There are no valid MDK projects in this solution.", "No MDK Projects", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            ScriptOptionsDialog.ShowDialog(scriptOptions);
        }
    }
}