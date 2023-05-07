using Malware.MDKServices;
using Microsoft.VisualStudio.Shell;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MDK.Views.ProjectHealth.Fixes
{
    class BadDotNetVersionFix: Fix
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";
        static readonly XName PropertyGroup = XName.Get("PropertyGroup", Xmlns);
        static readonly XName TargetFrameworkVersion = XName.Get("TargetFrameworkVersion", Xmlns);

        public BadDotNetVersionFix(): base(2000, HealthCode.BadDotNetVersion) { }

        public override async Task ApplyAsync(HealthAnalysis analysis, FixStatus status)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            status.Description = "Specifying .NET 4.8";
            var document = XDocument.Load(analysis.FileName);
            var targetFrameworkElement = document.Root?.Elements(PropertyGroup).Select(e => e.Element(TargetFrameworkVersion)).FirstOrDefault();
            if (targetFrameworkElement?.Value.Trim() != "v4.8")
                targetFrameworkElement.Value = "v4.8";
            {
                var relativeGroup = document.Root.Elements(PropertyGroup).LastOrDefault();
                if (relativeGroup != null)
                    relativeGroup.AddAfterSelf(new XElement(PropertyGroup, new XElement(TargetFrameworkVersion, "v4.8")));
                else
                    document.Root.Add(new XElement(PropertyGroup, new XElement(TargetFrameworkVersion, "v4.8")));
            }
            document.Save(analysis.FileName);
        }
    }
}