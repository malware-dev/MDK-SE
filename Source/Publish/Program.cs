using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Build.Utilities;

namespace Malware.BuildForPublish
{
    class Program
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/vsx-schema/2011";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No solution specified");
                return;
            }

            var msbuildExe = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            var solutionPath = Path.GetFullPath(args[0]);
            var manifestPath = Path.Combine(Path.GetDirectoryName(solutionPath), @"MDK\source.extension.vsixmanifest");

            UpdateVersion(manifestPath);
            Build(msbuildExe, solutionPath);
        }

        static void UpdateVersion(string manifestPath)
        {
            var document = XDocument.Load(manifestPath);
            var xmlns = new XmlNamespaceManager(new NameTable());
            xmlns.AddNamespace("ms", Xmlns);
            var identityElement = document.XPathSelectElement("/ms:PackageManifest/ms:Metadata/ms:Identity", xmlns);
            var versionAttribute = identityElement.Attribute("Version");
            var version = new Version((string)versionAttribute);
            var newVersion = new Version(version.Major, version.Minor, version.Build + 1);
            versionAttribute.Value = newVersion.ToString();
            document.Save(manifestPath);
        }

        static void Build(string msbuildExe, string solutionPath)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = msbuildExe,
                    Arguments = $"\"{Path.GetFileName(solutionPath)}\" /t:Rebuild /p:Configuration=Release /p:Platform=\"Any Cpu\"",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath)
                }
            };
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Build Failed!");
                Console.WriteLine("Press Any Key...");
                Console.ReadKey();
            }
        }
    }
}