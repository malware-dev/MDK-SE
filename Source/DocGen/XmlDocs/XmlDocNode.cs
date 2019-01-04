using System.IO;
using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.XmlDocs
{
    abstract class XmlDocNode
    {
        public abstract Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer);
    }
}