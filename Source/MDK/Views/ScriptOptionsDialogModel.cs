using System;
using System.Collections.ObjectModel;
using System.Linq;
using EnvDTE;
using JetBrains.Annotations;
using MDK.Services;
using MDK.VisualStudio;

namespace MDK.Views
{
    /// <summary>
    /// The view model for <see cref="ScriptOptionsDialog"/>
    /// </summary>
    public class ScriptOptionsDialogModel : DialogViewModel
    {
        static ProjectScriptInfo LoadScriptInfo(MDKPackage package, Project project)
        {
            try
            {
                return ProjectScriptInfo.Load(project.FullName, project.Name);
            }
            catch (Exception e)
            {
                package?.LogPackageError(typeof(ProjectScriptInfo).FullName, e);
                throw;
            }
        }

        ProjectScriptInfo _activeProject;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="dte"></param>
        public ScriptOptionsDialogModel([NotNull] MDKPackage package, [NotNull] DTE dte)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            if (dte == null)
                throw new ArgumentNullException(nameof(dte));

            var activeProject = dte.ActiveDocument?.ProjectItem?.ContainingProject;
            var allProjects = dte.Solution.Projects.Cast<Project>().Where(p => p.IsLoaded()).ToArray();
            Projects = new ReadOnlyCollection<ProjectScriptInfo>(allProjects.Where(p => !string.IsNullOrEmpty(p.FullName)).Select(p => LoadScriptInfo(package, p)).Where(p => p.IsValid).ToArray());
            ActiveProject = activeProject != null ? Projects.FirstOrDefault(p => p.FileName == activeProject.FullName) ?? Projects.FirstOrDefault() : Projects.FirstOrDefault();
        }

        /// <summary>
        /// The currently selected project
        /// </summary>
        public ProjectScriptInfo ActiveProject
        {
            get => _activeProject;
            set
            {
                if (Equals(value, _activeProject))
                    return;
                _activeProject = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A list of valid script projects
        /// </summary>
        public ReadOnlyCollection<ProjectScriptInfo> Projects { get; }

        /// <summary>
        /// Determines whether there are any valid projects available
        /// </summary>
        public bool HasValidProjects => Projects.Count > 0;

        /// <summary>
        /// Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave()
        {
            foreach (var item in Projects)
                if (item.HasChanges)
                    item.Save();
            return true;
        }
    }
}
