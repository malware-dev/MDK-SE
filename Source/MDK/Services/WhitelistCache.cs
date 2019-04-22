using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task RefreshAsync(string installPath)
        {
            var steam = new Steam();
            if (!steam.Exists)
                throw new InvalidOperationException("Cannot find Steam");

            var appId = SpaceEngineers.SteamAppId;
            var pluginPath = Path.Combine(installPath, "MDKWhitelistExtractor.dll");
            var whitelistTarget = Path.Combine(installPath, "Analyzers");
            var terminalTarget = Path.Combine(installPath, "Analyzers");
            var directoryInfo = new DirectoryInfo(whitelistTarget);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            whitelistTarget = Path.Combine(whitelistTarget, "whitelist.cache");
            terminalTarget = Path.Combine(terminalTarget, "terminal.cache");

            var args = new List<string>
            {
                $"-applaunch {appId}",
                $"-plugin \"{pluginPath}\"",
                "-nosplash",
                "-skipintro",
                "-whitelistcaches",
                $"\"{whitelistTarget}\"",
                "-terminalcaches",
                $"\"{terminalTarget}\""
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

            await Task.Delay(5000);
            while (true)
            {
                var processByName = Process.GetProcessesByName("SpaceEngineers").Where(p => !p.HasExited).ToArray();
                if (processByName.Length == 0)
                    break;
                await Task.Delay(1000);
            }
        }
    }
}
