using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Malware.MDKUtilities;

namespace Mal.DocGen2.Services
{
    class WhitelistAndTerminalCaches
    {
        public static async Task Update(string path, Action<string> stateChangedFn)
        {
            stateChangedFn?.Invoke("Starting the game...");
            var steam = new Steam();
            if (!steam.Exists)
                throw new InvalidOperationException("Cannot find Steam");

            var appId = SpaceEngineers.SteamAppId;
            var pluginPath = Path.GetFullPath("MDKWhitelistExtractor.dll");
            var whitelistTarget = path;
            var terminalTarget = path;
            var appdata = Path.Combine(path, "GameStartupPath");
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
                "-appdata",
                $"\"{appdata}\"",
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
            stateChangedFn?.Invoke("Waiting for the game to start...");
            if (await ForProcess("SpaceEngineers", TimeSpan.FromSeconds(60)))
            {
                stateChangedFn?.Invoke("Waiting for the game to end...");
                await ForProcessToEnd("SpaceEngineers", TimeSpan.MaxValue);
            }
            stateChangedFn?.Invoke("Done.");
        }

        static async Task<bool> ForProcess(string processName, TimeSpan timeout)
        {
            return await Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    if (stopwatch.Elapsed >= timeout)
                        return false;
                    if (Process.GetProcessesByName(processName).Length > 0)
                        return true;
                    await Task.Delay(1000);
                }
            }).ConfigureAwait(false);
        }

        static async Task<bool> ForProcessToEnd(string processName, TimeSpan timeout)
        {
            return await Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                while (true)
                {
                    if (stopwatch.Elapsed >= timeout)
                        return false;
                    if (Process.GetProcessesByName(processName).Length == 0)
                        return true;
                    await Task.Delay(1000);
                }
            }).ConfigureAwait(false);
        }
    }
}