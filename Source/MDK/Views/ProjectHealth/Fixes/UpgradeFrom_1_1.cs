using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Malware.MDKServices;
using MDK.Resources;

namespace MDK.Views.ProjectHealth.Fixes
{
    // ReSharper disable once InconsistentNaming
    class UpgradeFrom_1_1
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        public void Upgrade(HealthAnalysis project)
        {
            XDocument document;
            XmlNameTable nameTable;
            using (var streamReader = File.OpenText(project.FileName))
            {
                var readerSettings = new XmlReaderSettings
                {
                    IgnoreWhitespace = true
                };

                var xmlReader = XmlReader.Create(streamReader, readerSettings);
                document = XDocument.Load(xmlReader);
                nameTable = xmlReader.NameTable;
                if (nameTable == null)
                    throw new InvalidOperationException(Text.UpgradeFrom1_1_Upgrade_ErrorLoadingProject);
            }

            var nsm = new XmlNamespaceManager(nameTable);
            nsm.AddNamespace("m", Xmlns);

            var assemblies = MDKProjectPaths.DefaultAssemblyReferences;
            var analyzers = MDKProjectPaths.DefaultAnalyzerReferences;

            RemoveOldElements(document, assemblies, analyzers, nsm);
            AddNewElements(document.Root, nsm);

            project.Properties.Save();
            document.Save(project.FileName);

            var oldOptionsFileName = Path.Combine(Path.GetDirectoryName(project.FileName) ?? ".\\", "MDK\\MDK.options");
            if (File.Exists(oldOptionsFileName))
                File.Delete(oldOptionsFileName);
        }

        void AddNewElements(XElement root, XmlNamespaceManager nsm)
        {
            var itemGroup = root.XPathSelectElements("./m:Project/ItemGroup", nsm).LastOrDefault();
            if (itemGroup == null)
            {
                itemGroup = new XElement(XName.Get("ItemGroup", Xmlns));
                root.Add(itemGroup);
            }

            itemGroup.Add(new XElement(XName.Get("AdditionalFiles", Xmlns),
                new XAttribute("Include", "MDK\\MDK.options.props"),
                new XElement(XName.Get("CopyToOutputDirectory", Xmlns), "Always")
            ));
            itemGroup.Add(new XElement(XName.Get("AdditionalFiles", Xmlns),
                new XAttribute("Include", "MDK\\MDK.paths.props"),
                new XElement(XName.Get("CopyToOutputDirectory", Xmlns), "Always")
            ));

            itemGroup = root.XPathSelectElement("./m:Project/ItemGroup[Reference]", nsm);
            if (itemGroup == null)
            {
                root.Add(new XElement(XName.Get("Import", Xmlns), new XAttribute("Project", "MDK/MDK.options.props")));
                root.Add(new XElement(XName.Get("Import", Xmlns), new XAttribute("Project", "MDK/MDK.paths.props")));
            }
            else
            {
                itemGroup.AddAfterSelf(new XElement(XName.Get("Import", Xmlns), new XAttribute("Project", "MDK/MDK.paths.props")));
                itemGroup.AddAfterSelf(new XElement(XName.Get("Import", Xmlns), new XAttribute("Project", "MDK/MDK.options.props")));
            }

            var afterBuildTarget = root.XPathSelectElement("./m:Project/Target[@Name='AfterBuild']", nsm);
            if (afterBuildTarget == null)
            {
                afterBuildTarget = new XElement(XName.Get("Target", Xmlns), new XAttribute("Name", "AfterBuild"));
                root.Add(afterBuildTarget);
            }

            afterBuildTarget.Add(new XElement(XName.Get("Copy", Xmlns),
                new XAttribute("SourceFiles", "MDK\\MDK.options.props"),
                new XAttribute("DestinationFolder", "$(TargetDir)\\MDK")
            ));
            afterBuildTarget.Add(new XElement(XName.Get("Copy", Xmlns),
                new XAttribute("SourceFiles", "MDK\\MDK.paths.props"),
                new XAttribute("DestinationFolder", "$(TargetDir)\\MDK")
            ));
        }

        void RemoveOldElements(XDocument document, ReadOnlyCollection<MDKProjectPaths.AssemblyReference> assemblies, ReadOnlyCollection<MDKProjectPaths.AnalyzerReference> analyzers, XmlNamespaceManager nsm)
        {
            var referenceElements = document.XPathSelectElements("./m:Project/m:ItemGroup/m:Reference", nsm).ToList();
            foreach (var element in referenceElements)
            {
                var include = (string)element.Attribute("Include");
                if (assemblies.Any(a => a.Include == include))
                    element.Remove();
            }

            var analyzerElements = document.XPathSelectElements("./m:Project/m:ItemGroup/m:Analyzer", nsm).ToList();
            foreach (var element in analyzerElements)
            {
                var fileName = Path.GetFileName((string)element.Attribute("Include"));
                if (analyzers.Any(a => string.Equals(Path.GetFileName(a.Include), fileName, StringComparison.CurrentCultureIgnoreCase)))
                    element.Remove();
            }

            var optionsElements = document.XPathSelectElements("./m:Project/m:ItemGroup/m:AdditionalFiles", nsm).ToList();
            foreach (var element in optionsElements)
            {
                var include = (string)element.Attribute("Include");
                if (string.Equals(include, "MDK\\MDK.options", StringComparison.CurrentCultureIgnoreCase))
                    element.Remove();
            }

            var copyElements = document.XPathSelectElements("./m:Project/m:Target[@Name='AfterBuild']/m:Copy", nsm).ToList();
            foreach (var element in copyElements)
            {
                var sourceFiles = (string)element.Attribute("SourceFiles");
                var destinationFolder = (string)element.Attribute("DestinationFolder");
                if (string.Equals(sourceFiles, "MDK\\MDK.options", StringComparison.CurrentCultureIgnoreCase) && string.Equals(destinationFolder, "$(TargetDir)\\MDK", StringComparison.CurrentCultureIgnoreCase))
                    element.Remove();
            }
        }
    }
}
