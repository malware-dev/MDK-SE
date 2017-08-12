using System;
using System.Collections;
using System.Linq;
using EnvDTE;
using MDK.VisualStudio;
using Command = MDK.VisualStudio.Command;

namespace MDK.Commands
{
    sealed class DeployProjectCommand : Command
    {
        public DeployProjectCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.DeployProject;

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            var dte2 = (EnvDTE80.DTE2)package.DTE;
            var selectedProject = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (selectedProject == null)
                return;


            await package.Deploy(selectedProject.FullName);
        }
    }
}
