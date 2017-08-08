using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;

namespace MDK.Commands
{
    sealed class DeployProjectCommand : DeployCommand
    {
        public DeployProjectCommand(MDKPackage package) : base(package, CommandIds.DeployProject)
        { }

        protected override async Task OnExecute(MDKPackage package, DTE dte)
        {
            var dte2 = (EnvDTE80.DTE2)dte;
            var selectedProject = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>()
                .FirstOrDefault();
            if (selectedProject == null)
                return;

            await Deploy(selectedProject.FullName);
        }
    }
}