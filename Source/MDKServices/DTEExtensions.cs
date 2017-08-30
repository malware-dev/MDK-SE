using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
// ReSharper disable SuspiciousTypeConversion.Global

namespace Malware.MDKServices
{
    /// <summary>
    /// Extension helper method for the Visual Studio DTE
    /// </summary>
    public static class DTEExtensions
    {
        /// <summary>
        /// A handle for controlling a previously unloaded project
        /// </summary>
        public class UnloadedProjectHandle
        {
            readonly string _path;
            /// <summary>
            
                /// The unloaded project
            /// </summary>
            public Project Project { get; }

            internal UnloadedProjectHandle(Project project, string path)
            {
                _path = path;
                Project = project;
            }

            /// <summary>
            /// Reload the project
            /// </summary>
            public void Reload()
            {
                var dte = Project.DTE;
                dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(_path).Select(vsUISelectionType.vsUISelectionTypeSelect);

                dte.ExecuteCommand("Project.ReloadProject");
            }
        }

        /// <summary>
        /// Determines whether a project is currently loaded.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static bool IsLoaded(this Project project)
        {
            // This is downright dirty, but it's the only way to determine if a project is loaded or not.
            try
            {
                return !string.IsNullOrEmpty(project.FullName);
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Unload a project
        /// </summary>
        /// <param name="project"></param>
        public static UnloadedProjectHandle Unload(this Project project)
        {
            var dte = project.DTE;
            var solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName);
            var path = $@"{solutionName}\{project.Name}";

            dte.Windows.Item(Constants.vsWindowKindSolutionExplorer).Activate();
            ((DTE2)dte).ToolWindows.SolutionExplorer.GetItem(path).Select(vsUISelectionType.vsUISelectionTypeSelect);

            dte.ExecuteCommand("Project.UnloadProject");
            return new UnloadedProjectHandle(project, path);
        }
    }
}
