using System;
using EnvDTE;
using JetBrains.Annotations;
using Malware.MDKServices;

namespace MDK.Views.Options
{
    /// <summary>
    /// The view model for <see cref="ScriptOptionsDialog"/>
    /// </summary>
    public class ScriptOptionsDialogModel : DialogViewModel
    {
        ProjectScriptInfo _activeProject;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="dte"></param>
        /// <param name="project"></param>
        public ScriptOptionsDialogModel([NotNull] MDKPackage package, [NotNull] DTE dte, [NotNull] Project project)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            if (dte == null)
                throw new ArgumentNullException(nameof(dte));
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            ActiveProject = ProjectScriptInfo.Load(project.FullName, project.Name);
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
        /// Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave()
        {
            if (ActiveProject.HasChanges)
                ActiveProject.Save();
            return true;
        }
    }
}
