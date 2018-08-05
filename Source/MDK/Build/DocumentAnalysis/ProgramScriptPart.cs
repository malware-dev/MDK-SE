using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.DocumentAnalysis
{
    /// <summary>
    /// Represents a normal script part, which means types and members contained within the Program.
    /// </summary>
    public class ProgramScriptPart : ScriptPart
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProgramScriptPart"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="partRoot"></param>
        /// <param name="sortWeight"></param>
        public ProgramScriptPart(Document document, ClassDeclarationSyntax partRoot, int? sortWeight) : base(document, partRoot, sortWeight)
        { }

        /// <summary>
        /// Retrieves the leading trivia of this part.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SyntaxTrivia> GetLeadingTrivia() => SkipFirstTriviaLine(((ClassDeclarationSyntax)PartRoot).OpenBraceToken.TrailingTrivia);

        IEnumerable<SyntaxTrivia> SkipFirstTriviaLine(SyntaxTriviaList triviaList)
        {
            var skipCount = FindTriviaSkipCount(triviaList);
            return triviaList.Skip(skipCount);
        }

        static int FindTriviaSkipCount(SyntaxTriviaList triviaList)
        {
            for (var index = 0; index < triviaList.Count; index++)
            {
                var trivia = triviaList[index];
                switch (trivia.Kind())
                {
                    case SyntaxKind.WhitespaceTrivia:
                        continue;
                    case SyntaxKind.EndOfLineTrivia:
                        return index + 1;
                    default:
                        return 0;
                }
            }

            return triviaList.Count;
        }

        /// <summary>
        /// Gets the content of this part.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<MemberDeclarationSyntax> Content()
        {
            // Write general content
            foreach (var node in PartRoot.ChildNodes())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                        yield return (MemberDeclarationSyntax)node;
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the trailing trivia of this part.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SyntaxTrivia> GetTrailingTrivia() => ((ClassDeclarationSyntax)PartRoot).CloseBraceToken.LeadingTrivia;

        /// <inheritdoc />
        public override string GenerateContent()
        {
            var classDeclaration = (ClassDeclarationSyntax)PartRoot;
            var buffer = new StringBuilder();
            foreach (var trivia in GetLeadingTrivia())
                buffer.Append(trivia.ToFullString());
            //// Write opening brace trailing trivia
            //if (classDeclaration.OpenBraceToken.HasTrailingTrivia)
            //{
            //    var trailingTrivia = classDeclaration.OpenBraceToken.TrailingTrivia;

            //    // Skip the whitespace and line the brace itself is on
            //    var i = 0;
            //    while (i < trailingTrivia.Count && trailingTrivia[i].Kind() == SyntaxKind.WhitespaceTrivia)
            //        i++;
            //    if (i < trailingTrivia.Count && trailingTrivia[i].Kind() == SyntaxKind.EndOfLineTrivia)
            //        i++;
            //    for (; i < trailingTrivia.Count; i++)
            //        buffer.Append(trailingTrivia[i].ToFullString());
            //}

            // Write general content
            foreach (var node in PartRoot.ChildNodes())
            {
                switch (node.Kind())
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.EnumDeclaration:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                    case SyntaxKind.MethodDeclaration:
                    case SyntaxKind.OperatorDeclaration:
                    case SyntaxKind.ConversionOperatorDeclaration:
                    case SyntaxKind.ConstructorDeclaration:
                    case SyntaxKind.DestructorDeclaration:
                    case SyntaxKind.PropertyDeclaration:
                    case SyntaxKind.EventDeclaration:
                    case SyntaxKind.IndexerDeclaration:
                        buffer.Append(node.ToFullString());
                        break;
                }
            }

            foreach (var trivia in GetTrailingTrivia())
                buffer.Append(trivia.ToFullString());

            //// Write closing brace opening trivia
            //// Write opening brace trailing trivia
            //if (classDeclaration.CloseBraceToken.HasLeadingTrivia)
            //{
            //    foreach (var trivia in classDeclaration.CloseBraceToken.LeadingTrivia)
            //        buffer.Append(trivia.ToFullString());
            //}

            return buffer.ToString();
        }
    }
}
