using System;
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
        /// Determines whether the service is currently busy working.
        /// </summary>
        public bool IsBusy { get; private set; }

        /// <summary>
        /// Shows a dialog to inform the user that some script projects needs to be upgraded. Performs the
        /// upgrade if the user accepts.
        /// </summary>
        /// <param name="package"></param>
        /// <param name="result"></param>
        public void QueryUpgrade([NotNull] MDKPackage package, ScriptSolutionAnalysisResult result)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            var model = new RequestUpgradeDialogModel(package, result);
            RequestUpgradeDialog.ShowDialog(model);
        }

        /// <summary>
        /// Analyzes all the projects in the given solution, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="options"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> AnalyzeAsync([NotNull] Solution solution, ScriptUpgradeAnalysisOptions options, MDKPackage package = null)
        {
            StatusBarAnimation animation = null;
            if (package != null)
                animation = new StatusBarAnimation(package, Animation.General);

            IsBusy = true;
            try
            {
                var results = (await Task.WhenAll(solution.Projects.Cast<Project>().Select(project => Task.Run(() => AnalyzeProject(project, options)))))
                    .Where(a => a.IsScriptProject)
                    .ToArray();
                if (!results.Any())
                    return ScriptSolutionAnalysisResult.NoScriptProjectsResult;
                return new ScriptSolutionAnalysisResult(results.Where(r => !r.IsValid).ToImmutableArray());
            }
            finally
            {
                IsBusy = false;
                animation?.Dispose();
            }
        }

        /// <summary>
        /// Analyzes the given project, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult> AnalyzeAsync([NotNull] Project project, ScriptUpgradeAnalysisOptions options, MDKPackage package = null)
        {
            StatusBarAnimation animation = null;
            if (package != null)
                animation = new StatusBarAnimation(package, Animation.General);

            IsBusy = true;
            try
            {
                return await Task.Run(() => Analyze(project, options, package));
            }
            finally
            {
                IsBusy = false;
                animation?.Dispose();
            }
        }

        /// <summary>
        /// Analyzes the given project, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public ScriptSolutionAnalysisResult Analyze([NotNull] Project project, ScriptUpgradeAnalysisOptions options, MDKPackage package = null)
        {
            StatusBarAnimation animation = null;
            if (package != null)
                animation = new StatusBarAnimation(package, Animation.General);

            IsBusy = true;
            try
            {
                var result = AnalyzeProject(project, options);
                if (!result.IsScriptProject)
                    return ScriptSolutionAnalysisResult.NoScriptProjectsResult;
                if (!result.IsValid)
                    return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty.Add(result));
                else
                    return new ScriptSolutionAnalysisResult(ImmutableArray<ScriptProjectAnalysisResult>.Empty);
            }
            finally
            {
                IsBusy = false;
                animation?.Dispose();
            }
        }

        ScriptProjectAnalysisResult AnalyzeProject(Project project, ScriptUpgradeAnalysisOptions options)
        {
            if (!project.IsLoaded())
                return ScriptProjectAnalysisResult.NonScriptProjectResult;
            var projectInfo = ProjectScriptInfo.Load(project.FullName, project.Name);
            if (!projectInfo.IsValid)
                return ScriptProjectAnalysisResult.NonScriptProjectResult;
            var expectedGamePath = projectInfo.GetActualGameBinPath(options.DefaultGameBinPath).TrimEnd('\\');
            var expectedInstallPath = options.InstallPath.TrimEnd('\\');

            var badReferences = ImmutableArray.CreateBuilder<BadReference>();
            var projectFile = new FileInfo(projectInfo.FileName);
            var projectDir = projectFile.Directory ?? throw new InvalidOperationException($"Unexpected error: Could not determine the directory of the project {projectInfo.FileName}");
            var document = XDocument.Load(projectInfo.FileName);
            var xmlns = new XmlNamespaceManager(new NameTable());
            xmlns.AddNamespace("ms", Xmlns);

            AnalyzeReferences(document, xmlns, projectDir, expectedGamePath, expectedInstallPath, badReferences);
            AnalyzeFiles(document, xmlns, projectDir, expectedGamePath, expectedInstallPath, badReferences);

            return new ScriptProjectAnalysisResult(projectInfo, document, badReferences.ToImmutable());
        }

        void AnalyzeFiles(XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedInstallPath, ImmutableArray<BadReference>.Builder badReferences)
        {
            foreach (var element in document.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:*", xmlns))
            {
                var include = (string)element.Attribute("Include");
                var file = ResolvePath(projectDir, include);
                var gameFile = MDKPackage.GameFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (gameFile != null)
                    CheckFileReference(element, expectedGamePath, file, gameFile, badReferences);
                var utilityFile = MDKPackage.UtilityFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (utilityFile != null)
                    CheckFileReference(element, expectedInstallPath, file, utilityFile, badReferences);
            }
        }

        void CheckFileReference(XElement element, string expectedPath, string currentPath, string fileName, ImmutableArray<BadReference>.Builder badReferences)
        {
            var correctPath = Path.GetFullPath(Path.Combine(expectedPath, fileName.TrimStart('\\')));
            if (!string.Equals(currentPath, correctPath, StringComparison.CurrentCultureIgnoreCase))
                badReferences.Add(new BadReference(BadReferenceType.File, element, currentPath, correctPath));
        }

        void AnalyzeReferences(XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedInstallPath, ImmutableArray<BadReference>.Builder badReferences)
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
                    CheckAssemblyReference(projectDir, element, expectedInstallPath, hintPath, utilityAssemblyName, badReferences);
            }
        }

        void CheckAssemblyReference(DirectoryInfo projectDir, XElement element, string expectedPath, string hintPath, string assemblyName, ImmutableArray<BadReference>.Builder badReferences)
        {
            var dllFile = ResolvePath(projectDir, hintPath);
            var correctPath = Path.GetFullPath(Path.Combine(expectedPath, $"{assemblyName}.dll"));
            if (!string.Equals(dllFile, correctPath, StringComparison.CurrentCultureIgnoreCase))
                badReferences.Add(new BadReference(BadReferenceType.Assembly, element, dllFile, correctPath));
        }

        /// <summary>
        /// Upgrades the provided projects.
        /// </summary>
        /// <param name="analysisResults"></param>
        public void Upgrade(ScriptSolutionAnalysisResult analysisResults)
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
    }
}
