using System.Collections.Generic;

namespace DocGen.XmlDocs
{
    class TypeParamParagraph : Paragraph
    {
        public TypeParamParagraph(string name, IEnumerable<XmlDocNode> content) : base(ParagraphType.Param, content)
        {
            Name = name;
        }

        public string Name { get; }
    }
}