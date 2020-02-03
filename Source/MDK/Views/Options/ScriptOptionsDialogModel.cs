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
        KeyValuePair<MinificationLevel, string> _selectedMinifier;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="projectProperties"></param>
        public ScriptOptionsDialogModel([NotNull] MDKPackage package, [NotNull] MDKProjectProperties projectProperties)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            Minifiers = new Collection<KeyValuePair<MinificationLevel, string>>
            {
                new KeyValuePair<MinificationLevel, string>(MinificationLevel.None, "None"),
                new KeyValuePair<MinificationLevel, string>(MinificationLevel.StripComments, "Strip Comments"),
                new KeyValuePair<MinificationLevel, string>(MinificationLevel.Full, "Full"),
            };
            ActiveProject = projectProperties ?? throw new ArgumentNullException(nameof(projectProperties));
            _selectedMinifier = this.Minifiers.FirstOrDefault(m => ActiveProject.Options.MinifyLevel == m.Key);
        }

        public KeyValuePair<MinificationLevel, string> SelectedMinifier
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

        public Collection<KeyValuePair<MinificationLevel, string>> Minifiers { get; } = new Collection<KeyValuePair<MinificationLevel, string>>();

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
