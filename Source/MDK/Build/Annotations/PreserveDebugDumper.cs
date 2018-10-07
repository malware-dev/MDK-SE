using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK.Build.Annotations
{
    class PreserveDebugDumper : CSharpSyntaxWalker
    {
        readonly string _fileName;

        public PreserveDebugDumper(string fileName) : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            _fileName = fileName;
            File.WriteAllText(fileName, "");
        }

        public override void VisitToken(SyntaxToken token)
        {
            VisitLeadingTrivia(token);
            if (token.ShouldBePreserved())
                File.AppendAllText(_fileName, $"[{token}]");
            else
                File.AppendAllText(_fileName, token.ToString());
            VisitTrailingTrivia(token);
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            base.VisitTrivia(trivia);
            if (trivia.ShouldBePreserved())
                File.AppendAllText(_fileName, $"[{ReplaceWhitespace(trivia.ToString())}]");
            else
                File.AppendAllText(_fileName, ReplaceWhitespace(trivia.ToString()));
        }

        string ReplaceWhitespace(string input)
        {
            return Regex.Replace(input, @" |\t|\r\n|\n", match =>
            {
                switch (match.Value)
                {
                    case " ":
                        return ".";
                    case "\t":
                        return "->->";
                    case "\r\n":
                        return "\\r\\n\r\n";
                    case "\n":
                        return "\\n\n";
                    default:
                        return match.Value;
                }
            });
        }
    }
}
