using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK
{
    static class SyntaxDumperExtensions
    {
        class StringBuilderWriter : TextWriter
        {
            readonly StringBuilder _stringBuilder;

            public StringBuilderWriter(StringBuilder stringBuilder)
            {
                _stringBuilder = stringBuilder;
            }

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value) => _stringBuilder.Append(value);
        }

        public static void Dump(this SyntaxNode node)
        {
            var writer = new StringWriter();
            node.Dump(writer);
            writer.Flush();
            Debug.Write(writer.ToString());
        }

        public static void Dump(this SyntaxNode node, [NotNull] TextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var dumper = new SyntaxDumper(writer);
            dumper.Visit(node);
        }

        public static void Dump(this SyntaxNode node, [NotNull] StringBuilder stringBuilder)
        {
            if (stringBuilder == null)
                throw new ArgumentNullException(nameof(stringBuilder));
            var writer = new StringBuilderWriter(stringBuilder);
            node.Dump(writer);
            writer.Flush();
        }
    }

    class SyntaxDumper : CSharpSyntaxWalker
    {
        static bool IsInterestingTrivia(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.DocumentationCommentExteriorTrivia:
                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                case SyntaxKind.DisabledTextTrivia:
                case SyntaxKind.PreprocessingMessageTrivia:
                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElifDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                case SyntaxKind.EndIfDirectiveTrivia:
                case SyntaxKind.RegionDirectiveTrivia:
                case SyntaxKind.EndRegionDirectiveTrivia:
                case SyntaxKind.DefineDirectiveTrivia:
                case SyntaxKind.UndefDirectiveTrivia:
                case SyntaxKind.ErrorDirectiveTrivia:
                case SyntaxKind.WarningDirectiveTrivia:
                case SyntaxKind.LineDirectiveTrivia:
                case SyntaxKind.PragmaWarningDirectiveTrivia:
                case SyntaxKind.PragmaChecksumDirectiveTrivia:
                case SyntaxKind.ReferenceDirectiveTrivia:
                case SyntaxKind.BadDirectiveTrivia:
                case SyntaxKind.SkippedTokensTrivia:
                case SyntaxKind.ConflictMarkerTrivia:
                case SyntaxKind.XmlElement:
                case SyntaxKind.XmlElementStartTag:
                case SyntaxKind.XmlElementEndTag:
                case SyntaxKind.XmlEmptyElement:
                case SyntaxKind.XmlTextAttribute:
                case SyntaxKind.XmlCrefAttribute:
                case SyntaxKind.XmlNameAttribute:
                case SyntaxKind.XmlName:
                case SyntaxKind.XmlPrefix:
                case SyntaxKind.XmlText:
                case SyntaxKind.XmlCDataSection:
                case SyntaxKind.XmlComment:
                case SyntaxKind.XmlProcessingInstruction:
                case SyntaxKind.UsingDirective:
                case SyntaxKind.ExternAliasDirective:
                case SyntaxKind.ShebangDirectiveTrivia:
                case SyntaxKind.LoadDirectiveTrivia:
                    return true;
                default:
                    return false;
            }
        }

        readonly TextWriter _writer;
        int _indent;

        public SyntaxDumper(TextWriter writer) : base(SyntaxWalkerDepth.Node)
        {
            _writer = writer;
        }

        public override void Visit(SyntaxNode node)
        {
            var indent = new string(' ', _indent * 2);
            _writer.WriteLine($"{indent}{{");
            foreach (var annotation in node.GetAnnotations("MDK"))
                _writer.WriteLine($"{indent}  [{annotation.Kind} {annotation.Data}]");
            _writer.WriteLine($"{indent}  {node.Kind()}");
            _indent++;
            if (node.HasLeadingTrivia)
            {
                _writer.WriteLine($"{indent}  [>>");
                var trivia = node.GetLeadingTrivia();
                foreach (var item in trivia)
                {
                    VisitTrivia(item);
                }
                _writer.WriteLine($"{indent}  >>]");
            }
            else if (node.HasTrailingTrivia)
            {
                _writer.WriteLine($"{indent}  [<<");
                var trivia = node.GetTrailingTrivia();
                foreach (var item in trivia)
                {
                    VisitTrivia(item);
                }
                _writer.WriteLine($"{indent}  <<]");
            }
            base.Visit(node);
            _indent--;
            _writer.WriteLine($"{indent}}}");
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            var indent = new string(' ', _indent * 4);
            if (IsInterestingTrivia(trivia.Kind()))
            {
                foreach (var annotation in trivia.GetAnnotations("MDK"))
                    _writer.WriteLine($"{indent}[{annotation.Kind} {annotation.Data}]");
                _writer.WriteLine($"{indent}[>>");
                _writer.WriteLine($"{indent}  {trivia.Kind()}");
                _indent++;
            }
            base.VisitTrivia(trivia);
            if (IsInterestingTrivia(trivia.Kind()))
            {
                _indent--;
                foreach (var annotation in trivia.GetAnnotations("MDK"))
                    _writer.WriteLine($"{indent}[{annotation.Kind} {annotation.Data}]");
                _writer.WriteLine($"{indent}>>]");
            }
        }
    }
}
