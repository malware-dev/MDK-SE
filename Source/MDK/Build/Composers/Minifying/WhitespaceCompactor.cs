using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Annotations;
using MDK.Build.Solution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MDK.Build.Composers.Minifying
{
    class WhitespaceCompactor : ProgramRewriter
    {
        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            trivia = base.VisitTrivia(trivia);
            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia && trivia.ToString() == "\r\n")
                trivia = trivia.CopyAnnotationsTo(SyntaxFactory.EndOfLine("\n"));

            return trivia;
        }

        public override SyntaxToken VisitToken(SyntaxToken currentToken)
        {
            var previousToken = currentToken.GetPreviousToken();
            currentToken = base.VisitToken(currentToken);

            currentToken = currentToken.WithLeadingTrivia(currentToken.LeadingTrivia.Where(t => t.ShouldBePreserved()));
            currentToken = currentToken.WithTrailingTrivia(currentToken.TrailingTrivia.Where(t => t.ShouldBePreserved()));

            if (currentToken.LeadingTrivia.Sum(t => t.FullSpan.Length) + previousToken.TrailingTrivia.Where(t => t.ShouldBePreserved()).Sum(t => t.FullSpan.Length) == 0)
            {
                if (IsColliding(previousToken.Kind(), currentToken.Kind()))
                    currentToken = currentToken.WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
            }

            return currentToken;
        }
       
        bool IsColliding(SyntaxKind firstKind, SyntaxKind secondKind)
        {
            if (IsWord(firstKind) && IsWord(secondKind))
                return true;
            return false;
        }

        bool IsWord(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.NumericLiteralToken:
                case SyntaxKind.BoolKeyword:
                case SyntaxKind.ByteKeyword:
                case SyntaxKind.SByteKeyword:
                case SyntaxKind.ShortKeyword:
                case SyntaxKind.UShortKeyword:
                case SyntaxKind.IntKeyword:
                case SyntaxKind.UIntKeyword:
                case SyntaxKind.LongKeyword:
                case SyntaxKind.ULongKeyword:
                case SyntaxKind.DoubleKeyword:
                case SyntaxKind.FloatKeyword:
                case SyntaxKind.DecimalKeyword:
                case SyntaxKind.StringKeyword:
                case SyntaxKind.CharKeyword:
                case SyntaxKind.VoidKeyword:
                case SyntaxKind.ObjectKeyword:
                case SyntaxKind.TypeOfKeyword:
                case SyntaxKind.SizeOfKeyword:
                case SyntaxKind.NullKeyword:
                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:
                case SyntaxKind.IfKeyword:
                case SyntaxKind.ElseKeyword:
                case SyntaxKind.WhileKeyword:
                case SyntaxKind.ForKeyword:
                case SyntaxKind.ForEachKeyword:
                case SyntaxKind.DoKeyword:
                case SyntaxKind.SwitchKeyword:
                case SyntaxKind.CaseKeyword:
                case SyntaxKind.DefaultKeyword:
                case SyntaxKind.TryKeyword:
                case SyntaxKind.CatchKeyword:
                case SyntaxKind.FinallyKeyword:
                case SyntaxKind.LockKeyword:
                case SyntaxKind.GotoKeyword:
                case SyntaxKind.BreakKeyword:
                case SyntaxKind.ContinueKeyword:
                case SyntaxKind.ReturnKeyword:
                case SyntaxKind.ThrowKeyword:
                case SyntaxKind.PublicKeyword:
                case SyntaxKind.PrivateKeyword:
                case SyntaxKind.InternalKeyword:
                case SyntaxKind.ProtectedKeyword:
                case SyntaxKind.StaticKeyword:
                case SyntaxKind.ReadOnlyKeyword:
                case SyntaxKind.SealedKeyword:
                case SyntaxKind.ConstKeyword:
                case SyntaxKind.FixedKeyword:
                case SyntaxKind.StackAllocKeyword:
                case SyntaxKind.VolatileKeyword:
                case SyntaxKind.NewKeyword:
                case SyntaxKind.OverrideKeyword:
                case SyntaxKind.AbstractKeyword:
                case SyntaxKind.VirtualKeyword:
                case SyntaxKind.EventKeyword:
                case SyntaxKind.ExternKeyword:
                case SyntaxKind.RefKeyword:
                case SyntaxKind.OutKeyword:
                case SyntaxKind.InKeyword:
                case SyntaxKind.IsKeyword:
                case SyntaxKind.AsKeyword:
                case SyntaxKind.ParamsKeyword:
                case SyntaxKind.ArgListKeyword:
                case SyntaxKind.MakeRefKeyword:
                case SyntaxKind.RefTypeKeyword:
                case SyntaxKind.RefValueKeyword:
                case SyntaxKind.ThisKeyword:
                case SyntaxKind.BaseKeyword:
                case SyntaxKind.NamespaceKeyword:
                case SyntaxKind.UsingKeyword:
                case SyntaxKind.ClassKeyword:
                case SyntaxKind.StructKeyword:
                case SyntaxKind.InterfaceKeyword:
                case SyntaxKind.EnumKeyword:
                case SyntaxKind.DelegateKeyword:
                case SyntaxKind.CheckedKeyword:
                case SyntaxKind.UncheckedKeyword:
                case SyntaxKind.UnsafeKeyword:
                case SyntaxKind.OperatorKeyword:
                case SyntaxKind.ExplicitKeyword:
                case SyntaxKind.ImplicitKeyword:
                case SyntaxKind.YieldKeyword:
                case SyntaxKind.PartialKeyword:
                case SyntaxKind.AliasKeyword:
                case SyntaxKind.GlobalKeyword:
                case SyntaxKind.AssemblyKeyword:
                case SyntaxKind.ModuleKeyword:
                case SyntaxKind.TypeKeyword:
                case SyntaxKind.FieldKeyword:
                case SyntaxKind.MethodKeyword:
                case SyntaxKind.ParamKeyword:
                case SyntaxKind.PropertyKeyword:
                case SyntaxKind.TypeVarKeyword:
                case SyntaxKind.GetKeyword:
                case SyntaxKind.SetKeyword:
                case SyntaxKind.AddKeyword:
                case SyntaxKind.RemoveKeyword:
                case SyntaxKind.WhereKeyword:
                case SyntaxKind.FromKeyword:
                case SyntaxKind.GroupKeyword:
                case SyntaxKind.JoinKeyword:
                case SyntaxKind.IntoKeyword:
                case SyntaxKind.LetKeyword:
                case SyntaxKind.ByKeyword:
                case SyntaxKind.SelectKeyword:
                case SyntaxKind.OrderByKeyword:
                case SyntaxKind.OnKeyword:
                case SyntaxKind.EqualsKeyword:
                case SyntaxKind.AscendingKeyword:
                case SyntaxKind.DescendingKeyword:
                case SyntaxKind.NameOfKeyword:
                case SyntaxKind.AsyncKeyword:
                case SyntaxKind.AwaitKeyword:
                case SyntaxKind.WhenKeyword:
                case SyntaxKind.ElifKeyword:
                case SyntaxKind.EndIfKeyword:
                case SyntaxKind.RegionKeyword:
                case SyntaxKind.EndRegionKeyword:
                case SyntaxKind.DefineKeyword:
                case SyntaxKind.UndefKeyword:
                case SyntaxKind.WarningKeyword:
                case SyntaxKind.ErrorKeyword:
                case SyntaxKind.LineKeyword:
                case SyntaxKind.PragmaKeyword:
                case SyntaxKind.HiddenKeyword:
                case SyntaxKind.ChecksumKeyword:
                case SyntaxKind.DisableKeyword:
                case SyntaxKind.RestoreKeyword:
                case SyntaxKind.ReferenceKeyword:
                case SyntaxKind.LoadKeyword:
                case SyntaxKind.IdentifierToken:
                    return true;
            }

            return false;
        }

        public async Task<ProgramComposition> ProcessAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            var root = composition.RootNode;
            root = Visit(root);
            return await composition.WithNewDocumentRootAsync(root);
        }

        public WhitespaceCompactor() : base(false)
        { }
    }
}
