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
            var package = (MDKPackage)Package;
            package.Echo("MDK Command", Id, "is attempting to retrieve an MDK project");
            var dte2 = (EnvDTE80.DTE2)Package.DTE;
            project = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (project == null)
            {
                package.Echo("MDK Command", Id, "failed because no project could be found");
                projectProperties = null;
                return false;
            }
            projectProperties = MDKProjectProperties.Load(project.FullName, project.Name);
            if (projectProperties.IsValid)
            {
                package.Echo("MDK Command", Id, "retrieved a valid project");
                return true;
            }
            package.Echo("MDK Command", Id, "did not retrieve a valid project");
            return false;
        }

        protected bool TryGetValidProject(out MDKProjectProperties projectProperties)
            => TryGetValidProject(out _, out projectProperties);
    }
}