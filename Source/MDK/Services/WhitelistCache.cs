using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Malware.MDKUtilities;

namespace MDK.Services
{
    /// <summary>
    /// A service for refreshing the Space Engineers ingame script whitelist cache file.
    /// </summary>
    public class WhitelistCache
    {
        /// <summary>
        /// Start space engineers with a dedicated plugin designed to update the ingame script whitelist cache file.
        /// </summary>
        public void Refresh(string installPath)
        {
            var steam = new Steam();
            if (!steam.Exists)
                throw new InvalidOperationException("Cannot find Steam");

            var appId = SpaceEngineers.SteamAppId;
            var pluginPath = Path.Combine(installPath, "MDKWhitelistExtractor.dll");
            var targetPath = Path.Combine(installPath, "Analyzers");
            var directoryInfo = new DirectoryInfo(targetPath);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            targetPath = Path.Combine(targetPath, "whitelist.cache");

            var args = new List<string>
            {
                $"-applaunch {appId}",
                $"-plugin \"{pluginPath}\"",
                "-nosplash",
                "-whitelistcaches",
                $"\"{targetPath}\""
            };

            var process = new Process
            {
                StartInfo =
                {
                    FileName = steam.ExePath,
                    Arguments = string.Join(" ", args)
                }
            };
            process.Start();
        }
    }
}
