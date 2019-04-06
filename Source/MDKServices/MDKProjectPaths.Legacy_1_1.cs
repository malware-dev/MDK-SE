using System;

namespace Malware.MDKServices
{
    partial class MDKProjectPaths
    {
        internal static MDKProjectPaths Import(MDKProjectProperties.LegacyProjectScriptInfo_1_1 scriptInfo, string pathsFileName)
        {
            if (!scriptInfo.IsValid)
                return null;
            var paths = new MDKProjectPaths(pathsFileName, true)
            {
                GameBinPath = scriptInfo.GameBinPath,
                InstallPath = scriptInfo.InstallPath,
                OutputPath = scriptInfo.OutputPath
            };
            foreach (var reference in DefaultAssemblyReferences)
                paths.AssemblyReferences.Add(reference);
            foreach (var reference in DefaultAnalyzerReferences)
                paths.AnalyzerReferences.Add(reference);
            paths.Commit();
            paths.Version = scriptInfo.Version;
            return paths;
        }
    }
}
