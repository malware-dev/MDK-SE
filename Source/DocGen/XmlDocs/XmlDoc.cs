using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DocGen.XmlDocs
{
    class XmlDoc
    {
        MemberParagraph _root;

        public static XmlDoc Generate(XElement doc)
        {
            if (doc == null)
                return null;
            var root = new XmlDoc();
            root.Root = (MemberParagraph)Decode(root, doc);
            return root;
        }

        public static XmlDocNode Decode(XmlDoc root, XNode node)
        {
            if (node == null)
                return null;
            switch (node)
            {
                case XElement element:
                    switch (element.Name.LocalName)
                    {
                        case "member":
                            return new MemberParagraph(element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "code":
                            return new CodeParagraph(element.Value);
                        case "c":
                            return new CodeSpan(element.Value);
                        case "example":
                            return new Paragraph(ParagraphType.Example, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "para":
                            return new Paragraph(ParagraphType.Default, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "param":
                            return new ParamParagraph((string)element.Attribute("name"), element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "paramref":
                            return new ParamRefSpan((string)element.Attribute("name"));
                        case "remarks":
                            return new Paragraph(ParagraphType.Remarks, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "returns":
                            return new Paragraph(ParagraphType.Returns, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "see":
                            return new TypeRefSpan((string)element.Attribute("cref"));
                        case "seealso":
                        {
                            var link = new TypeRefSpan((string)element.Attribute("cref"));
                            root.SeeAlso.Add(link);
                            return link;
                        }
                        case "summary":
                            return new Paragraph(ParagraphType.Summary, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "typeparam":
                            return new TypeParamParagraph((string)element.Attribute("name"), element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "typeparamref":
                            return new TypeParamRefSpan((string)element.Attribute("name"));
                        case "value":
                            return new Paragraph(ParagraphType.Value, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
                        case "include":
                        case "list":
                        case "permissions":
                            throw new NotImplementedException($"{element.Name.LocalName} elements are not implemented");
                    }

                    break;

                case XText text:
                    return new Span(text.Value);
            }

            return null;
        }

        public MemberParagraph Root
        {
            get => _root;
            private set
            {
                _root = value;
                Summary = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Summary);
                Example = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Example);
                Remarks = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Remarks);
                Returns = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Returns);
                Value = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Value);
                //Exception = _root?.Content.FirstOrDefault(n => n is Paragraph paragraph && paragraph.Type == ParagraphType.Ex);
            }
        }

        public XmlDocNode Value { get; set; }

        public XmlDocNode Returns { get; set; }

        public XmlDocNode Remarks { get; set; }

        public XmlDocNode Example { get; set; }

        public XmlDocNode Summary { get; private set; }

        public List<TypeRefSpan> SeeAlso { get; } = new List<TypeRefSpan>();
    }
}