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
        const string VsxXmlns = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        const string SetXmlns = "http://schemas.microsoft.com/VisualStudio/2004/01/settings";

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No solution specified");
                return;
            }

            bool isPrerelease = false;
            Console.Write("Is this to be a prerelease version? Y/N/ESC>");
            while (true)
            {
                var res = Console.ReadKey(true);
                if (res.Key == ConsoleKey.Y)
                {
                    isPrerelease = true;
                    break;
                }
                if (res.Key == ConsoleKey.N)
                {
                    isPrerelease = false;
                    break;
                }
                if (res.Key == ConsoleKey.Escape)
                {
                    return;
                }
            }
            Console.WriteLine();
            //var msbuildExe = ToolLocationHelper.GetPathToBuildToolsFile("msbuild.exe", ToolLocationHelper.CurrentToolsVersion);
            // Ugh. So a Visual Studio update made the call above stop working, even after updating the nuget package. Thanks, MS.
            var msbuildExe = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"; 
            var solutionPath = Path.GetFullPath(args[0]);
            var manifestPath = Path.Combine(Path.GetDirectoryName(solutionPath), @"MDK\source.extension.vsixmanifest");
            var appConfigPath = Path.Combine(Path.GetDirectoryName(solutionPath), @"MDK\other.xml");

            UpdateManifestVersion(manifestPath);
            UpdateAppConfigVersion(appConfigPath, isPrerelease);
            Build(msbuildExe, solutionPath);
        }

        static void UpdateAppConfigVersion(string appConfigPath, bool isPrerelease)
        {
            var document = XDocument.Load(appConfigPath);
            var element = document.XPathSelectElement("/Other/IsPrerelease");
            element.Value = isPrerelease.ToString();
            document.Save(appConfigPath);
        }

        static void UpdateManifestVersion(string manifestPath)
        {
            var document = XDocument.Load(manifestPath);
            var xmlns = new XmlNamespaceManager(new NameTable());
            xmlns.AddNamespace("ms", VsxXmlns);
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