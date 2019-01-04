using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.XmlDocs
{
    class TypeRefSpan : Span
    {
        public TypeRefSpan(string textValue) : base(textValue)
        { }

        public override async Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer)
        {
            await writer.WriteAsync(" ");
            var entry = context.ResolveReference(TextValue);
            if (entry.Key == null)
                await writer.WriteAsync(entry.Value ?? TextValue);
            else
                await writer.WriteAsync(MarkdownInline.HRef(entry.Value, entry.Key));
            await writer.WriteAsync(" ");
        }
    }
}