using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Sandbox;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

namespace Malware.MDKWhitelistExtractor
{
    public class ExtractorPlugin : IPlugin
    {
        const string ObjectBuilderPrefix = "MyObjectBuilder_";
        CommandLine _commandLine;
        bool _firstInit = true;

        public SpaceEngineersGame Game { get; private set; }

        public void Dispose() { }

        public void Init(object gameInstance)
        {
            _commandLine = new CommandLine(Environment.CommandLine);

            Game = (SpaceEngineersGame)gameInstance;
        }

        public void Update()
        {
            if (_firstInit)
            {
                _firstInit = false;
                MySession.AfterLoading += MySession_AfterLoading;
                MySessionLoader.LoadInventoryScene();
            }
        }

        void WriteWhitelists(string[] targets)
        {
            var ingameTypes = new List<string>();
            var modTypes = new List<string>();
            foreach (var item in MyScriptCompiler.Static.Whitelist.GetWhitelist())
            {
                if ((item.Value & MyWhitelistTarget.Ingame) != 0)
                    ingameTypes.Add(item.Key);
                if ((item.Value & MyWhitelistTarget.ModApi) != 0)
                    modTypes.Add(item.Key);
            }

            foreach (var target in targets)
            {
                File.WriteAllText(target, string.Join(Environment.NewLine, ingameTypes));
                var extension = Path.GetExtension(target);
                var modTarget = Path.ChangeExtension(target, ".mod" + extension);
                File.WriteAllText(modTarget, string.Join(Environment.NewLine, modTypes));
            }
        }

        void GrabTerminalActions(CommandLine commandLine)
        {
            try
            {
                var targetsArgumentIndex = commandLine.IndexOf("-terminalcaches");
                if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
                    return;
                var targetsArgument = commandLine[targetsArgumentIndex + 1];
                var targets = targetsArgument.Split(';');

                var gameAssembly = Game.GetType().Assembly;
                var blockTypes = FindBlocks(gameAssembly).ToArray();
                var blocks = new List<BlockInfo>();
                var experimentalMode = MySandboxGame.Config.ExperimentalMode;
                MySandboxGame.Config.ExperimentalMode = true;
                try
                {
                    foreach (var blockType in blockTypes)
                    {
                        var instance = (IMyTerminalBlock)Activator.CreateInstance(blockType);
                        var actions = new List<ITerminalAction>(new List<ITerminalAction>());
                        var properties = new List<ITerminalProperty>();
                        instance.GetActions(actions);
                        instance.GetProperties(properties);
                        var blockInfo = new BlockInfo(blockType, FindTypeDefinition(blockType), FindInterface(blockType), actions, properties);
                        if (blockInfo.BlockInterfaceType != null && blocks.All(b => b.BlockInterfaceType != blockInfo.BlockInterfaceType))
                            blocks.Add(blockInfo);
                    }
                }
                finally
                {
                    MySandboxGame.Config.ExperimentalMode = experimentalMode;
                }

                WriteTerminals(blocks, targets);
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
                throw;
            }
        }

        void WriteTerminals(List<BlockInfo> blocks, string[] targets)
        {
            var document = new XDocument(new XElement("terminals"));
            foreach (var blockInfo in blocks)
                // ReSharper disable once PossibleNullReferenceException
                document.Root.Add(blockInfo.ToXElement());

            foreach (var target in targets)
                document.Save(target);
        }

        void MySession_AfterLoading()
        {
            GrabWhitelist(_commandLine);
            GrabTerminalActions(_commandLine);
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                MySandboxGame.ExitThreadSafe();
            });
        }

        string FindTypeDefinition(Type block)
        {
            var attr = block.GetCustomAttribute<MyCubeBlockTypeAttribute>();
            if (attr == null)
                return null;
            return attr.ObjectBuilderType.Name.StartsWith(ObjectBuilderPrefix) ? attr.ObjectBuilderType.Name.Substring(ObjectBuilderPrefix.Length) : attr.ObjectBuilderType.Name;
        }

        Type FindInterface(Type block)
        {
            var attr = block.GetCustomAttribute<MyTerminalInterfaceAttribute>();
            return attr?.LinkedTypes.FirstOrDefault(l => l.Namespace?.EndsWith(".Ingame", StringComparison.OrdinalIgnoreCase) ?? false);

            //var interfaces = block.GetInterfaces().Where(i => typeof(IMyTerminalBlock).IsAssignableFrom(i) && (i.Namespace?.EndsWith(".Ingame") ?? false)).ToArray();
            //var candidateInterfaces = interfaces.Where(iface =>
            //    /* bad interface inheritance workaround */
            //    iface != typeof(IMyTerminalBlock) && iface != typeof(IMyFunctionalBlock) &&
            //    /* workaround end */
            //    !interfaces.Any(i => iface != i && iface.IsAssignableFrom(i))).ToArray();
            //return candidateInterfaces.SingleOrDefault();
        }

        IEnumerable<Type> FindBlocks(Assembly gameAssembly, HashSet<AssemblyName> visitedAssemblies = null)
        {
            visitedAssemblies = visitedAssemblies ?? new HashSet<AssemblyName>(new AssemblyNameComparer());
            visitedAssemblies.Add(gameAssembly.GetName());
            var companyAttribute = gameAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (companyAttribute?.Company == "Microsoft Corporation")
                yield break;

            var types = gameAssembly.DefinedTypes.Where(type => type.HasAttribute<MyCubeBlockTypeAttribute>());
            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;
                if (!typeof(MyTerminalBlock).IsAssignableFrom(type))
                    continue;
                var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                if (constructor == null)
                    continue;
                yield return type;
            }

            foreach (var assemblyName in gameAssembly.GetReferencedAssemblies())
            {
                if (visitedAssemblies.Contains(assemblyName))
                    continue;
                foreach (var block in FindBlocks(Assembly.Load(assemblyName), visitedAssemblies))
                    yield return block;
            }
        }

        void GrabWhitelist(CommandLine commandLine)
        {
            var targetsArgumentIndex = commandLine.IndexOf("-whitelistcaches");
            if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
                return;
            var targetsArgument = commandLine[targetsArgumentIndex + 1];
            var targets = targetsArgument.Split(';');

            WriteWhitelists(targets);
        }

        class AssemblyNameComparer : IEqualityComparer<AssemblyName>
        {
            public bool Equals(AssemblyName x, AssemblyName y)
            {
                return string.Equals(x?.ToString(), y?.ToString());
            }

            public int GetHashCode(AssemblyName obj)
            {
                return obj.ToString().GetHashCode();
            }
        }

        public class BlockInfo
        {
            public BlockInfo(Type blockType, string typeDefinition, Type blockInterfaceType, List<ITerminalAction> actions, List<ITerminalProperty> properties)
            {
                BlockType = blockType;
                TypeDefinition = typeDefinition;
                BlockInterfaceType = blockInterfaceType;
                Actions = new ReadOnlyCollection<ITerminalAction>(actions);
                Properties = new ReadOnlyCollection<ITerminalProperty>(properties);
            }

            public Type BlockType { get; }
            public string TypeDefinition { get; }
            public Type BlockInterfaceType { get; }

            public ReadOnlyCollection<ITerminalProperty> Properties { get; set; }

            public ReadOnlyCollection<ITerminalAction> Actions { get; set; }

            public void Write(TextWriter writer)
            {
                writer.WriteLine(BlockInterfaceType.FullName);
                foreach (var action in Actions)
                    writer.WriteLine($"- action {action.Id}");
                foreach (var property in Properties)
                    writer.WriteLine($"- action {property.Id} {DetermineType(property.TypeName)}");
            }

            string DetermineType(string propertyTypeName)
            {
                return propertyTypeName;
            }

            public XElement ToXElement()
            {
                var root = new XElement("block", new XAttribute("type", BlockInterfaceType.FullName ?? ""), new XAttribute("typedefinition", TypeDefinition ?? ""));
                foreach (var action in Actions)
                    root.Add(new XElement("action", new XAttribute("name", action.Id), new XAttribute("text", action.Name)));
                foreach (var property in Properties)
                    root.Add(new XElement("property", new XAttribute("name", property.Id), new XAttribute("type", DetermineType(property.TypeName))));
                return root;
            }
        }
    }
}