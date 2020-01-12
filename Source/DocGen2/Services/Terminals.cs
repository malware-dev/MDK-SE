using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mal.DocGen2.Services
{
    public class Terminals
    {
        public static async Task Update(string fileName, string output, Action<string> updateStatusFn)
        {
            updateStatusFn?.Invoke("Loading cache...");
            var terminals = await Task.Run(() => Load(fileName));
            updateStatusFn?.Invoke("Saving document...");
            await Task.Run(() => terminals.Save(output));
            updateStatusFn?.Invoke("Done.");
        }

        public static Terminals Load(string fileName)
        {
            var document = XDocument.Load(fileName);
            var terminals = new Terminals();
            terminals.Load(document.Element("terminals"));
            return terminals;
        }

        readonly List<BlockInfo> _blocks = new List<BlockInfo>();

        Terminals()
        {
            Blocks = new ReadOnlyCollection<BlockInfo>(_blocks);
        }

        public ReadOnlyCollection<BlockInfo> Blocks { get; }

        public void Save(string fileName)
        {
            var document = new StringBuilder();
            var blocks = Blocks.OrderBy(b => GetBlockName(b.BlockInterfaceType)).ToList();
            document.AppendLine("## Overview");
            document.AppendLine("**Note: Terminal actions and properties are for all intents and purposes obsolete since all vanilla block interfaces now contain proper API access to all this information. It is highly recommended you use those for less overhead.**");
            document.AppendLine();

            foreach (var block in blocks)
            {
                var name = GetBlockName(block.BlockInterfaceType);
                document.AppendLine($"[{name}](#{name.ToLower()})  ");
            }

            document.AppendLine();

            foreach (var block in blocks)
            {
                document.AppendLine($"## {GetBlockName(block.BlockInterfaceType)}");
                document.AppendLine();
                var actions = block.Actions.OrderBy(a => a.Name).ToList();
                if (actions.Any())
                {
                    document.AppendLine("### Actions");
                    document.AppendLine();
                    document.AppendLine("|Name|Description|");
                    document.AppendLine("|-|-|");
                    foreach (var action in actions)
                        document.AppendLine($"|{action.Name}|{action.Text}|");
                    document.AppendLine();
                }

                var properties = block.Properties.OrderBy(a => a.Name).ToList();
                if (properties.Any())
                {
                    document.AppendLine("### Properties");
                    document.AppendLine();
                    document.AppendLine("|Name|Type|");
                    document.AppendLine("|-|-|");
                    foreach (var property in properties)
                        document.AppendLine($"|{property.Name}|{TranslateType(property.Type)}|");
                    document.AppendLine();
                }
            }

            File.WriteAllText(fileName, document.ToString());
        }

        void Load(XElement root)
        {
            _blocks.Clear();
            _blocks.AddRange(root.Elements("block").Select(element => new BlockInfo(element)));
        }

        string TranslateType(string name)
        {
            if (name == null)
                return string.Empty;
            switch (name.ToUpper())
            {
                case "BOOLEAN":
                    return "bool";
                case "CHAR":
                    return "char";
                case "SBYTE":
                    return "sbyte";
                case "BYTE":
                    return "byte";
                case "INT16":
                    return "short";
                case "UINT16":
                    return "ushort";
                case "INT32":
                    return "int";
                case "UINT32":
                    return "uint";
                case "INT64":
                    return "long";
                case "UINT64":
                    return "ulong";
                case "SINGLE":
                    return "float";
                case "DOUBLE":
                    return "double";
                case "DECIMAL":
                    return "decimal";
                case "STRING":
                    return "string";
                default:
                    return name;
            }
        }

        string GetBlockName(string name)
        {
            if (name == null)
                return string.Empty;
            var endPt = name.LastIndexOf('.');
            if (endPt >= 0)
                return name.Substring(endPt + 1);
            return name;
        }

        public readonly struct TerminalAction
        {
            public readonly string Name;
            public readonly string Text;

            public TerminalAction(string name, string text)
            {
                Name = name;
                Text = text;
            }
        }

        public readonly struct TerminalProperty
        {
            public readonly string Name;
            public readonly string Type;

            public TerminalProperty(string name, string type)
            {
                Name = name;
                Type = type;
            }
        }

        public class BlockInfo
        {
            public BlockInfo(XElement element)
            {
                TypeId = (string) element.Attribute("typedefinition");
                BlockInterfaceType = (string) element.Attribute("type");
                var actions = new List<TerminalAction>();
                var elements = element.Elements("action");
                foreach (var action in elements)
                    actions.Add(new TerminalAction((string) action.Attribute("name"), (string) action.Attribute("text")));
                var properties = new List<TerminalProperty>();
                elements = element.Elements("property");
                foreach (var property in elements)
                    properties.Add(new TerminalProperty((string) property.Attribute("name"), (string) property.Attribute("type")));
                Actions = new ReadOnlyCollection<TerminalAction>(actions);
                Properties = new ReadOnlyCollection<TerminalProperty>(properties);
            }

            public string TypeId { get; }
            public string BlockInterfaceType { get; }

            public ReadOnlyCollection<TerminalProperty> Properties { get; set; }

            public ReadOnlyCollection<TerminalAction> Actions { get; set; }

            public void Write(TextWriter writer)
            {
                writer.WriteLine(BlockInterfaceType);
                foreach (var action in Actions)
                    writer.WriteLine($"- action {action.Name}");
                foreach (var property in Properties)
                    writer.WriteLine($"- action {property.Name} {DetermineType(property.Type)}");
            }

            string DetermineType(string propertyTypeName)
            {
                return propertyTypeName;
            }

            public XElement ToXElement()
            {
                var root = new XElement("block", new XAttribute("type", BlockInterfaceType ?? ""), new XAttribute("typedefinition", TypeId ?? ""));
                foreach (var action in Actions)
                    root.Add(new XElement("action", new XAttribute("name", action.Name), new XAttribute("text", action.Text)));
                foreach (var property in Properties)
                    root.Add(new XElement("property", new XAttribute("name", property.Name), new XAttribute("type", DetermineType(property.Type))));
                return root;
            }
        }
    }
}