using System.Xml.Linq;

namespace MDK.Services
{
    /// <summary>
    /// Represents a bad reference in a script project.
    /// </summary>
    public struct BadReference
    {
        /// <summary>
        /// The reference type
        /// </summary>
        public readonly BadReferenceType Type;

        /// <summary>
        /// The XML element where this reference is defined
        /// </summary>
        public readonly XElement Element;

        /// <summary>
        /// The original bad path
        /// </summary>
        public readonly string BadPath;
        
        /// <summary>
        /// The reference path as it was expected to be
        /// </summary>
        public readonly string ExpectedPath;

        /// <summary>
        /// Creates an instance of <see cref="BadReference"/>
        /// </summary>
        /// <param name="type">The reference type</param>
        /// <param name="element">The XML element where this reference is defined</param>
        /// <param name="badPath">The original bad path</param>
        /// <param name="expectedPath">The reference path as it was expected to be</param>
        public BadReference(BadReferenceType type, XElement element, string badPath, string expectedPath)
        {
            Type = type;
            Element = element;
            BadPath = badPath;
            ExpectedPath = expectedPath;
        }
    }
}