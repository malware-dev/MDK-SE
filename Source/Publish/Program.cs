using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

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

            var releaseType = GetReleaseType();
            if (releaseType == ReleaseType.None)
                return;
            Console.WriteLine();
            // Ugh. So a Visual Studio update made the call above stop working, even after updating the nuget package. Thanks, MS.
            var msbuildExe = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe";
            var c = Directory.GetCurrentDirectory();
            var solutionPath = Path.GetFullPath(args[0]);
            var manifestPath = Path.Combine(Path.GetDirectoryName(solutionPath), @"MDK\source.extension.vsixmanifest");
            var appConfigPath = Path.Combine(Path.GetDirectoryName(solutionPath), @"MDK\other.xml");

            if (releaseType != ReleaseType.SameVersion)
            {
                UpdateManifestVersion(manifestPath, releaseType);
                UpdateAppConfigVersion(appConfigPath, releaseType);
            }

            Build(msbuildExe, solutionPath);
        }

        static ReleaseType GetReleaseType()
        {
            Console.WriteLine("Release type:");
            Console.WriteLine("P: Prerelease");
            Console.WriteLine("B: Bugfix Release");
            Console.WriteLine("F: Feature Release");
            Console.WriteLine("0: Same Version");
            Console.WriteLine("X: Cancel");
            while (true)
            {
                Console.Write("> ");
                var r = Console.ReadLine();
                switch (r?.ToUpper().Trim())
                {
                    case "P":
                        return ReleaseType.Prerelease;
                    case "B":
                        return ReleaseType.BugfixRelease;
                    case "F":
                        return ReleaseType.FeatureRelease;
                    case "0":
                        return ReleaseType.SameVersion;
                    case "X":
                        return ReleaseType.None;
                }
            }
        }

        static void UpdateAppConfigVersion(string appConfigPath, ReleaseType releaseType)
        {
            var document = XDocument.Load(appConfigPath);
            var element = document.XPathSelectElement("/Other/IsPrerelease");
            element.Value = releaseType == ReleaseType.Prerelease ? bool.TrueString : bool.FalseString;
            document.Save(appConfigPath);
        }

        static void UpdateManifestVersion(string manifestPath, ReleaseType releaseType)
        {
            var document = XDocument.Load(manifestPath);
            var xmlns = new XmlNamespaceManager(new NameTable());
            xmlns.AddNamespace("ms", VsxXmlns);
            var identityElement = document.XPathSelectElement("/ms:PackageManifest/ms:Metadata/ms:Identity", xmlns);
            var versionAttribute = identityElement.Attribute("Version");
            var version = new Version((string)versionAttribute);
            Version newVersion;
            switch (releaseType)
            {
                case ReleaseType.Prerelease:
                    newVersion = new Version(version.Major, version.Minor, version.Build + 1);
                    break;
                case ReleaseType.BugfixRelease:
                    newVersion = new Version(version.Major, version.Minor, version.Build + 1);
                    break;
                case ReleaseType.FeatureRelease:
                    newVersion = new Version(version.Major, version.Minor + 1, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(releaseType), releaseType, null);
            }

            versionAttribute.Value = newVersion.ToString();
            document.Save(manifestPath);
        }

        static void Build(string msbuildExe, string solutionPath)
        {
            //var pc = new ProjectCollection();
            //var properties = new Dictionary<string, string>
            //{
            //    ["Configuration"] = "Release",
            //    ["Platform"] = "Any CPU"
            //};
            //var buildRequest = new BuildRequestData(solutionPath, properties, "14.0", new[] { "Build" }, null);
            //var buildResult = BuildManager.DefaultBuildManager.Build(new BuildParameters(pc), buildRequest);

            var process = new Process
            {
                StartInfo =
                {
                    FileName = msbuildExe,
                    Arguments = $"\"{Path.GetFileName(solutionPath)}\" /t:Rebuild /p:Configuration=Release /p:Platform=\"Any Cpu\"",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath),
                    UseShellExecute = false
                }
            };
            process.OutputDataReceived += OnProcessOutputDataReceived;
            process.ErrorDataReceived += OnProcessErrorDataReceived;

            Console.Clear();
            process.Start();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("Build Failed!");
                Console.WriteLine("Press Any Key...");
                Console.ReadKey();
            }
        }

        static void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Write(e.Data);
        }

        static void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.Write(e.Data);
        }

        enum ReleaseType
        {
            None,
            Prerelease,
            BugfixRelease,
            FeatureRelease,
            SameVersion
        }
    }
}