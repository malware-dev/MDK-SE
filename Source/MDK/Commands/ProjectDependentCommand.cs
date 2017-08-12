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

        protected bool TryGetValidProject(out Project project, out ProjectScriptInfo projectInfo)
        {
            var dte2 = (EnvDTE80.DTE2)Package.DTE;
            project = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (project == null)
            {
                projectInfo = null;
                return false;
            }
            projectInfo = ProjectScriptInfo.Load(project.FullName, project.Name);
            return projectInfo.IsValid;
        }

        protected bool TryGetValidProject(out ProjectScriptInfo projectInfo)
            => TryGetValidProject(out _, out projectInfo);
    }
}