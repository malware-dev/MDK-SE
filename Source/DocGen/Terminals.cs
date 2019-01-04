using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DocGen
{
    public class Terminals
    {
        StringBuilder _document;

        public static void Update(string fileName, string output)
        {
            var terminals = Terminals.Load(fileName);
            terminals.Save(output);
        }

        public static Terminals Load(string fileName)
        {
            var document = XDocument.Load(fileName);
            var terminals = new Terminals();
            terminals.Load(document.Element("terminals"));
            return terminals;
        }

        public void Save(string fileName)
        {
            File.WriteAllText(fileName, _document.ToString());
        }

        void Load(XElement root)
        {
            _document = new StringBuilder();
            if (root == null)
                return;
            var blocks = root.Elements("block").OrderBy(block => GetBlockName((string)block.Attribute("type"))).ToArray();
            _document.AppendLine($"## Overview");
            _document.AppendLine("**Note: Terminal actions and properties are for all intents and purposes obsolete since all vanilla block interfaces now contain proper API access to all this information. It is highly recommended you use those for less overhead.**");
            _document.AppendLine();
            foreach (var block in blocks)
            {
                var name = GetBlockName((string)block.Attribute("type"));
                _document.AppendLine($"[{name}](#{name.ToLower()})  ");
            }
            _document.AppendLine();

            foreach (var block in blocks)
            {
                _document.AppendLine($"## {GetBlockName((string)block.Attribute("type"))}");
                _document.AppendLine();
                var elements = block.Elements("action").OrderBy(e => (string)e.Attribute("name")).ToArray();
                if (elements.Length > 0)
                {
                    _document.AppendLine("### Actions");
                    _document.AppendLine();
                    _document.AppendLine("|Name|Description|");
                    _document.AppendLine("|-|-|");
                    foreach (var action in elements)
                        _document.AppendLine($"|{(string)action.Attribute("name")}|{(string)action.Attribute("text")}|");
                    _document.AppendLine();
                }
                elements = block.Elements("property").OrderBy(e => (string)e.Attribute("name")).ToArray();
                if (elements.Length > 0)
                {
                    _document.AppendLine("### Properties");
                    _document.AppendLine();
                    _document.AppendLine("|Name|Type|");
                    _document.AppendLine("|-|-|");
                    foreach (var action in elements)
                        _document.AppendLine($"|{(string)action.Attribute("name")}|{TranslateType((string)action.Attribute("type"))}|");
                    _document.AppendLine();
                }
            }
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
    }
}