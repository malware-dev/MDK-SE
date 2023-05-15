using Digi.BuildInfo.Features.LiveData;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using VRage;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;
using VRageMath;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;

namespace Malware.MDKWhitelistExtractor
{
    public class ExtractorPlugin: IPlugin
    {
        const string ObjectBuilderPrefix = "MyObjectBuilder_";
        CommandLine _commandLine;
        bool _firstInit = true;

        public SpaceEngineersGame Game { get; private set; }

        public void Dispose() { }

        public void Init(object gameInstance)
        {
            MyLog.Default.Info("Extractor Plugin Loaded.");
            _commandLine = new CommandLine(Environment.CommandLine);

            Game = (SpaceEngineersGame)gameInstance;
        }

        public async void Update()
        {
            if (_firstInit)
            {
                _firstInit = false;
                await Task.Delay(TimeSpan.FromSeconds(1));
                MySandboxGame.Static.Invoke(() =>
                {
                    MySession.AfterLoading += MySession_AfterLoading;
                    var screen = MyScreenManager.GetFirstScreenOfType<MyGuiScreenMainMenu>();
                    var button = (MyGuiControlButton)screen.Controls.FirstOrDefault(c => c is MyGuiControlButton button && MyTexts.Get(MyCommonTexts.ScreenMenuButtonInventory).EqualsStrFast(button.Text));
                    button.PressButton();
                }, "ExtractorPlugin");
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

        async Task<List<(MyCubeBlockDefinition, IMyTerminalBlock)>> SpawnBlocksForAnalysisAsync(CommandLine commandLine)
        {
            try
            {
                var targetsArgumentIndex = commandLine.IndexOf("-terminalcaches");
                if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
                    return null;

                var blocks = new List<(MyCubeBlockDefinition, IMyTerminalBlock)>();
                foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    var position = new Vector3D(100000, 0, 0);
                    if (definition is MyCubeBlockDefinition cbd)
                    {
                        await GameThread.SwitchToGameThread();
                        var tcs = new TaskCompletionSource<(MyCubeBlockDefinition, IMyTerminalBlock)>();
                        TempBlockSpawn.Spawn(cbd, deleteGridOnSpawn: false, spawnPos: position, callback: slim =>
                        {
                            if (slim.FatBlock is not IMyTerminalBlock block)
                            {
                                tcs.SetResult((null, null));
                                return;
                            }

                            tcs.SetResult((cbd, block));
                        });
                        var pair = await tcs.Task;
                        position.Z += 100;
                        if (pair.Item1 != null || pair.Item2 != null)
                            blocks.Add(pair);
                    }
                }

                return blocks;
            }
            catch (ReflectionTypeLoadException e)
            {
                foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
                throw;
            }
        }

        void GrabTerminalActions(CommandLine commandLine, List<(MyCubeBlockDefinition, IMyTerminalBlock)> blocks)
        {
            try
            {
                var targetsArgumentIndex = commandLine.IndexOf("-terminalcaches");
                if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
                    return;
                var targetsArgument = commandLine[targetsArgumentIndex + 1];
                var targets = targetsArgument.Split(';');

                var blockInfos = new List<BlockInfo>();
                foreach (var (cbd, block) in blocks)
                {
                    var infoAttribute = block.GetType().GetCustomAttribute<MyTerminalInterfaceAttribute>();
                    if (infoAttribute == null)
                    {
                        MyLog.Default.Info($"Could not get any info for {cbd.Id} because there's no interface attribute");
                        continue;
                    }

                    var ingameType = infoAttribute.LinkedTypes.FirstOrDefault(t => t.Namespace?.EndsWith(".Ingame") ?? false);
                    if (ingameType == null)
                    {
                        MyLog.Default.Info($"Could not get any info for {cbd.Id} because there's no ingame interface in the interface attribute");
                        continue;
                    }


                    var actions = new List<ITerminalAction>(new List<ITerminalAction>());
                    var properties = new List<ITerminalProperty>();
                    block.GetActions(actions);
                    block.GetProperties(properties);

                    MyLog.Default.Info($"Got {actions.Count} actions and {properties.Count} properties from {cbd.Id}");

                    //instance.GetActions(actions);
                    //instance.GetProperties(properties);
                    var blockInfo = new BlockInfo(block.GetType(), FindTypeDefinition(block.GetType()), ingameType, actions, properties);
                    if (blockInfo.BlockInterfaceType != null && blockInfos.All(b => b.BlockInterfaceType != blockInfo.BlockInterfaceType))
                        blockInfos.Add(blockInfo);
                }


                //    var terminalController = (IMyTerminalActionsHelper)MyTerminalControlFactoryHelper.Static;
                //    foreach (var blockType in blockTypes)
                //    {
                //        //var instance = (IMyTerminalBlock)Activator.CreateInstance(blockType);
                //        var actions = new List<ITerminalAction>(new List<ITerminalAction>());
                //        var properties = new List<ITerminalProperty>();
                //        terminalController.GetActions(blockType, actions);
                //        terminalController.GetProperties(blockType, properties);
                //        MyLog.Default.Info($"Got {actions.Count} actions and {properties.Count} properties from {blockType.Name}");

                //        //instance.GetActions(actions);
                //        //instance.GetProperties(properties);
                //        var blockInfo = new BlockInfo(blockType, FindTypeDefinition(blockType), FindInterface(blockType), actions, properties);
                //        if (blockInfo.BlockInterfaceType != null && blocks.All(b => b.BlockInterfaceType != blockInfo.BlockInterfaceType))
                //            blocks.Add(blockInfo);
                //    }

                WriteTerminals(blockInfos, targets);
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

        private int _status;

        async void MySession_AfterLoading()
        {
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            await GameThread.SwitchToGameThread();
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            MySandboxGame.Config.ExperimentalMode = true;
            List<(MyCubeBlockDefinition, IMyTerminalBlock)> result = null;
            //var status = this._status;
            //MySandboxGame.Static.Invoke(() =>
            //{
                GrabWhitelist(_commandLine);
                result = await SpawnBlocksForAnalysisAsync(_commandLine);
                this._status++;
            //}, "ExtractorPlugin");
            //while (status == this._status)
            //{
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await GameThread.SwitchToGameThread();
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
            //}

            //status = this._status;
            if (result != null)
            {
                //MySandboxGame.Static.Invoke(() =>
                //{
                    GrabTerminalActions(_commandLine, result);
                //    this._status++;
                //}, "ExtractorPlugin");
            }
            //while (status == this._status)
            //{
            Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await Task.Delay(TimeSpan.FromSeconds(1));
                Debug.WriteLine(Thread.CurrentThread.ManagedThreadId);
                await GameThread.SwitchToGameThread();
            //}
            MySandboxGame.ExitThreadSafe();
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
        }

        IEnumerable<Type> FindBlocks(Assembly gameAssembly, HashSet<AssemblyName> visitedAssemblies = null)
        {
            visitedAssemblies = visitedAssemblies ?? new HashSet<AssemblyName>(new AssemblyNameComparer());
            visitedAssemblies.Add(gameAssembly.GetName());
            var companyAttribute = gameAssembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (companyAttribute?.Company == "Microsoft Corporation" || companyAttribute?.Company == "ProtoBuf.Net.Core")
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

        class AssemblyNameComparer: IEqualityComparer<AssemblyName>
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