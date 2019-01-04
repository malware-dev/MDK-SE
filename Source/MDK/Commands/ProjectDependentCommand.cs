using System.Collections;
using System.Linq;
using EnvDTE;
using Malware.MDKServices;
using MDK.VisualStudio;
using Command = MDK.VisualStudio.Command;

namespace MDK.Commands
{
    abstract class ProjectDependentCommand : Command
    {
        protected ProjectDependentCommand(ExtendedPackage package) : base(package)
        { }

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled && TryGetValidProject(out _);
        }

        protected bool TryGetValidProject(out Project project, out MDKProjectProperties projectProperties)
        {
            var dte2 = (EnvDTE80.DTE2)Package.DTE;
            project = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (project == null)
            {
                projectProperties = null;
                return false;
            }
            projectProperties = MDKProjectProperties.Load(project.FullName, project.Name);
            return projectProperties.IsValid;
        }

        protected bool TryGetValidProject(out MDKProjectProperties projectProperties)
            => TryGetValidProject(out _, out projectProperties);
    }
}