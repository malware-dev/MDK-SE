using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        public ProgramScriptPart(Document document, SyntaxNode partRoot) : base(document, partRoot)
        { }

        /// <inheritdoc />
        public override IEnumerable<SyntaxNode> ContentNodes()
        {
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
                        yield return node;
                        break;
                }
            }
        }
    }
}
