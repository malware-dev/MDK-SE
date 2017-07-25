using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace MDK.Build
{
    public abstract class ScriptPart
    {
        protected ScriptPart(Document document, SyntaxNode partRoot)
        {
            Document = document;
            PartRoot = partRoot;
        }

        public Document Document { get; }
        public SyntaxNode PartRoot { get; }

        public abstract IEnumerable<SyntaxNode> ContentNodes();
    }
}