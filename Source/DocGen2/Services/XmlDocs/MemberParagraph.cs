using System.Collections.Generic;

namespace Mal.DocGen2.Services.XmlDocs
{
    class MemberParagraph : Paragraph
    {
        public MemberParagraph(IEnumerable<XmlDocNode> content) : base(ParagraphType.Member, content)
        { }
    }
}