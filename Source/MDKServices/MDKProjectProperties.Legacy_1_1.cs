namespace Malware.MDKServices
{
    /// <summary>
    /// Provides information about a given project and its script.
    /// </summary>
    public partial class MDKProjectProperties
    {
        static partial void ImportLegacy_1_1(string legacyOptionsFileName, ref MDKProjectOptions options, string optionsFileName, ref MDKProjectPaths paths, string pathsFileName)
        {
            var scriptInfo = LegacyProjectScriptInfo_1_1.Load(legacyOptionsFileName);
            if (scriptInfo.IsValid)
            {
                options = MDKProjectOptions.Import(scriptInfo, optionsFileName);
                paths = MDKProjectPaths.Import(scriptInfo, pathsFileName);
            }
        }
    }
}
