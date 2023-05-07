using Malware.MDKServices;
using System.Threading.Tasks;

namespace MDK.Views.ProjectHealth.Fixes
{
    class MissingPathsFileFix: Fix
    {
        public MissingPathsFileFix() : base(2000, HealthCode.MissingPathsFile) { }

        public override Task ApplyAsync(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Restoring missing paths file";
            analysis.Properties.Paths.InstallPath = analysis.AnalysisOptions.InstallPath;
            analysis.Properties.Paths.GameBinPath = analysis.AnalysisOptions.DefaultGameBinPath;
            analysis.Properties.Paths.OutputPath = analysis.AnalysisOptions.DefaultOutputPath;
            foreach (var reference in MDKProjectPaths.DefaultAssemblyReferences)
                analysis.Properties.Paths.AssemblyReferences.Add(reference);
            foreach (var reference in MDKProjectPaths.DefaultAnalyzerReferences)
                analysis.Properties.Paths.AnalyzerReferences.Add(reference);
            analysis.Properties.Paths.Save();
            status.Description = "Restored missing paths file";
            return Task.CompletedTask;
        }
    }
}
