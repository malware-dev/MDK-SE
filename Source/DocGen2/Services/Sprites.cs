using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using DirectXTexNet;
using Mal.DocGen2.Services.Markdown;
using Image = System.Drawing.Image;

namespace Mal.DocGen2.Services
{
    public class Sprites
    {
        private static readonly HashSet<string> SpriteDefinitionTypes = new HashSet<string>(new[] {"MyObjectBuilder_PhysicalItemDefinition", "MyObjectBuilder_ToolItemDefinition", "MyObjectBuilder_WeaponItemDefinition", "MyObjectBuilder_OxygenContainerDefinition", "MyObjectBuilder_ConsumableItemDefinition", "MyObjectBuilder_DatapadDefinition", "MyObjectBuilder_PackageDefinition", "MyObjectBuilder_FactionIconsDefinition"});

        public static async Task UpdateAsync(string path, string output, string gameBinaryPath)
        {
            var sprites = await LoadAsync(path);
            await sprites.GenerateAsync(path, output);
        }

        public static async Task<Sprites> LoadAsync(string path)
        {
            var sprites = new Sprites();

            var allFiles = Directory.GetFiles(path, "*.sbc", SearchOption.AllDirectories);

            await Task.WhenAll(allFiles.Select(fileName => Task.Run(() => Load(fileName, sprites._icons, path))));

            return sprites;
        }

        private static void Load(string fileName, List<SpriteDef> icons, string path)
        {
            using (var stream = File.OpenRead(fileName))
            {
                if (IsGZipFile(stream)) return;

                var document = XDocument.Load(stream, LoadOptions.SetBaseUri);

                var definitions = document.XPathSelectElements("/Definitions/Definition");
                foreach (var definition in definitions) LoadIconsFromDefinition(definition, icons, path);

                var textureDefinitions = document.XPathSelectElements("/Definitions/LCDTextures/LCDTextureDefinition");
                foreach (var definition in textureDefinitions) LoadIconsFromLCDTextureDefinition(definition, icons, path);

                var physicalItemDefinitions = document.XPathSelectElements("/Definitions/PhysicalItems/PhysicalItem");
                foreach (var definition in physicalItemDefinitions) LoadIconFromPhysicalItem(definition, icons, path);

                var ammoMagazines = document.XPathSelectElements("/Definitions/AmmoMagazines/AmmoMagazine");
                foreach (var definition in ammoMagazines) LoadIconFromPhysicalItem(definition, icons, path);

                var components = document.XPathSelectElements("//Definitions/Components/Component");
                foreach (var definition in components) LoadIconFromPhysicalItem(definition, icons, path);
            }
        }

        private static void LoadIconFromPhysicalItem(XElement definition, List<SpriteDef> icons, string path)
        {
            var idElement = definition.Element("Id");
            if (idElement == null)
                return;
            var typeId = idElement.Attribute("Type")?.Value ?? idElement?.Element("TypeId")?.Value;
            if (typeId == null)
                return;
            var subtypeId = idElement.Attribute("Subtype")?.Value ?? idElement?.Element("SubtypeId")?.Value;
            if (subtypeId == null)
                return;
            lock (icons)
            {
                foreach (var icon in definition.Elements("Icon").Select(i => i.Value))
                    icons.Add(new SpriteDef($"MyObjectBuilder_{typeId}/{subtypeId}", Path.GetFullPath(Path.Combine(path, "..", icon))));
            }
        }

        private static void LoadIconsFromLCDTextureDefinition(XElement definition, List<SpriteDef> icons, string path)
        {
            var idElement = definition.Element("Id");
            if (idElement == null)
                return;
            var id = idElement.Attribute("Subtype")?.Value ?? idElement?.Element("SubtypeId")?.Value;
            if (id == null)
                return;
            var icon = definition.Element("SpritePath")?.Value;
            if (icon != null)
            {
                lock (icons)
                {
                    icons.Add(new SpriteDef(id, Path.GetFullPath(Path.Combine(path, "..", icon))));
                }
            }
        }

        private static void LoadIconsFromDefinition(XElement definition, List<SpriteDef> icons, string path)
        {
            var attr = definition.Attributes().FirstOrDefault(IsXmlTypeAttribute);
            if (attr == null)
                return;
            if (SpriteDefinitionTypes.Contains(attr.Value))
            {
                lock (icons)
                {
                    foreach (var icon in definition.Elements("Icon").Select(i => i.Value))
                        icons.Add(new SpriteDef(icon, Path.GetFullPath(Path.Combine(path, "..", icon))));
                }
            }
        }

        private static bool IsGZipFile(FileStream stream)
        {
            var binaryReader = new BinaryReader(stream);
            var magicBytes = binaryReader.ReadBytes(2);
            stream.Position = 0;
            if (magicBytes[0] == 0x1f && magicBytes[1] == 0x8b) return true;

            return false;
        }

        private static bool IsXmlTypeAttribute(XAttribute arg)
        {
            return arg.Name.NamespaceName == "http://www.w3.org/2001/XMLSchema-instance" && arg.Name.LocalName == "type";
        }

        private readonly List<SpriteDef> _icons = new List<SpriteDef>();

        private async Task GenerateAsync(string path, string output)
        {
            var loader = new TextureLoader();
            var dir = Path.GetDirectoryName(output) ?? ".\\";
            using (var file = File.CreateText(output))
            {
                var writer = new MarkdownWriter(file);
                await writer.WriteLineAsync("All images are copyright &copy; Keen Software House.");
                await writer.WriteRulerAsync();

                await writer.BeginParagraphAsync();
                await writer.WriteAsync("See ");
                await writer.WriteLinkAsync("Whiplash' nice little tool", "https://gitlab.com/whiplash141/spritebuilder");
                await writer.WriteLineAsync(" for visually designing sprites and generating the code to display them.");
                await writer.EndParagraphAsync();

                await writer.BeginTableAsync("Id", "Size", "Thumbnail");
                int n = 1;
                foreach (var sprite in _icons.OrderBy(i => i.Id))
                {
                    await writer.BeginTableCellAsync();
                    await writer.WriteAsync(sprite.Id);
                    await writer.EndTableCellAsync();

                    var texture = loader.LoadTextureScratch(sprite.Path);
                    if (texture != null)
                    {
                        var image0 = texture.GetImage(0);
                        await writer.BeginTableCellAsync();
                        await writer.WriteAsync($"{image0.Width}x{image0.Height}");
                        await writer.EndTableCellAsync();

                        var hScale = 64.0 / image0.Width;
                        var vScale = 64.0 / image0.Height;
                        var scale = Math.Min(hScale, vScale);
                        if (scale < 1.0)
                        {
                            var width = (int)(image0.Width * scale);
                            var height = (int)(image0.Height * scale);
                            var thumbnail = texture.Resize(width, height, TEX_FILTER_FLAGS.CUBIC);
                            texture.Dispose();
                            texture = thumbnail;
                        }

                        var thumbnailFile = $@"images\spritethumb_{n}.jpg";
                        n++;
                        var thumbnailPath = Path.Combine(dir, thumbnailFile);
                        texture.SaveToJPGFile(0, 1, thumbnailPath);

                        await writer.BeginTableCellAsync();
                        await writer.WriteImageLinkAsync(sprite.Id, thumbnailFile.Replace("\\", "/"));
                        await writer.EndTableCellAsync();
                        texture.Dispose();
                    }
                    else
                    {
                        await writer.BeginTableCellAsync();
                        await writer.WriteAsync("?x?");
                        await writer.EndTableCellAsync();

                        await writer.BeginTableCellAsync();
                        await writer.WriteAsync("Sprite Not Found! Bad Definition?");
                        await writer.EndTableCellAsync();
                    }
                }

                await writer.EndTableAsync();
            }
        }

        private readonly struct SpriteDef
        {
            public readonly string Id;
            public readonly string Path;

            public SpriteDef(string id, string path)
            {
                Id = id;
                Path = path;
            }

            public override string ToString()
            {
                return Id;
            }
        }
    }
}