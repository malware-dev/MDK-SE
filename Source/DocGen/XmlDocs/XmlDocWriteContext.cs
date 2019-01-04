using System;
using System.Collections.Generic;

namespace DocGen.XmlDocs
{
    class XmlDocWriteContext
    {
        readonly Func<string, KeyValuePair<string, string>> _resolveReferenceFunc;
        int _preservingWhitespaceKey;

        public XmlDocWriteContext(Func<string, KeyValuePair<string, string>> resolveReferenceFunc, bool shouldPreserveWhitespace = false)
        {
            _resolveReferenceFunc = resolveReferenceFunc;
            _preservingWhitespaceKey = shouldPreserveWhitespace ? 1 : 0;
        }

        public bool ShouldPreserveWhitespace { get; private set; }

        public void BeginPreservingWhitespace()
        {
            _preservingWhitespaceKey++;
            if (_preservingWhitespaceKey == 1)
                ShouldPreserveWhitespace = true;
        }

        public void EndPreservingWhitespace()
        {
            if (_preservingWhitespaceKey <= 0)
                throw new InvalidOperationException($"{nameof(EndPreservingWhitespace)} without {nameof(BeginPreservingWhitespace)}");
            _preservingWhitespaceKey--;
            if (_preservingWhitespaceKey == 0)
                ShouldPreserveWhitespace = false;
        }

        public KeyValuePair<string, string> ResolveReference(string uri) => _resolveReferenceFunc?.Invoke(uri) ?? new KeyValuePair<string, string>();
    }
}