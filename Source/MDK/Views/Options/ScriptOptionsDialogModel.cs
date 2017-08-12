using System;
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
        /// <param name="projectScriptInfo"></param>
        public ScriptOptionsDialogModel([NotNull] MDKPackage package, [NotNull] ProjectScriptInfo projectScriptInfo)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            ActiveProject = projectScriptInfo ?? throw new ArgumentNullException(nameof(projectScriptInfo));
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
