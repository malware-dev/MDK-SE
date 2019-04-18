using System.IO;
using Malware.MDKServices;
using Malware.MDKUtilities;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BadGamePathFix : Fix
    {
        public BadGamePathFix() : base(3000, HealthCode.BadGamePath) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Fixing bad game path";
            var path = analysis.AnalysisOptions.DefaultGameBinPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                status.Description = "Cannot find game path";
                status.Failed = true;
                return;
            }

            analysis.Properties.Paths.GameBinPath = path;
            analysis.Properties.Paths.Save();
            status.Description = "Fixed bad game path";
        }
    }
}
