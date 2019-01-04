using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MDK.Build.Composers.Minifying
{
    class LineWrapper : ProgramRewriter
    {
        LinePosition _lastNewLine;
        bool _isPreservedBlock;

        public LineWrapper() : base(true)
        { }

        public int LineWidth { get; set; } = 120;

        bool IsAtStartOfLine() => _lastNewLine.Character == 0;

        void ClearLineInfo()
        {
            _lastNewLine = LinePosition.Zero;
        }

        int GetCharacterIndexFor(LinePosition linePosition)
        {
            var character = linePosition.Character;
            if (linePosition.Line == _lastNewLine.Line)
                character -= _lastNewLine.Character;
            else
                MoveToStartOfLine(linePosition);

            return character;
        }

        void SetLineshift(LinePosition linePosition)
        {
            _lastNewLine = linePosition;
        }

        void MoveToStartOfLine(LinePosition linePosition)
        {
            _lastNewLine = new LinePosition(linePosition.Line, 0);
        }

        public override SyntaxNode VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            if (node.ShouldBePreserved())
            {
                if (!_isPreservedBlock)
                {
                    _isPreservedBlock = true;
                    //if (!IsAtStartOfLine())
                    //{
                    ClearLineInfo();
                    node = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                    //}
                }

                return node;
            }

            _isPreservedBlock = false;

            var span = node.GetLocation().GetLineSpan();
            var endPosition = GetCharacterIndexFor(span.EndLinePosition);

            if (node.Span.Length < LineWidth && endPosition > LineWidth)
            {
                node = node.WithLeadingTrivia(SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve")));
                SetLineshift(span.EndLinePosition);
            }

            return node;
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            token = base.VisitToken(token);
            if (token.Kind() == SyntaxKind.None)
                return token;

            if (token.ShouldBePreserved())
            {
                if (!_isPreservedBlock)
                {
                    _isPreservedBlock = true;
                    //if (!IsAtStartOfLine())
                    //{
                    ClearLineInfo();
                    token = token.WithLeadingTrivia(token.LeadingTrivia.Insert(0, SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve"))));
                    //}
                }

                return token;
            }

            _isPreservedBlock = false;

            if (token.Kind() == SyntaxKind.None)
                return token;

            var span = token.GetLocation().GetLineSpan();
            var endPosition = GetCharacterIndexFor(span.EndLinePosition);

            if (token.Span.Length < LineWidth && endPosition > LineWidth)
            {
                token = token.WithLeadingTrivia(SyntaxFactory.EndOfLine("\n").WithAdditionalAnnotations(new SyntaxAnnotation("MDK", "preserve")));
                SetLineshift(span.EndLinePosition);
            }

            return token;
        }

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            ClearLineInfo();
            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }
    }
}
