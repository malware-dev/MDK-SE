using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.XmlDocs
{
    class Span : XmlDocNode
    {
        public Span(string textValue) => TextValue = textValue;

        public string TextValue { get; }

        public override string ToString() => TextValue;

        public override async Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer) => await writer.WriteAsync(context.ShouldPreserveWhitespace ? TextValue : MarkdownInline.Normalize(TextValue));
    }
}