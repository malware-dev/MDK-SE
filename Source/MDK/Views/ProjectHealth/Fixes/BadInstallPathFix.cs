using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BadInstallPathFix: Fix
    {
        public BadInstallPathFix() : base(3000, HealthCode.BadInstallPath) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Fixing bad install path";
            analysis.Properties.Paths.InstallPath = analysis.AnalysisOptions.InstallPath;
            analysis.Properties.Paths.Save();
            status.Description = "Fixed bad install path";
        }
    }
}
