using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using Malware.MDKServices;

namespace MDK.Views.Options
{
    /// <summary>
    /// The view model for <see cref="ScriptOptionsDialog"/>
    /// </summary>
    public class ScriptOptionsDialogModel : DialogViewModel
    {
        MDKProjectProperties _activeProject;
        KeyValuePair<MinifyLevel, string> _selectedMinifier;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="projectProperties"></param>
        public ScriptOptionsDialogModel([NotNull] MDKPackage package, [NotNull] MDKProjectProperties projectProperties)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            Minifiers = new Collection<KeyValuePair<MinifyLevel, string>>
            {
                new KeyValuePair<MinifyLevel, string>(MinifyLevel.None, "None"),
                new KeyValuePair<MinifyLevel, string>(MinifyLevel.StripComments, "Strip Comments"),
                new KeyValuePair<MinifyLevel, string>(MinifyLevel.Full, "Full"),
            };
            ActiveProject = projectProperties ?? throw new ArgumentNullException(nameof(projectProperties));
            _selectedMinifier = this.Minifiers.FirstOrDefault(m => ActiveProject.Options.MinifyLevel == m.Key);
        }

        /// <summary>
        /// The currently selected minifier configuration
        /// </summary>
        public KeyValuePair<MinifyLevel, string> SelectedMinifier
        {
            get => _selectedMinifier;
            set
            {
                if (value.Equals(_selectedMinifier)) return;
                _selectedMinifier = value;
                ActiveProject.Options.MinifyLevel = value.Key;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A list of available minifier configurations
        /// </summary>
        public Collection<KeyValuePair<MinifyLevel, string>> Minifiers { get; }

        /// <summary>
        /// The currently selected project
        /// </summary>
        public MDKProjectProperties ActiveProject
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
