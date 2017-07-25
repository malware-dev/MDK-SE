using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK.Build
{
    public class ProgramScriptPart : ScriptPart
    {
        public ProgramScriptPart(Document document, SyntaxNode partRoot) : base(document, partRoot)
        { }

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