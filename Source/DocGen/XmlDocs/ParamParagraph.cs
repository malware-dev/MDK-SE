using System.Collections.Generic;

namespace DocGen.XmlDocs
{
    class ParamParagraph : Paragraph
    {
        public ParamParagraph(string name, IEnumerable<XmlDocNode> content) : base(ParagraphType.Param, content)
        {
            Name = name;
        }

        public string Name { get; }
    }
}