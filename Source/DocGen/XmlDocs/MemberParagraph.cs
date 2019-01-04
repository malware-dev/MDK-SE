using System.Collections.Generic;

namespace DocGen.XmlDocs
{
    class MemberParagraph : Paragraph
    {
        public MemberParagraph(IEnumerable<XmlDocNode> content) : base(ParagraphType.Member, content)
        { }
    }
}