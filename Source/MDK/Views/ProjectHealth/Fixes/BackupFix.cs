using System;
using System.IO;
using System.IO.Compression;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BackupFix : Fix
    {
        public BackupFix() : base(0, HealthCode.Healthy) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Creating a backup in the parent folder...";
            var directory = Path.GetDirectoryName(analysis.FileName) ?? ".\\";
            var zipFileName = $"{Path.GetFileNameWithoutExtension(analysis.FileName)}_Backup_{DateTime.Now:yyyy-MM-dd-HHmmssfff}.zip";
            var tmpZipName = Path.Combine(Path.GetTempPath(), zipFileName);
            ZipFile.CreateFromDirectory(directory, tmpZipName, CompressionLevel.Fastest, false);
            var backupDirectory = new DirectoryInfo(Path.Combine(directory, "..\\"));
            File.Copy(tmpZipName, Path.Combine(backupDirectory.FullName, zipFileName));
            File.Delete(tmpZipName);
            status.Description = "Backup created";
        }

        public override bool IsApplicableTo(HealthAnalysis project) => true;
    }
}
