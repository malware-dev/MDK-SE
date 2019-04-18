using System.IO;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BadOutputPathFix : Fix
    {
        public BadOutputPathFix() : base(3000, HealthCode.BadOutputPath) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Fixing bad output path";
            var path = analysis.AnalysisOptions.DefaultOutputPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                status.Description = "Cannot find output path";
                status.Failed = true;
                return;
            }

            analysis.Properties.Paths.OutputPath = path;
            analysis.Properties.Paths.Save();
            status.Description = "Fixed bad output path";
        }
    }
}
