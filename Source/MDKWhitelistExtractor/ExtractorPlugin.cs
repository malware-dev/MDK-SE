using System;
using System.Collections.Generic;
using System.IO;
using Sandbox;
using Sandbox.ModAPI;
using VRage.Plugins;
using VRage.Scripting;

namespace Malware.MDKWhitelistExtractor
{
    public class ExtractorPlugin : IPlugin
    {
        public MySandboxGame Game { get; private set; }

        public void Dispose()
        { }

        public void Init(object gameInstance)
        {
            var commandLine = new CommandLine(Environment.CommandLine);

            Game = (MySandboxGame)gameInstance;

            var targetsArgumentIndex = commandLine.IndexOf("-whitelistcaches");
            if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
            {
                Game.Exit();
                return;
            }
            var targetsArgument = commandLine[targetsArgumentIndex + 1];
            var targets = targetsArgument.Split(';');

            var types = new List<string>();
            foreach (var item in MyScriptCompiler.Static.Whitelist.GetWhitelist())
            {
                if (!item.Value.HasFlag(MyWhitelistTarget.Ingame))
                {
                    continue;
                }
                types.Add(item.Key);
            }
            foreach (var target in targets)
                File.WriteAllText(target, string.Join(Environment.NewLine, types));
            Game.Exit();
        }

        public void Update()
        { }
    }
}