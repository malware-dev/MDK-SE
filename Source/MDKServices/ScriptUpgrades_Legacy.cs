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

namespace Malware.MDKServices
{
    /// <summary>
    /// A service designed to detect whether a solution's script projects are in need of an upgrade after the VSPackage has been updated.
    /// </summary>
    public class ScriptUpgrades_Legacy
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";
        const string SourceWhitelistSubPath = @"Analyzers\whitelist.cache";
        const string TargetWhitelistSubPath = @"MDK\whitelist.cache";
        //const string TargetOptionsSubPath = @"MDK\MDK.options";
        //const string TargetPropsSubPath = @"MDK\MDK.props";

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

        int _busyCount;

        /// <summary>
        /// Fired whenever the <see cref="IsBusy"/> property changes.
        /// </summary>
        public event EventHandler IsBusyChanged;

        /// <summary>
        /// Determines whether the service is currently busy working.
        /// </summary>
        public bool IsBusy => _busyCount > 0;

        /// <summary>
        /// Called to begin a work load block. Manages the <see cref="IsBusy"/> property and <see cref="IsBusyChanged"/> event.
        /// </summary>
        protected void BeginBusy()
        {
            _busyCount++;
            if (_busyCount == 1)
                IsBusyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called to end a work load block. Manages the <see cref="IsBusy"/> property and <see cref="IsBusyChanged"/> event.
        /// </summary>
        protected void EndBusy()
        {
            if (_busyCount == 0)
                return;
            _busyCount--;
            if (_busyCount == 0)
                IsBusyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Analyzes all the projects in the given solution, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult_Legacy> AnalyzeAsync([NotNull] Solution solution, ScriptUpgradeAnalysisOptions options)
        {
            BeginBusy();
            try
            {
                var results = (await Task.WhenAll(solution.Projects.Cast<Project>().Select(project => Task.Run(() => AnalyzeProject(project, options)))))
                    .Where(a => a.IsScriptProject)
                    .ToArray();
                if (!results.Any())
                    return ScriptSolutionAnalysisResult_Legacy.NoScriptProjectsResult;
                return new ScriptSolutionAnalysisResult_Legacy(results.Where(r => !r.IsValid).ToImmutableArray());
            }
            finally
            {
                EndBusy();
            }
        }

        /// <summary>
        /// Analyzes the given project, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ScriptSolutionAnalysisResult_Legacy> AnalyzeAsync([NotNull] Project project, ScriptUpgradeAnalysisOptions options)
        {
            BeginBusy();
            try
            {
                return await Task.Run(() => Analyze(project, options));
            }
            finally
            {
                EndBusy();
            }
        }

        /// <summary>
        /// Analyzes the given project, attempting to find irregularities like bad assembly- or file references.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public ScriptSolutionAnalysisResult_Legacy Analyze([NotNull] Project project, ScriptUpgradeAnalysisOptions options)
        {
            BeginBusy();
            try
            {
                var result = AnalyzeProject(project, options);
                if (!result.IsScriptProject)
                    return ScriptSolutionAnalysisResult_Legacy.NoScriptProjectsResult;
                if (!result.IsValid)
                    return new ScriptSolutionAnalysisResult_Legacy(ImmutableArray<ScriptProjectAnalysisResult_Legacy>.Empty.Add(result));
                else
                    return new ScriptSolutionAnalysisResult_Legacy(ImmutableArray<ScriptProjectAnalysisResult_Legacy>.Empty);
            }
            finally
            {
                EndBusy();
            }
        }

        ScriptProjectAnalysisResult_Legacy AnalyzeProject(Project project, ScriptUpgradeAnalysisOptions options)
        {
            if (!project.IsLoaded())
                return ScriptProjectAnalysisResult_Legacy.NonScriptProjectResult;
            var projectInfo = MDKProjectProperties.Load(project.FullName, project.Name);
            if (!projectInfo.IsValid)
                return ScriptProjectAnalysisResult_Legacy.NonScriptProjectResult;

            if (projectInfo.Options.Version < new Version(1, 2))
            {
                return new ScriptProjectAnalysisResult_Legacy(project, projectInfo, null, default, ImmutableArray<BadReference>.Empty, false);
            }

            var hasValidGamePath = true;
            string expectedGamePath;
            try
            {
                expectedGamePath = projectInfo.Paths.GameBinPath?.TrimEnd('\\');
            }
            catch (GamePathUnavailableException)
            {
                hasValidGamePath = false;
                expectedGamePath = null;
            }

            var expectedInstallPath = options.InstallPath.TrimEnd('\\');

            var badReferences = ImmutableArray.CreateBuilder<BadReference>();
            var projectFile = new FileInfo(projectInfo.Paths.FileName);
            var projectDir = projectFile.Directory;
            var propsDocument = XDocument.Load(projectInfo.FileName);
            var xmlns = new XmlNamespaceManager(new NameTable());
            xmlns.AddNamespace("ms", Xmlns);

            AnalyzeReferences(options, propsDocument, xmlns, projectDir, expectedGamePath, expectedInstallPath, badReferences);
            AnalyzeFiles(options, propsDocument, xmlns, projectDir, expectedGamePath, expectedInstallPath, badReferences);
            var whitelist = VerifyWhitelist(propsDocument, projectDir, expectedInstallPath);

            return new ScriptProjectAnalysisResult_Legacy(project, projectInfo, propsDocument, whitelist, badReferences.ToImmutable(), hasValidGamePath);
        }

        WhitelistReference VerifyWhitelist(XDocument document, DirectoryInfo projectDir, string expectedInstallPath)
        {
            var hasWhitelistElement = document
                                          .Element($"{{{Xmlns}}}Project")?
                                          .Elements($"{{{Xmlns}}}ItemGroup")
                                          .Elements($"{{{Xmlns}}}AdditionalFiles")
                                          .Any(e => string.Equals((string)e.Attribute("Include"), TargetWhitelistSubPath, StringComparison.CurrentCultureIgnoreCase))
                                      ?? false;

            var sourceWhitelistFileInfo = new FileInfo(Path.Combine(expectedInstallPath, SourceWhitelistSubPath));
            var targetWhitelistFileInfo = new FileInfo(Path.Combine(projectDir.FullName, TargetWhitelistSubPath));

            return new WhitelistReference(hasWhitelistElement, targetWhitelistFileInfo.Exists && sourceWhitelistFileInfo.Exists && sourceWhitelistFileInfo.LastWriteTime <= targetWhitelistFileInfo.LastWriteTime, sourceWhitelistFileInfo.FullName, targetWhitelistFileInfo.FullName);
        }

        void AnalyzeFiles(ScriptUpgradeAnalysisOptions options, XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedInstallPath, ImmutableArray<BadReference>.Builder badReferences)
        {
            foreach (var element in document.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:*", xmlns))
            {
                var include = (string)element.Attribute("Include");
                var file = ResolvePath(projectDir, include);
                var gameFile = options.GameFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
                if (gameFile != null && expectedGamePath != null)
                    CheckFileReference(element, expectedGamePath, file, gameFile, badReferences);
                var utilityFile = options.UtilityFiles.FirstOrDefault(fileName => file.EndsWith(fileName, StringComparison.CurrentCultureIgnoreCase));
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

        void AnalyzeReferences(ScriptUpgradeAnalysisOptions options, XDocument document, XmlNamespaceManager xmlns, DirectoryInfo projectDir, string expectedGamePath, string expectedInstallPath, ImmutableArray<BadReference>.Builder badReferences)
        {
            foreach (var element in document.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:Reference", xmlns))
            {
                var include = (string)element.Attribute("Include");
                var hintPath = (string)element.Element(XName.Get("HintPath", Xmlns));
                var gameAssemblyName = options.GameAssemblyNames.FirstOrDefault(dll => dll == include);
                if (gameAssemblyName != null && expectedGamePath != null)
                    CheckAssemblyReference(projectDir, element, expectedGamePath, hintPath, gameAssemblyName, badReferences);
                var utilityAssemblyName = options.UtilityAssemblyNames.FirstOrDefault(dll => dll == include);
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
        /// Repairs the provided projects.
        /// </summary>
        /// <param name="analysisResults"></param>
        public void Repair(ScriptSolutionAnalysisResult_Legacy analysisResults)
        {
            foreach (var project in analysisResults.BadProjects)
            {
                var handle = project.Project.Unload();
                Repair(project);
                handle.Reload();
            }
        }

        /// <summary>
        /// Repairs the provided project.
        /// </summary>
        /// <param name="projectResult"></param>
        void Repair(ScriptProjectAnalysisResult_Legacy projectResult)
        {
            //RepairBadReferences(projectResult);
            //RepairWhitelist(projectResult);
            //RepairOptions(projectResult);
            //var projectFileInfo = new FileInfo(projectResult.ProjectProperties.FileName);
            //var targetPropsFileInfo = new FileInfo(Path.Combine(projectFileInfo.Directory.FullName, TargetPropsSubPath));
            //projectResult.PropsDocument.Save(targetPropsFileInfo.FullName, SaveOptions.OmitDuplicateNamespaces);
        }

        void RepairOptions(ScriptProjectAnalysisResult_Legacy projectResult)
        {
            //var projectFileInfo = new FileInfo(projectResult.ProjectProperties.FileName);
            //var targetOptionsFileInfo = new FileInfo(Path.Combine(projectFileInfo.Directory.FullName, TargetOptionsSubPath));
            //var document = XDocument.Load(targetOptionsFileInfo.FullName);
            //var attribute = document.Element("mdk")?.Attribute("version");
            //if (attribute != null)
            //{
            //    attribute.Value = MDKProjectProperties.TargetPackageVersion.ToString();
            //    document.Save(targetOptionsFileInfo.FullName, SaveOptions.OmitDuplicateNamespaces);
            //}
        }

        void RepairWhitelist(ScriptProjectAnalysisResult_Legacy projectResult)
        {
            //var whitelist = projectResult.Whitelist;
            //if (!whitelist.HasValidWhitelistFile)
            //{
            //    var projectFileInfo = new FileInfo(projectResult.ProjectProperties.FileName);
            //    var targetWhitelistFileInfo = new FileInfo(Path.Combine(projectFileInfo.Directory.FullName, TargetWhitelistSubPath));
            //    if (!targetWhitelistFileInfo.Directory.Exists)
            //        targetWhitelistFileInfo.Directory.Create();
            //    File.Copy(whitelist.SourceWhitelistFilePath, targetWhitelistFileInfo.FullName, true);
            //}

            //if (!whitelist.HasValidWhitelistElement)
            //{
            //    var projectElement = projectResult.PropsDocument
            //        .Element($"{{{Xmlns}}}Project");
            //    if (projectElement == null)
            //        throw new InvalidOperationException("Bad MDK project");
            //    var badElements = projectElement
            //        .Elements($"{{{Xmlns}}}ItemGroup")
            //        .Elements()
            //        .Where(e =>
            //            string.Equals((string)e.Attribute("Include"), TargetWhitelistSubPath, StringComparison.CurrentCultureIgnoreCase)
            //            || string.Equals((string)e.Element($"{{{Xmlns}}}Link"), TargetWhitelistSubPath, StringComparison.CurrentCultureIgnoreCase))
            //        .ToArray();
            //    foreach (var element in badElements)
            //        element.Remove();

            //    var targetGroup = projectElement
            //        .Elements($"{{{Xmlns}}}ItemGroup")
            //        .Elements()
            //        .FirstOrDefault(e => string.Equals((string)e.Attribute("Include"), TargetOptionsSubPath, StringComparison.CurrentCultureIgnoreCase))
            //        ?.Parent;
            //    if (targetGroup == null)
            //    {
            //        targetGroup = new XElement(XName.Get("ItemGroup", Xmlns));
            //        projectElement.Add(targetGroup);
            //    }

            //    var itemElement = new XElement(XName.Get("AdditionalFiles", Xmlns),
            //        new XAttribute("Include", TargetWhitelistSubPath));
            //    targetGroup.Add(itemElement);
            //}
        }

        static void RepairBadReferences(ScriptProjectAnalysisResult_Legacy projectResult)
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
        }
    }
}
