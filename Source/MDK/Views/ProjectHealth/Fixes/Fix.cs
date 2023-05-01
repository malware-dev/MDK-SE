using Malware.MDKServices;
using MDK.Resources;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace MDK.Views.ProjectHealth.Fixes
{
    abstract class Fix
    {
        const string Xmlns = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        ///     Create a fix that is always applied. See <seealso cref="Fix(int, HealthCode, bool)" />
        ///     to make a healthcode violation specific fix.
        /// </summary>
        /// <param name="sortIndex"></param>
        /// <param name="needsLoadedProject"></param>
        protected Fix(int sortIndex, bool needsLoadedProject = false): this(sortIndex, HealthCode.Healthy, needsLoadedProject) { }

        /// <summary>
        ///     Create a fix that is applied when a specific healthcode violation is detected.
        ///     See <seealso cref="Fix(int, bool)" /> to make a fix that is always applied.
        /// </summary>
        /// <param name="sortIndex"></param>
        /// <param name="code"></param>
        /// <param name="needsLoadedProject"></param>
        protected Fix(int sortIndex, HealthCode code, bool needsLoadedProject = false)
        {
            SortIndex = sortIndex;
            Code = code;
            NeedsLoadedProject = needsLoadedProject;
        }

        public int SortIndex { get; }
        public HealthCode? Code { get; }
        public bool NeedsLoadedProject { get; }

        public abstract Task ApplyAsync(HealthAnalysis analysis, FixStatus status);

        protected void Include(HealthAnalysis analysis, string fileName)
        {
            XDocument document;
            XmlNameTable nameTable;
            using (var streamReader = File.OpenText(analysis.FileName))
            {
                var readerSettings = new XmlReaderSettings { IgnoreWhitespace = true };

                var xmlReader = XmlReader.Create(streamReader, readerSettings);
                document = XDocument.Load(xmlReader);
                nameTable = xmlReader.NameTable;
                if (nameTable == null)
                    throw new InvalidOperationException(Text.UpgradeFrom1_1_Upgrade_ErrorLoadingProject);
            }

            var nsm = new XmlNamespaceManager(nameTable);
            nsm.AddNamespace("m", Xmlns);

            var projectBasePath = Path.GetDirectoryName(Path.GetFullPath(analysis.FileName)) ?? ".";
            if (!projectBasePath.EndsWith("\\"))
                projectBasePath += "\\";
            fileName = Path.Combine(projectBasePath, fileName);
            if (fileName.StartsWith(projectBasePath))
                fileName = fileName.Substring(projectBasePath.Length);
            else
                fileName = Path.GetFullPath(fileName);

            var existingElement = document.Descendants(XName.Get("AdditionalFiles", Xmlns))
                .FirstOrDefault(e => string.Equals((string)e.Attribute("Include"), fileName, StringComparison.CurrentCultureIgnoreCase));
            if (existingElement != null)
                return;

            var itemGroupElement = document.Descendants(XName.Get("ItemGroup", Xmlns)).LastOrDefault();
            if (itemGroupElement == null)
            {
                itemGroupElement = new XElement(XName.Get("ItemGroup", Xmlns));
                document.Root?.Add(itemGroupElement);
            }

            var fileElement = new XElement(XName.Get("AdditionalFiles", Xmlns),
                new XAttribute("Include", fileName),
                new XElement(XName.Get("CopyToOutputDirectory", Xmlns), new XText("Always"))
            );
            itemGroupElement.Add(fileElement);
            document.Save(analysis.FileName);
        }

        public virtual bool IsApplicableTo(HealthAnalysis project) => Code == HealthCode.Healthy || project.Problems.Any(p => p.Code == Code);
    }
}