using System;
using Malware.MDKServices;
using MDK.Resources;

namespace MDK.Views.ProjectHealth.Fixes
{
    class OutdatedFix : Fix
    {
        public OutdatedFix() : base(1000, HealthCode.Outdated) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Upgrading outdated project format";
            if (analysis.Properties.Options.Version < new Version(1, 2))
            {
                var upgrader = new UpgradeFrom_1_1();
                upgrader.Upgrade(analysis);
                status.Description = "Project format updated";
                return;
            }

            throw new InvalidOperationException(string.Format(Text.ProjectHealthDialogModel_Upgrade_BadUpgradeVersion, analysis.Properties.Options.Version));
        }
    }
}
