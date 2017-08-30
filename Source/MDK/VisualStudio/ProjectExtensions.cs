using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
// ReSharper disable SuspiciousTypeConversion.Global

namespace MDK.VisualStudio
{
    /// <summary>
    /// Adds extra utility method to the <see cref="Project"/> type
    /// </summary>
    public static class ProjectExtensions
    {
        /// <summary>
        /// Unload a project
        /// </summary>
        /// <param name="project"></param>
        public static void Unload(this Project project)
        {
            var dte = project.DTE;
            var solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            var projectName = project.Name;

            dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
            ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + projectName).Select(vsUISelectionType.vsUISelectionTypeSelect);

            dte.ExecuteCommand("Project.UnloadProject");
        }

        /// <summary>
        /// Reloads a previously unloaded project
        /// </summary>
        /// <param name="project"></param>
        public static void Reload(this Project project)
        {
            var dte = project.DTE;
            var solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            var projectName = project.Name;

            dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
            ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + projectName).Select(vsUISelectionType.vsUISelectionTypeSelect);

            dte.ExecuteCommand("Project.ReloadProject");
        }
    }
}
