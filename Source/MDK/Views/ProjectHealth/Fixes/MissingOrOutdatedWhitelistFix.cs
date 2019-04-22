using System;
using System.IO;
using System.Linq;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class MissingOrOutdatedWhitelistFix : Fix
    {
        public MissingOrOutdatedWhitelistFix() : base(4000, HealthCode.MissingWhitelist) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Restoring whitelist cache";
            var sourceFileName = Path.Combine(analysis.Properties.Paths.InstallPath, "Analyzers\\whitelist.cache");
            if (!File.Exists(sourceFileName))
                throw new InvalidOperationException("Cannot find the source whitelist cache");
            var targetFileName = Path.Combine(Path.GetDirectoryName(analysis.FileName), "mdk\\whitelist.cache");
            File.Copy(sourceFileName, targetFileName, true);
            Include(analysis, targetFileName);
            status.Description = "Restored whitelist cache";
        }

        public override bool IsApplicableTo(HealthAnalysis project) => project.Problems.Any(p => p.Code == HealthCode.MissingWhitelist || p.Code == HealthCode.OutdatedWhitelist);
    }
}
