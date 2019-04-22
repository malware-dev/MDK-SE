using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BackupFix : Fix
    {
        public static class ZipHelper
        {
            public static void CreateFromDirectory(
                string sourceDirectoryName,
                string destinationArchiveFileName,
                CompressionLevel compressionLevel,
                bool includeBaseDirectory,
                Predicate<string> filter
            )
            {
                if (string.IsNullOrEmpty(sourceDirectoryName))
                {
                    throw new ArgumentNullException(nameof(sourceDirectoryName));
                }

                if (string.IsNullOrEmpty(destinationArchiveFileName))
                {
                    throw new ArgumentNullException(nameof(destinationArchiveFileName));
                }

                var filesToAdd = Directory.GetFiles(sourceDirectoryName, "*", SearchOption.AllDirectories);
                var entryNames = GetEntryNames(filesToAdd, sourceDirectoryName, includeBaseDirectory);
                using (var zipFileStream = new FileStream(destinationArchiveFileName, FileMode.Create))
                {
                    using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
                    {
                        for (int i = 0; i < filesToAdd.Length; i++)
                        {
                            // Add the following condition to do filtering:
                            if (!filter(filesToAdd[i]))
                            {
                                continue;
                            }

                            archive.CreateEntryFromFile(filesToAdd[i], entryNames[i], compressionLevel);
                        }
                    }
                }
            }
        }

        static string[] GetEntryNames(string[] names, string sourceFolder, bool includeBaseName)
        {
            if (names == null || names.Length == 0)
                return new string[0];

            if (includeBaseName)
                sourceFolder = Path.GetDirectoryName(sourceFolder);

            int length = string.IsNullOrEmpty(sourceFolder) ? 0 : sourceFolder.Length;
            if (length > 0 && sourceFolder != null && sourceFolder[length - 1] != Path.DirectorySeparatorChar && sourceFolder[length - 1] != Path.AltDirectorySeparatorChar)
                length++;

            var result = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                result[i] = names[i].Substring(length);
            }

            return result;
        }

        static bool NeedsBackup(HealthProblem healthProblem)
        {
            switch (healthProblem.Code)
            {
                case HealthCode.Outdated:
                case HealthCode.MissingPathsFile:
                case HealthCode.BadInstallPath:
                case HealthCode.BadGamePath:
                case HealthCode.BadOutputPath:
                    return true;
            }

            return false;
        }

        public BackupFix() : base(0, HealthCode.Healthy) { }

        public override void Apply(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Creating a backup in the parent folder...";
            var directory = Path.GetDirectoryName(analysis.FileName) ?? ".\\";
            if (!directory.EndsWith("\\"))
                directory += "\\";
            var zipFileName = $"{Path.GetFileNameWithoutExtension(analysis.FileName)}_Backup_{DateTime.Now:yyyy-MM-dd-HHmmssfff}.zip";
            var tmpZipName = Path.Combine(Path.GetTempPath(), zipFileName);
            ZipHelper.CreateFromDirectory(directory, tmpZipName, CompressionLevel.Fastest, false, path => OnlyInterestingFiles(directory, path));
            var backupDirectory = new DirectoryInfo(Path.Combine(directory, "..\\"));
            File.Copy(tmpZipName, Path.Combine(backupDirectory.FullName, zipFileName));
            File.Delete(tmpZipName);
            status.Description = "Backup created";
        }

        bool OnlyInterestingFiles(string baseDirectory, string path)
        {
            path = path.Substring(baseDirectory.Length);
            if (path.Equals(".vs", StringComparison.CurrentCultureIgnoreCase) || path.StartsWith(".vs\\", StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (path.Equals("bin", StringComparison.CurrentCultureIgnoreCase) || path.StartsWith("bin\\", StringComparison.CurrentCultureIgnoreCase))
                return false;
            if (path.Equals("obj", StringComparison.CurrentCultureIgnoreCase) || path.StartsWith("obj\\", StringComparison.CurrentCultureIgnoreCase))
                return false;
            return true;
        }

        public override bool IsApplicableTo(HealthAnalysis project) => project.Problems.Any(NeedsBackup);
    }
}
