using System.Collections.Immutable;
using System.Xml.Linq;

namespace Malware.MDKServices
{
    /// <summary>
    /// Represents the results for a single project of an analysis made by <see cref="ScriptUpgrades_Legacy.Analyze(EnvDTE.Project,ScriptUpgradeAnalysisOptions)"/>.
    /// </summary>
    public class ScriptProjectAnalysisResult_Legacy
    {
        /// <summary>
        /// Represents the results of an analysis which was ignored and should be disregarded.
        /// </summary>
        public static readonly ScriptProjectAnalysisResult_Legacy NonScriptProjectResult = new ScriptProjectAnalysisResult_Legacy(null, null, null, default(WhitelistReference), ImmutableArray<BadReference>.Empty, true);

        /// <summary>
        /// Creates a new instance of <see cref="ScriptProjectAnalysisResult_Legacy"/>
        /// </summary>
        /// <param name="project"></param>
        /// <param name="projectProperties">Basic information about the analyzed project</param>
        /// <param name="propsDocument">The source XML document of the MDK props file</param>
        /// <param name="whitelist">Whitelist verification results</param>
        /// <param name="badReferences">A list of bad file- or assembly references</param>
        /// <param name="hasValidGamePath"></param>
        public ScriptProjectAnalysisResult_Legacy(EnvDTE.Project project, MDKProjectProperties projectProperties, XDocument propsDocument, WhitelistReference whitelist, ImmutableArray<BadReference> badReferences, bool hasValidGamePath)
        {
            Project = project;
            ProjectProperties = projectProperties;
            PropsDocument = propsDocument;
            BadReferences = badReferences;
            Whitelist = whitelist;
            IsScriptProject = projectProperties != null;
            HasValidGamePath = hasValidGamePath;
            IsValid = BadReferences.Length == 0 && whitelist.IsValid && hasValidGamePath;
        }

        /// <summary>
        /// This is not a script project and should be ignored.
        /// </summary>
        public bool IsScriptProject { get; }

        /// <summary>
        /// The DTE project
        /// </summary>
        public EnvDTE.Project Project { get; }

        /// <summary>
        /// Basic information about the analyzed project.
        /// </summary>
        public MDKProjectProperties ProjectProperties { get; }

        /// <summary>
        /// The source XML document of the project file.
        /// </summary>
        public XDocument PropsDocument { get; }

        /// <summary>
        /// Returns a list of bad file- or assembly references.
        /// </summary>
        public ImmutableArray<BadReference> BadReferences { get; }

        /// <summary>
        /// Determines whether the registered game path is valid.
        /// </summary>
        public bool HasValidGamePath { get; }

        /// <summary>
        /// Whitelist verification result
        /// </summary>
        public WhitelistReference Whitelist { get; }

        /// <summary>
        /// Determines whether the analyzed project is fully valid and do not require any updates.
        /// </summary>
        public bool IsValid { get; }
    }
}
