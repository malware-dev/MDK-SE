using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MDK.Build
{
    public class ExtensionScriptPart : ScriptPart
    {
        public ExtensionScriptPart(Document document, SyntaxNode partRoot) : base(document, partRoot)
        { }

        public override IEnumerable<SyntaxNode> ContentNodes()
        {
            yield return PartRoot;
        }
    }
}