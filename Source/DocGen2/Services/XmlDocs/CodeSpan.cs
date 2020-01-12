using System.Threading.Tasks;
using Mal.DocGen2.Services.Markdown;

namespace Mal.DocGen2.Services.XmlDocs
{
    class CodeSpan : Span
    {
        public CodeSpan(string textValue) : base(textValue)
        { }

        public override async Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer)
        {
            context.BeginPreservingWhitespace();
            await writer.WriteAsync("`");
            await base.WriteMarkdown(context, writer);
            await writer.WriteAsync("`");
            context.EndPreservingWhitespace();
        }
    }
}