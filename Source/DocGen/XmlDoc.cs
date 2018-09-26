using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace DocGen
{
    class MemberParagraph : Paragraph
    {
        public MemberParagraph(IEnumerable<XmlDoc> content) : base(ParagraphType.Member, content)
        { }
    }

    class XmlDoc
    {
        public static XmlDoc Generate(XElement doc)
        {
            if (doc == null)
                return null;
            var root = new XmlDoc();
            var summary = Decode(root, doc);
            return summary;
        }

        public static XmlDoc Decode(XmlDoc root, XNode node)
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
                            return new Paragraph(ParagraphType.Remarks, element.Nodes().Select(n => Decode(root, n)).Where(n => n != null));
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

        public List<TypeRefSpan> SeeAlso { get; } = new List<TypeRefSpan>();
    }

    class TypeRefSpan : Span
    {
        public TypeRefSpan(string textValue) : base(textValue)
        { }
    }

    class ParamRefSpan : Span
    {
        public ParamRefSpan(string textValue) : base(textValue)
        { }
    }

    class TypeParamRefSpan : Span
    {
        public TypeParamRefSpan(string textValue) : base(textValue)
        { }
    }

    class ParamParagraph : Paragraph
    {
        public ParamParagraph(string name, IEnumerable<XmlDoc> content) : base(ParagraphType.Param, content)
        {
            Name = name;
        }

        public string Name { get; }
    }

    class TypeParamParagraph : Paragraph
    {
        public TypeParamParagraph(string name, IEnumerable<XmlDoc> content) : base(ParagraphType.Param, content)
        {
            Name = name;
        }

        public string Name { get; }
    }

    class CodeSpan : Span
    {
        public CodeSpan(string textValue) : base(textValue)
        { }
    }

    class CodeParagraph : Paragraph
    {
        public CodeParagraph(string content) : base(ParagraphType.Code, new[] {new Span(content)})
        { }
    }

    class Span : XmlDoc
    {
        public Span(string textValue) => TextValue = textValue;

        public string TextValue { get; }

        public override string ToString() => TextValue;
    }

    public enum ParagraphType
    {
        Default,
        Example,
        Param,
        Code,
        Remarks,
        Summary,
        Value,
        Member
    }

    class Paragraph : XmlDoc
    {
        public ParagraphType Type { get; }

        public Paragraph(ParagraphType type, IEnumerable<XmlDoc> content)
        {
            Type = type;
            Content = new ReadOnlyCollection<XmlDoc>(content?.Where(n => n != null).ToList() ?? new List<XmlDoc>());
        }

        public ReadOnlyCollection<XmlDoc> Content { get; }
    }
}