using System.Xml.Linq;

namespace MDK.Services
{
    /// <summary>
    /// Utility extensions for XDocument
    /// </summary>
    public static class XDocumentExtensions
    {
        /// <summary>
        /// If an element exists, updates its value. If not, adds a new element.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static XElement AddOrUpdateElement(this XElement parent, XName name, string value)
        {
            var element = parent.Element(name);
            if (element == null)
            {
                element = new XElement(name);
                parent.Add(name);
            }
            element.Value = value;
            return element;
        }

        /// <summary>
        /// If an attribute exists, updates its value. If not, adds a new attribute.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static XAttribute AddOrUpdateAttribute(this XElement parent, XName name, string value)
        {
            var attribute = parent.Attribute(name);
            if (attribute == null)
            {
                attribute = new XAttribute(name, value);
                parent.Add(name);
                return attribute;
            }
            attribute.Value = value;
            return attribute;
        }
    }
}
