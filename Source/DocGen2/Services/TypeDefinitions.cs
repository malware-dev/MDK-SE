using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Mal.DocGen2.Services
{
    public class TypeDefinitions
    {
        public static async Task UpdateAsync(string path, string output, string gameBinaryPath)
        {
            var typeDefinitions = await LoadAsync(path);
            typeDefinitions.Generate(path, output);
        }

        public static async Task<TypeDefinitions> LoadAsync(string path)
        {
            var def = new TypeDefinitions();
            var terminals = Terminals.Load(Path.Combine(Environment.CurrentDirectory, "terminal.cache"));
            var text = new SpaceEngineersText();
            await Task.Run(() =>
            {
                var sbcFiles = Directory.EnumerateFiles(path, "*.sbc", SearchOption.AllDirectories);
                Parallel.ForEach(sbcFiles, f => def.Search(f, terminals, text));
            });
            return def;
        }

        readonly List<Definition> _definitions = new List<Definition>();

        TypeDefinitions()
        {
            Definitions = new ReadOnlyCollection<Definition>(_definitions);
        }

        public ReadOnlyCollection<Definition> Definitions { get; }

        void Generate(string path, string output)
        {
            var document = new StringBuilder();
            var blocks = _definitions.Where(d => d.Group == "Blocks").ToList();
            document.AppendLine("## Blocks").AppendLine();

            foreach (var item in blocks.OrderBy(g => g.DisplayName).GroupBy(g => g.DisplayName))
            {
                var type = item.FirstOrDefault().TypeName;
                if (type != null)
                {
                    var i = type.LastIndexOf('.');
                    var typeDisplayName = type.Substring(i + 1);
                    var link = $"{type}";
                    document.Append($"<a name=\"{Uri.EscapeUriString($"blocks-{item.Key}")}\">**").Append(item.Key).Append("**</a> ([").Append(typeDisplayName).Append("](").Append(link).AppendLine("))  ");
                }
                else
                    document.Append($"<a name=\"{Uri.EscapeUriString($"blocks-{item.Key}")}\">**").Append(item.Key).AppendLine("**</a>  ");

                foreach (var subgroup in item.OrderBy(g => g.Size))
                {
                    document.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;")
                        .Append(subgroup.Size).Append(": `").Append(subgroup).AppendLine("`  ");
                }

                document.AppendLine("  ");
            }

            var other = _definitions.Where(d => d.Group != "Blocks").ToList();
            foreach (var group in other.GroupBy(g => g.Group).OrderBy(g => g.Key))
            {
                document.Append("## ").Append(group.Key).AppendLine("  ");
                foreach (var item in group.OrderBy(g => g.DisplayName))
                {
                    document.Append($"**<a name=\"{Uri.EscapeUriString($"{group.Key}-{item.DisplayName}")}\">").Append(item.DisplayName).AppendLine("</a>**  ");
                    document.Append("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;`").Append(item).AppendLine("`  ").AppendLine("  ");
                }

                document.AppendLine();
            }

            File.WriteAllText(output, document.ToString());
        }

        public void Search(string fileName, Terminals terminals, SpaceEngineersText text)
        {
            if (!File.Exists(fileName))
                return;
            XDocument document;
            try
            {
                document = XDocument.Load(fileName);
            }
            catch (Exception)
            {
                // Ignore
                return;
            }

            var blockDefinitions = document.XPathSelectElements(@"/Definitions/CubeBlocks/Definition");
            foreach (var blockDefinition in blockDefinitions)
            {
                var isPublic = (bool?)blockDefinition.Element("Public") ?? true;
                if (!isPublic)
                    continue;
                var idElement = blockDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                var displayName = text.Get((string)blockDefinition.Element("DisplayName"));
                var blockInfo = terminals.Blocks.FirstOrDefault(b => b.TypeId == typeId);
                var size = string.Equals((string)blockDefinition.Element("CubeSize"), "Small", StringComparison.OrdinalIgnoreCase) ? "Small Grid" : "Large Grid";
                //if (blockInfo == null)
                //    continue;

                lock (_definitions)
                {
                    _definitions.Add(new Definition("Blocks", size, displayName, blockInfo?.BlockInterfaceType, typeId, subtypeId));
                }
            }

            var componentDefinitions = document.XPathSelectElements(@"/Definitions/Components/Component");
            foreach (var componentDefinition in componentDefinitions)
            {
                var isPublic = (bool?)componentDefinition.Element("Public") ?? true;
                if (!isPublic)
                    continue;
                var canSpawnFromScreen = (bool?)componentDefinition.Element("CanSpawnFromScreen") ?? true;
                if (!canSpawnFromScreen)
                    continue;
                var idElement = componentDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                var displayName = text.Get((string)componentDefinition.Element("DisplayName"));
                lock (_definitions)
                {
                    _definitions.Add(new Definition("Components", null, displayName, null, typeId, subtypeId));
                }
            }

            var gasDefinitions = document.XPathSelectElements(@"/Definitions/GasProperties/Gas");
            foreach (var gasDefinition in gasDefinitions)
            {
                var isPublic = (bool?)gasDefinition.Element("Public") ?? true;
                if (!isPublic)
                    continue;
                var idElement = gasDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                lock (_definitions)
                {
                    _definitions.Add(new Definition("Gas", null, subtypeId, null, typeId, subtypeId));
                }
            }

            var physicalItemDefinitions = document.XPathSelectElements(@"/Definitions/PhysicalItems/PhysicalItem");
            foreach (var physicalItemDefinition in physicalItemDefinitions)
            {
                var isPublic = (bool?)physicalItemDefinition.Element("Public") ?? true;
                if (!isPublic)
                    continue;
                var canSpawnFromScreen = (bool?)physicalItemDefinition.Element("CanSpawnFromScreen") ?? true;
                if (!canSpawnFromScreen)
                    continue;
                var idElement = physicalItemDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                var displayName = text.Get((string)physicalItemDefinition.Element("DisplayName"));
                lock (_definitions)
                {
                    _definitions.Add(new Definition(GroupOf(typeId), null, displayName, null, typeId, subtypeId));
                }
            }

            var ammoMagazineDefinitions = document.XPathSelectElements(@"/Definitions/AmmoMagazines/AmmoMagazine");
            foreach (var ammoMagazineDefinition in ammoMagazineDefinitions)
            {
                var idElement = ammoMagazineDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                var displayName = text.Get((string)ammoMagazineDefinition.Element("DisplayName"));
                lock (_definitions)
                {
                    _definitions.Add(new Definition(GroupOf(typeId), null, displayName, null, typeId, subtypeId));
                }
            }

            var blueprintDefinitions = document.XPathSelectElements(@"/Definitions/Blueprints/Blueprint");
            foreach (var blueprintDefinition in blueprintDefinitions)
            {
                var idElement = blueprintDefinition.Element("Id");
                var typeId = (string)idElement?.Element("TypeId") ?? (string)idElement?.Attribute("Type");
                var subtypeId = (string)idElement?.Element("SubtypeId") ?? (string)idElement?.Attribute("Typeid");
                var displayName = text.Get((string)blueprintDefinition.Element("DisplayName"));
                lock (_definitions)
                {
                    _definitions.Add(new Definition("Blueprints", null, displayName, null, typeId, subtypeId));
                }
            }
        }

        string GroupOf(string typeId)
        {
            switch (typeId)
            {
                case "Ore": return "Ores";
                case "Ingot": return "Ingots";
                case "OxygenContainerObject":
                case "GasContainerObject":
                case "PhysicalGunObject": return "Tools";
                case "Blueprint": return "Blueprints";
                case "AmmoMagazine": return "Ammo Magazines";
                default: return "Other";
            }
        }


        public readonly struct Definition
        {
            public readonly string Size;
            public readonly string DisplayName;
            public readonly string Group;
            public readonly string TypeName;
            public readonly string TypeId;
            public readonly string SubtypeId;

            public Definition(string group, string size, string displayName, string typeName, string typeId, string subtypeId)
            {
                Size = size;
                DisplayName = displayName;
                Group = group;
                TypeName = typeName;
                TypeId = typeId;
                SubtypeId = subtypeId;
            }

            public override string ToString()
            {
                return TypeId.StartsWith("MyObjectBuilder_") ? $"{TypeId}/{SubtypeId}" : $"MyObjectBuilder_{TypeId}/{SubtypeId}";
            }
        }
    }
}