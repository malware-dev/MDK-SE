namespace Malware.MDKServices
{
    partial class MDKProjectOptions
    {
        internal static MDKProjectOptions Import(MDKProjectProperties.LegacyProjectScriptInfo_1_1 scriptInfo, string optionsFileName)
        {
            if (!scriptInfo.IsValid)
                return null;
            var options = new MDKProjectOptions(optionsFileName, true)
            {
                Minify = scriptInfo.Minify,
                TrimTypes = scriptInfo.TrimTypes
            };
            foreach (var ignore in scriptInfo.IgnoredFiles)
                options.IgnoredFiles.Add(ignore);
            foreach (var ignore in scriptInfo.IgnoredFolders)
                options.IgnoredFolders.Add(ignore);
            options.Commit();
            options.Version = scriptInfo.Version;
            return options;
        }
    }
}
