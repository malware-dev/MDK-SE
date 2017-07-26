using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using JetBrains.Annotations;
using MDK.Views;
using MDK.VisualStudio;

namespace MDK.Services
{
    /// <summary>
    /// A service designed to detect whether a solution's script projects are in need of an upgrade after the VSPackage has been updated.
    /// </summary>
    public class ScriptUpgrades
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Makes sure the provided path is correctly related to the base directory and not the current environment directory.
        /// </summary>
        /// <param name="baseDirectory"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        static string ResolvePath(DirectoryInfo baseDirectory, string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
            return Path.GetFullPath(Path.Combine(baseDirectory.FullName, path));
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
        /// Determines whether the service is currently busy working.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// Shows a dialog to inform the user that some script projects needs to be upgraded. Performs the
        /// upgrade if the user accepts.
        /// </summary>
        /// <param name="result"></param>
        public void QueryUpgrade(ScriptSolutionAnalysisResult result)
        {
            var model = new RequestUpgradeDialogModel(Package, result);
            RequestUpgradeDialog.ShowDialog(model);
        }

        /// <summary>
        /// Upgrades the provided projects.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="analysisResults"></param>
        public void Upgrade(MDKPackage package, ScriptSolutionAnalysisResult analysisResults)
        {
            foreach (var project in analysisResults.BadProjects)
                Upgrade(project);
        }

        /// <summary>
        /// Upgrades the provided project to the current package version.
        /// </summary>
        /// <param name="projectResult"></param>
        void Upgrade(ScriptProjectAnalysisResult projectResult)
        {
            foreach (var badReference in projectResult.BadReferences)
            {
                switch (badReference.Type)
                {
                    case BadReferenceType.File:
                        badReference.Element.AddOrUpdateAttribute("Include", badReference.ExpectedPath);
                        break;
                    case BadReferenceType.Assembly:
                        badReference.Element.AddOrUpdateElement(XName.Get("HintPath", Xmlns), badReference.ExpectedPath);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            projectResult.ProjectDocument.Save(projectResult.ProjectInfo.FileName, SaveOptions.OmitDuplicateNamespaces);
        }

        /// <summary>
        /// Analyzes all the projects in the given solution, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> Analyze([NotNull] Solution solution, Version targetVersion)
        {
            using (new StatusBarAnimation(Package, Animation.General))
            {
                IsBusy = true;
                try
                {
                    var results = (await Task.WhenAll(solution.Projects.Cast<Project>().Select(project => AnalyzeProject(project, targetVersion))))
                        .Where(a => !a.IsIgnored && !a.IsValid)
                        .ToArray();
                    return new ScriptSolutionAnalysisResult(results.ToImmutableArray());
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Analyzes the given project, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> Analyze([NotNull] Project project, Version targetVersion)
        {
            using (new StatusBarAnimation(Package, Animation.General))
            {
                IsBusy = true;
                try
                {
                    var result = await AnalyzeProject(project, targetVersion);
                    if (!result.IsValid)
                        return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty.Add(result));
                    else
                        return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty);
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        Task<ScriptProjectAnalysisResult> AnalyzeProject(Project project, Version targetVersion)
        {
            if (!project.IsLoaded())
                return Task.FromResult(ScriptProjectAnalysisResult.Ignored);
            var projectInfo = new ProjectScriptInfo(Package, project.FullName, project.Name);
            if (!projectInfo.IsValid)
                return Task.FromResult(ScriptProjectAnalysisResult.Ignored);
            return Task.Run(() =>
            {
                var expectedGamePath = Path.GetFullPath(Package.Options.GetActualGameBinPath()).TrimEnd('\\');
                var expectedUtilityPath = Package.InstallPath.FullName.TrimEnd('\\');

                var badReferences = ImmutableArray.CreateBuilder<BadReference>();
                var projectFile = new FileInfo(projectInfo.FileName);
                var projectDir = projectFile.Directory ?? throw new InvalidOperationException($"Unexpected error: Could not determine the directory of the project {projectInfo.FileName}");
                var document = XDocument.Load(projectInfo.FileName);
                var xmlns = new XmlNamespaceManager(new NameTable());
                xmlns.AddNamespace("ms", Xmlns);

                AnalyzeReferences(document, xmlns, projectDir, expectedGamePath, expectedUtilityPath, badReferences);
                AnalyzeFiles(document, xmlns, projectDir, expectedGamePath, expectedUtilityPath, badReferences);

                return new ScriptProjectAnalysisResult(projectInfo, document, badReferences.ToImmutable());
            });
        }

        void AnalyzeFiles(XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedUtilityPath, ImmutableArray<BadReference>.Builder badReferences)
        {
            foreach (var element in document.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:*", xmlns))
            {
                var include = (string)element.Attribute("Include");
                var file = ResolvePath(projectDir, include);
                var gameFile = MDKPackage.GameFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (gameFile != null)
                    CheckFileReference(element, expectedGamePath, include, gameFile, badReferences);
                var utilityFile = MDKPackage.UtilityFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (utilityFile != null)
                    CheckFileReference(element, expectedUtilityPath, include, utilityFile, badReferences);
            }
        }

        void CheckFileReference(XElement element, string expectedPath, string currentPath, string fileName, ImmutableArray<BadReference>.Builder badReferences)
        {
            var correctPath = Path.GetFullPath(Path.Combine(expectedPath, fileName.TrimStart('\\')));
            if (!string.Equals(currentPath, correctPath, StringComparison.CurrentCultureIgnoreCase))
                badReferences.Add(new BadReference(BadReferenceType.File, element, currentPath, correctPath));
        }

        void AnalyzeReferences(XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedUtilityPath, ImmutableArray<BadReference>.Builder badReferences)
        {
            foreach (var element in document.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:Reference", xmlns))
            {
                var include = (string)element.Attribute("Include");
                var hintPath = (string)element.Element(XName.Get("HintPath", Xmlns));
                var gameAssemblyName = MDKPackage.GameAssemblyNames.FirstOrDefault(dll => dll == include);
                if (gameAssemblyName != null)
                    CheckAssemblyReference(projectDir, element, expectedGamePath, hintPath, gameAssemblyName, badReferences);
                var utilityAssemblyName = MDKPackage.UtilityAssemblyNames.FirstOrDefault(dll => dll == include);
                if (utilityAssemblyName != null)
                    CheckAssemblyReference(projectDir, element, expectedUtilityPath, hintPath, utilityAssemblyName, badReferences);
            }
        }

        void CheckAssemblyReference(DirectoryInfo projectDir, XElement element, string expectedPath, string hintPath, string assemblyName, ImmutableArray<BadReference>.Builder badReferences)
        {
            var dllFile = ResolvePath(projectDir, hintPath);
            var correctPath = Path.GetFullPath(Path.Combine(expectedPath, $"{assemblyName}.dll"));
            if (!string.Equals(dllFile, correctPath, StringComparison.CurrentCultureIgnoreCase))
                badReferences.Add(new BadReference(BadReferenceType.Assembly, element, dllFile, correctPath));
        }
    }
}
