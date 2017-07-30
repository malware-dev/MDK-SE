using System;
using System.IO;
using System.Linq;

namespace Malware.MDKUtilities
{
    /// <summary>
    /// Utility service to retrieve information about Space Engineers (copyright Keen Software House, no affiliation)
    /// </summary>
    class SpaceEngineers
    {
        /// <summary>
        /// The <see cref="Steam"/> service
        /// </summary>
        public Steam Steam { get; } = new Steam();

        /// <summary>
        /// The Steam App ID of Space Engineers
        /// </summary>
        public const long SteamAppId = 244850;

        /// <summary>
        /// Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        public string GetInstallPath(params string[] subfolders)
        {
            if (!Steam.Exists)
                return null;
            var installFolder = Steam.GetInstallFolder("SpaceEngineers", "Bin64\\SpaceEngineers.exe");
            if (string.IsNullOrEmpty(installFolder))
                return null;
            if (subfolders == null || subfolders.Length == 0)
                return Path.GetFullPath(installFolder);

            subfolders = new[] {installFolder}.Concat(subfolders).ToArray();
            return Path.GetFullPath(Path.Combine(subfolders));
        }

        /// <summary>
        /// Attempts to get the default data path for Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        public string GetDataPath(params string[] subfolders)
        {
            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers");
            if (subfolders == null || subfolders.Length <= 0)
                return Path.GetFullPath(dataFolder);

            subfolders = new[] {dataFolder}.Concat(subfolders).ToArray();
            return Path.GetFullPath(Path.Combine(subfolders));
        }
    }
}
