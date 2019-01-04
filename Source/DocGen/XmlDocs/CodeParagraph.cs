using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.XmlDocs
{
    class CodeParagraph : Paragraph
    {
        static readonly string[] NewLines = {"\r\n", "\n", "\r"};

        static string SmartTrim(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            var lines = content.Split(NewLines, StringSplitOptions.None).ToList();
            while (lines.Count > 0 && lines[0].Trim().Length == 0)
                lines.RemoveAt(0);
            while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
                lines.RemoveAt(lines.Count - 1);
            var shortestIndent = GetIndent(lines[0]);
            foreach (var line in lines.Skip(1))
            {
                var indent = GetIndent(line);
                if (indent.Length < shortestIndent.Length)
                    shortestIndent = indent;
            }

            if (shortestIndent.Length > 0)
            {
                for (var i = 0; i < lines.Count; i++)
                    lines[i] = lines[i].Substring(shortestIndent.Length);
            }

            return string.Join(Environment.NewLine, lines);
        }

        static string GetIndent(string s)
        {
            if (s.Length == 0)
                return "";
            var ch = s[0];
            if (ch != ' ' && ch != '\n')
                return "";
            var i = 0;
            while (i < s.Length && s[i] == ch)
                i++;
            return new string(ch, i);
        }

        public CodeParagraph(string content) : base(ParagraphType.Code, new[] {new Span(SmartTrim(content))})
        { }

        public override async Task WriteMarkdown(XmlDocWriteContext context, MarkdownWriter writer)
        {
            context.BeginPreservingWhitespace();
            await writer.WriteLineAsync("```csharp");
            await base.WriteMarkdown(context, writer);
            await writer.WriteLineAsync("```");
            context.EndPreservingWhitespace();
        }
    }
}