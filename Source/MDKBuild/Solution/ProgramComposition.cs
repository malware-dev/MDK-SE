using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace MDK.Build.Solution
{
    /// <summary>
    /// Represents the results of a program composition
    /// </summary>
    public class ProgramComposition
    {
        /// <summary>
        /// Creates a new program composition
        /// </summary>
        /// <param name="document"></param>
        /// <param name="readme"></param>
        /// <returns></returns>
        public static async Task<ProgramComposition> CreateAsync([NotNull] Document document, string readme = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var rootNode = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            return new ProgramComposition(document, rootNode, semanticModel, readme);
        }

        ProgramComposition(Document document, SyntaxNode rootNode, SemanticModel semanticModel, string readme)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
            RootNode = rootNode ?? throw new ArgumentNullException(nameof(rootNode));
            SemanticModel = semanticModel ?? throw new ArgumentNullException(nameof(semanticModel));
            Readme = readme;
        }

        /// <summary>
        /// The document representing the unified <c>Program</c>
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// Gets the root node of the document
        /// </summary>
        public SyntaxNode RootNode { get; }

        /// <summary>
        /// The semantic model of the document
        /// </summary>
        public SemanticModel SemanticModel { get; }

        /// <summary>
        /// A specialized Readme document
        /// </summary>
        public string Readme { get; }

        /// <summary>
        /// Creates a new composition with a new document based on the given syntax root.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        public Task<ProgramComposition> WithNewDocumentRootAsync([NotNull] SyntaxNode root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            return CreateAsync(Document.WithSyntaxRoot(root), Readme);
        }

        /// <summary>
        /// Creates a new composition with the new document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public Task<ProgramComposition> WithDocumentAsync([NotNull] Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return CreateAsync(document, Readme);
        }
    }
}
