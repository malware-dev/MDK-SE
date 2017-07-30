using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

var namespaceName = "MDK";
var manifest = XDocument.Load(Path.Combine(Path.GetDirectoryName(Context.ProjectFilePath), "source.extension.vsixmanifest"));
var identity = manifest
    .Element(XName.Get("PackageManifest", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Metadata", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Identity", "http://schemas.microsoft.com/developer/vsx-schema/2011"));
var version = (string)identity.Attribute("Version");

var gameAssemblies = new List<string>();
var utilityAssemblies = new List<string>();
var gameFiles = new List<string>();
var utilityFiles = new List<string>();
var projectTemplate = XDocument.Load(Path.Combine(Path.GetDirectoryName(Context.ProjectFilePath), "..\\IngameScriptTemplate\\ProjectTemplate.csproj"));
var xmlns = new XmlNamespaceManager(new NameTable());
xmlns.AddNamespace("ms", Xmlns);
foreach (var element in projectTemplate.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:Reference", xmlns))
{
    var hintPath = (string)element.Element(XName.Get("HintPath", Xmlns));
    if (hintPath?.StartsWith("$mdkgamebinpath$\\") ?? false)
        gameAssemblies.Add($"\"{(string)element.Attribute("Include")}\"");
    if (hintPath?.StartsWith("$mdkinstallpath$\\") ?? false)
        utilityAssemblies.Add($"\"{(string)element.Attribute("Include")}\"");
}
foreach (var element in projectTemplate.XPathSelectElements("/ms:Project/ms:ItemGroup/ms:*", xmlns))
{
    var include = (string)element.Attribute("Include");
    if (include?.StartsWith("$mdkgamebinpath$\\") ?? false)
        gameFiles.Add($"\"{include.Substring(16).Replace("\\", "\\\\")}\"");
    if (include?.StartsWith("$mdkinstallpath$\\") ?? false)
        utilityFiles.Add($"\"{include.Substring(16).Replace("\\", "\\\\")}\"");
}

Context.Output.WriteLine($@"using System;
using System.Collections.ObjectModel;

namespace {namespaceName} 
{{
	public partial class MDKPackage 
    {{
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version(""{version}"");

        /// <summary>
        /// A list of the game assemblies referenced by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> GameAssemblyNames = new ReadOnlyCollection<string>(new string[] 
        {{
            {string.Join($",{Environment.NewLine}            ", gameAssemblies)}
        }});

        /// <summary>
        /// A list of the utility assemblies referenced by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> UtilityAssemblyNames = new ReadOnlyCollection<string>(new string[] 
        {{
            {string.Join($",{Environment.NewLine}            ", utilityAssemblies)}
        }});

        /// <summary>
        /// A list of the game files included by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> GameFiles = new ReadOnlyCollection<string>(new string[] 
        {{
            {string.Join($",{Environment.NewLine}            ", gameFiles)}
        }});

        /// <summary>
        /// A list of the utility files included by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> UtilityFiles = new ReadOnlyCollection<string>(new string[] 
        {{
            {string.Join($",{Environment.NewLine}            ", utilityFiles)}
        }});
	}}
}}");
