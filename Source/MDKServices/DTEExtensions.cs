using System;
using System.Diagnostics;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

// ReSharper disable SuspiciousTypeConversion.Global

namespace Malware.MDKServices
{
    /// <summary>
    /// Extension helper method for the Visual Studio DTE
    /// </summary>
    public static class DTEExtensions
    {
        /// <summary>
        /// Determines whether a project is currently loaded.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [DebuggerNonUserCode]
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

        //public static Guid GetProjectId(this EnvDTE.DTE dte, EnvDTE.Project project)
        //{
        //    var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
        //    var solutionService = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
        //    var hr = solutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy);
        //    ErrorHandler.ThrowOnFailure(hr);
        //    hr = solutionService.GetGuidOfProject(projectHierarchy, out Guid projectGuid);
        //    ErrorHandler.ThrowOnFailure(hr);
        //    return projectGuid;
        //}


        /// <summary>
        /// Unload a project
        /// </summary>
        /// <param name="project"></param>
        public static UnloadedProjectHandle Unload(this EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)project.DTE);
            var solutionService = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            var hr = solutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy);
            ErrorHandler.ThrowOnFailure(hr);
            hr = solutionService.GetGuidOfProject(projectHierarchy, out Guid projectGuid);
            ErrorHandler.ThrowOnFailure(hr);
            var solutionService4 = (IVsSolution4)solutionService;
            solutionService4.UnloadProject(ref projectGuid, (uint)_VSProjectUnloadStatus.UNLOADSTATUS_UnloadedByUser);

            return new UnloadedProjectHandle(solutionService4, project, projectGuid);
        }

        //public static void Reload(this EnvDTE.Project project)
        //{
        //    var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)project.DTE);
        //    var solutionService = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
        //    var hr = solutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy projectHierarchy);
        //    ErrorHandler.ThrowOnFailure(hr);
        //    hr = solutionService.GetGuidOfProject(projectHierarchy, out Guid projectGuid);
        //    ErrorHandler.ThrowOnFailure(hr);
        //    var solutionService4 = (IVsSolution4)solutionService;
        //    solutionService4.ReloadProject(ref projectGuid);
        //}

        /// <summary>
        /// A handle for controlling a previously unloaded project
        /// </summary>
        public class UnloadedProjectHandle
        {
            IVsSolution4 _solutionService4;
            Guid _projectGuid;

            internal UnloadedProjectHandle(IVsSolution4 solutionService4, Project project, Guid projectGuid)
            {
                _solutionService4 = solutionService4;
                Project = project;
                _projectGuid = projectGuid;
            }

            /// <summary>
            /// The unloaded project
            /// </summary>
            public Project Project { get; }

            /// <summary>
            /// Reload the project
            /// </summary>
            public void Reload()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                _solutionService4.ReloadProject(ref _projectGuid);
            }
        }
    }
}