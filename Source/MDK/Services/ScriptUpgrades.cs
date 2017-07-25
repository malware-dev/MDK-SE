using System;
using System.Collections.Generic;
using System.IO;
using EnvDTE;
using JetBrains.Annotations;
using MDK.Views;

namespace MDK.Services
{
    /// <summary>
    /// A service designed to detect whether a solution's script projects are in need of an upgrade after the VSPackage has been updated.
    /// </summary>
    public class ScriptUpgrades
    {
        /// <summary>
        /// Makes sure the provided path is correctly related to the base directory and not the current environment directory.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static string ResolvePath(DirectoryInfo baseDirectory, string path)
        {
            if (Path.IsPathRooted(path))
                return path;
            return Path.Combine(baseDirectory.FullName, path);
        }

        /// <summary>
        /// Creates a new instance of <see cref="ScriptUpgrades"/>
        /// </summary>
        /// <param name="package"></param>
        public ScriptUpgrades([NotNull] MDKPackage package)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// The associated <see cref="MDKPackage"/>
        /// </summary>
        public MDKPackage Package { get; }

        /// <summary>
        /// Detects whether there are projects in the given solution which is in need of an upgrade.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="targetVersion"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public bool Detect([NotNull] Solution solution, [NotNull] IList<ProjectScriptInfo> output, Version targetVersion)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            var detected = false;
            foreach (Project project in solution.Projects)
            {
                if (!string.IsNullOrEmpty(project.FileName))
                {
                    var projectScriptInfo = new ProjectScriptInfo(Package, project.FileName, project.Name);
                    if (!projectScriptInfo.IsValid)
                        continue;
                    if (targetVersion == projectScriptInfo.Version)
                        continue;

                    detected = true;
                    output.Add(projectScriptInfo);
                }
            }
            return detected;
        }

        /// <summary>
        /// Shows a dialog to inform the user that some script projects needs to be upgraded. Performs the
        /// upgrade if the user accepts.
        /// </summary>
        /// <param name="projects"></param>
        public void QueryUpgrade(IEnumerable<ProjectScriptInfo> projects)
        {
            var model = new RequestUpgradeDialogModel(Package, projects);
            RequestUpgradeDialog.ShowDialog(model);
        }

        /// <summary>
        /// Upgrades the provided projects.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="projects"></param>
        public void Upgrade(MDKPackage package, IEnumerable<ProjectScriptInfo> projects)
        {
            foreach (var project in projects)
            {
                Upgrade(project);
            }
        }

        /// <summary>
        /// Upgrades the provided project to the current package version.
        /// </summary>
        /// <param name="project"></param>
        void Upgrade(ProjectScriptInfo project)
        {
            if (!project.IsValid)
                throw new InvalidOperationException("Cannot update an invalid project");

            var projectPath = new FileInfo(project.FileName);
            var projectDir = projectPath.Directory;

            var currentUtilityPath = new FileInfo(new Uri(GetType().Assembly.CodeBase).LocalPath).Directory;
            var currentUtilityBasePath = currentUtilityPath?.Parent;
            if (currentUtilityBasePath == null)
                return;

            var projectUtilityPath = new FileInfo(ResolvePath(projectDir, project.UtilityPath));
            if (!projectUtilityPath.FullName.Equals(currentUtilityBasePath.FullName, StringComparison.CurrentCultureIgnoreCase))
            {
                project.UtilityPath = currentUtilityPath.FullName;
                var metaFileName = Path.Combine(Path.GetDirectoryName(project.FileName) ?? ".", "mdk.meta");
                Dictionary<string, string> content;
                if (File.Exists(metaFileName))
                    content = DictionaryFile.Load(metaFileName, StringComparer.CurrentCultureIgnoreCase);
                else
                    content = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
                content["version"] = MDKPackage.Version.ToString();
                DictionaryFile.Save(metaFileName, content);
                project.Save();
            }
        }
    }
}
