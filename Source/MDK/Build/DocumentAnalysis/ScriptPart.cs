using Microsoft.CodeAnalysis;

namespace MDK.Build.DocumentAnalysis
{
    /// <summary>
    /// Represents a part of a script. Script parts will be evaluated and joined together during build in order to
    /// create the final Space Engineers script.
    /// </summary>
    public abstract class ScriptPart
    {
        /// <summary>
        /// Create a new instance of <see cref="ScriptPart"/>
        /// </summary>
        /// <param name="document"></param>
        /// <param name="partRoot"></param>
        /// <param name="sortWeight"></param>
        protected ScriptPart(Document document, SyntaxNode partRoot, long? sortWeight)
        {
            Document = document;
            PartRoot = partRoot;
            SortWeight = sortWeight;
        }

        /// <summary>
        /// The name of this document.
        /// </summary>
        public virtual string Name => Document?.Name;

        /// <summary>
        /// The document this part originated from
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The root part that contains this part
        /// </summary>
        public SyntaxNode PartRoot { get; }

        /// <summary>
        /// The sorting weight of this part. The higher the number, the higher up it gets.
        /// </summary>
        public long? SortWeight { get; }

        /// <summary>
        /// Generates the script content that makes up this part
        /// </summary>
        /// <returns></returns>
        public abstract string GenerateContent();
    }
}
