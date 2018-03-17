using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build
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
        public IEnumerable<SyntaxTrivia> GetLeadingTrivia() => ((ClassDeclarationSyntax)PartRoot).OpenBraceToken.TrailingTrivia;

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
            // Write opening brace trailing trivia
            if (classDeclaration.OpenBraceToken.HasTrailingTrivia)
            {
                foreach (var trivia in classDeclaration.OpenBraceToken.TrailingTrivia)
                    buffer.Append(trivia.ToFullString());
            }

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

            // Write closing brace opening trivia
            // Write opening brace trailing trivia
            if (classDeclaration.CloseBraceToken.HasLeadingTrivia)
            {
                foreach (var trivia in classDeclaration.CloseBraceToken.LeadingTrivia)
                    buffer.Append(trivia.ToFullString());
            }

            return buffer.ToString();
        }
    }
}
