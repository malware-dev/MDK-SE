using Malware.MDKServices;
using MDK.Resources;
using System;
using System.Threading.Tasks;

namespace MDK.Views.ProjectHealth.Fixes
{
    class OutdatedFix: Fix
    {
        public OutdatedFix(): base(1000, HealthCode.Outdated) { }

        public override async Task ApplyAsync(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Upgrading outdated project format";
            if (analysis.Properties.Options.Version < new Version(1, 2))
            {
                var upgrader = new UpgradeFrom_1_1();
                await Task.Run(() => upgrader.Upgrade(analysis));
                status.Description = "Project format updated";
                return;
            }

            throw new InvalidOperationException(string.Format(Text.ProjectHealthDialogModel_Upgrade_BadUpgradeVersion, analysis.Properties.Options.Version));
        }
    }
}