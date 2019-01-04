using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.XmlDocs
{
    class Paragraph : XmlDocNode
    {
        public Paragraph(ParagraphType type, IEnumerable<XmlDocNode> content)
        {
            Type = type;
            Content = new ReadOnlyCollection<XmlDocNode>(content?.Where(n => n != null).ToList() ?? new List<XmlDocNode>());
        }

        public ParagraphType Type { get; }

        public ReadOnlyCollection<XmlDocNode> Content { get; }

        public override async Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer)
        {
            await writer.BeginParagraphAsync();
            foreach (var node in Content)
                await node.WriteMarkdown(context, writer);
            await writer.EndParagraphAsync();
        }
    }
}