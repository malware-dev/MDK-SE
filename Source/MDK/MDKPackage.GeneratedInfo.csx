using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;

var namespaceName = "MDK";
var manifest = XDocument.Load(Path.Combine(Path.GetDirectoryName(Context.ProjectFilePath), "source.extension.vsixmanifest"));
var identity = manifest
    .Element(XName.Get("PackageManifest", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Metadata", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Identity", "http://schemas.microsoft.com/developer/vsx-schema/2011"));
var version = (string)identity.Attribute("Version");

Context.Output.WriteLine($@"using System;

namespace {namespaceName} {{
	public partial class MDKPackage {{
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version(""{version}"");
	}}
}}")