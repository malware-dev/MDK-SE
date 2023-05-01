using Malware.MDKServices;
using System.Threading.Tasks;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BadInstallPathFix: Fix
    {
        public BadInstallPathFix(): base(3000, HealthCode.BadInstallPath) { }

        public override Task ApplyAsync(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Fixing bad install path";
            analysis.Properties.Paths.InstallPath = analysis.AnalysisOptions.InstallPath;
            analysis.Properties.Paths.Save();
            status.Description = "Fixed bad install path";
            return Task.CompletedTask;
        }
    }
}