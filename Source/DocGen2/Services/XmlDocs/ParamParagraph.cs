using System.Collections.Generic;

namespace Mal.DocGen2.Services.XmlDocs
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