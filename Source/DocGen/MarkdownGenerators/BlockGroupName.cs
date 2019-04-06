using System;
using System.Collections.Generic;
using System.Linq;

namespace DocGen.MarkdownGenerators
{
    struct BlockGroupName
    {
        public static BlockGroupName From(ApiEntry entry)
        {
            if (entry.Member.IsDefined(typeof(ObsoleteAttribute), false))
                return new BlockGroupName("Obsolete", "These types should no longer be used and may be removed in the future. If you're using one of these types, you should replace them as soon as possible.", int.MaxValue);

            var isBlock = ThisAndAncestorsOf(entry).Any(e => e.FullName == "Sandbox.ModAPI.Ingame.IMyTerminalBlock");
            if (isBlock)
            {
                if (entry.InheritorEntries.Count == 0 || entry.Name == "IMyVirtualMass" || entry.Name == "IMyGasTank")
                    return new BlockGroupName("Blocks", "Use these interfaces when you wish to access specific block types.", 0);

                return new BlockGroupName("Block Categories", "Use these interfaces when you wish to access blocks by a specific function or category rather than a specific block type.", 1000);
            }

            if (entry.NamespaceName == "VRage.Game.ModAPI.Ingame.Utilities")
                return new BlockGroupName("Utilities", "Various useful utilities for your scripts", 2000);

            if (entry.NamespaceName == "Sandbox.Game.EntityComponents" || entry.NamespaceName == "VRage.Game.Components")
                return new BlockGroupName("Entity Component Systems", "Gain access to advanced block components", 4000);

            switch (entry.FullName)
            {
                case "Sandbox.ModAPI.Ingame.IMyBlockGroup":
                case "Sandbox.ModAPI.Ingame.IMyGridTerminalSystem":
                case "Sandbox.ModAPI.Ingame.IMyGridProgramRuntimeInfo":
                case "Sandbox.ModAPI.Ingame.UpdateFrequency":
                case "Sandbox.ModAPI.Ingame.UpdateType":
                case "Sandbox.ModAPI.Ingame.MyGridProgram":
                case "VRage.Game.ModAPI.Ingame.IMyCubeGrid":
                case "VRage.Game.ModAPI.Ingame.IMyCubeBlock":
                case "VRage.Game.ModAPI.Ingame.IMySlimBlock":
                    return new BlockGroupName("Grid Program and Terminal System", "Types related to the grid program and grid terminal system", 3000);

                case "VRage.Game.ModAPI.Ingame.MyInventoryItem":
                case "VRage.Game.ModAPI.Ingame.IMyInventory":
                case "VRage.Game.ModAPI.Ingame.MyInventoryItemFilter":
                case "VRage.Game.ModAPI.Ingame.MyItemInfo":
                    return new BlockGroupName("Inventory", "Types related to inventory analysis and management.", 5000);

                case "Sandbox.ModAPI.Ingame.IMyIntergridCommunicationSystem":
                case "Sandbox.ModAPI.Ingame.IMyBroadcastListener":
                case "Sandbox.ModAPI.Ingame.IMyUnicastListener":
                case "Sandbox.ModAPI.Ingame.IMyMessageProvider":
                case "Sandbox.ModAPI.Ingame.TransmissionDistance":
                    return new BlockGroupName("IGC", "Types related to the intergrid communication system.", 5000);

                case "Sandbox.ModAPI.Interfaces.TerminalBlockExtentions":
                case "Sandbox.ModAPI.Interfaces.ITerminalProperty":
                case "Sandbox.ModAPI.Ingame.TerminalPropertyExtensions":
                case "Sandbox.ModAPI.Interfaces.ITerminalAction":
                case "Sandbox.ModAPI.Ingame.TerminalActionParameter":
                case "Sandbox.Game.Gui.TerminalActionExtensions":
                    return new BlockGroupName("Terminal Properties and Actions", "Types related to the terminal properties and actions. You should endeavor to avoid the use of these if possible due to their extra overhead. There are usually proper interface members available instead, which are orders of magnitude faster in use.", 6000);
            }

            if (entry.AssemblyName == "VRage.Math")
                return new BlockGroupName("Math", "Math utilities", 7000);

            return new BlockGroupName("Other", "Currently ungrouped types", 1000000);
        }

        static IEnumerable<ApiEntry> ThisAndAncestorsOf(ApiEntry entry)
        {
            while (entry != null)
            {
                yield return entry;
                foreach (var iface in entry.InheritedEntries)
                    yield return iface;

                entry = entry.BaseEntry;
            }
        }

        public readonly string Description;
        public readonly string Name;
        public readonly int SortOrder;

        public BlockGroupName(string name, string description, int sortOrder)
        {
            Description = description;
            Name = name;
            SortOrder = sortOrder;
        }
    }
}