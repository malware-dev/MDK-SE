using System.Threading.Tasks;
using Mal.DocGen2.Services.Markdown;

namespace Mal.DocGen2.Services.XmlDocs
{
    abstract class XmlDocNode
    {
        public abstract Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer);
    }
}