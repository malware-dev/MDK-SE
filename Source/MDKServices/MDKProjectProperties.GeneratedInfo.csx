using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

const string OutputFile = @"..\Mixin.MDKProjectProperties\MDKProjectProperties.GeneratedInfo.cs"; 
const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

var namespaceName = "Malware.MDKServices";
var manifest = XDocument.Load(Path.Combine(Path.GetDirectoryName(Context.ProjectFilePath), "..\\mdk\\source.extension.vsixmanifest"));
var identity = manifest
    .Element(XName.Get("PackageManifest", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Metadata", "http://schemas.microsoft.com/developer/vsx-schema/2011"))
    .Element(XName.Get("Identity", "http://schemas.microsoft.com/developer/vsx-schema/2011"));
var version = (string)identity.Attribute("Version");

Context.Output[OutputFile].BuildAction = BuildAction.GenerateOnly;
Context.Output[OutputFile].WriteLine($@"using System;

namespace {namespaceName} 
{{
	partial class MDKProjectProperties 
    {{
	    /// <summary>
		/// The current package version this utility assembly targets
		/// </summary>
		public static readonly Version TargetPackageVersion = new Version(""{version}"");
	}}
}}");
