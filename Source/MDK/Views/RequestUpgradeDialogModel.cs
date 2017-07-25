using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using MDK.Services;

namespace MDK.Views
{
    /// <summary>
    /// The view model for the <see cref="RequestUpgradeDialog"/> view.
    /// </summary>
    public class RequestUpgradeDialogModel : DialogViewModel
    {
        /// <summary>
        /// Creates a new instance of this view model.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="projects"></param>
        public RequestUpgradeDialogModel([NotNull] MDKPackage package, [NotNull] IEnumerable<ProjectScriptInfo> projects)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));

            if (projects == null)
                throw new ArgumentNullException(nameof(projects));

            Projects = new ReadOnlyCollection<ProjectScriptInfo>(projects.ToArray());
        }

        /// <summary>
        /// The associated MDK package
        /// </summary>
        public MDKPackage Package { get; }

        /// <summary>
        /// Contains the list of projects to examine.
        /// </summary>
        public ReadOnlyCollection<ProjectScriptInfo> Projects { get; }

        /// <summary>
        /// Upgrades the projects.
        /// </summary>
        protected override bool OnSave()
        {
            Package.ScriptUpgrades.Upgrade(Package, Projects);
            return true;
        }
    }
}
