using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MDK.Build.Composers.Minifying
{
    abstract class ProgramRewriter : CSharpSyntaxRewriter
    {
        protected ProgramRewriter(): base(true) { }

        public sealed override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
        {
            var preservedMembers = new List<MemberDeclarationSyntax>();
            foreach (var typeDeclaration in node.Members)
            {
                if (typeDeclaration is ClassDeclarationSyntax classDeclaration && classDeclaration.GetFullName(DeclarationFullNameFlags.WithoutNamespaceName) == "Program")
                {
                    var nodes = typeDeclaration.ChildNodes().OfType<MemberDeclarationSyntax>();
                    var preservedNodes = new List<MemberDeclarationSyntax>();
                    foreach (var childNode in nodes)
                    {
                        if (Visit(childNode) is MemberDeclarationSyntax newNode)
                            preservedNodes.Add(newNode);
                    }

                    preservedMembers.Add(classDeclaration.WithMembers(SyntaxFactory.List(preservedNodes)));
                }
                else
                if (Visit(typeDeclaration) is MemberDeclarationSyntax newNode)
                    preservedMembers.Add(newNode);
            }

            return node.WithMembers(SyntaxFactory.List(preservedMembers));
        }
    }
}