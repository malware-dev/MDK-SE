using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DocGen.Markdown;
using DocGen.XmlDocs;

namespace DocGen.MarkdownGenerators
{
    class TypeGenerator : DocumentGenerator
    {
        public override async Task Generate(DirectoryInfo directory, ProgrammableBlockApi api)
        {
            var tasks = api.Entries.Where(e => e.DeclaringEntry == null || e.Member.MemberType == MemberTypes.NestedType).Select(g => GeneratePage(api, directory, g));
            await Task.WhenAll(tasks);
        }

        async Task GeneratePage(ProgrammableBlockApi api, DirectoryInfo directory, ApiEntry entry)
        {
            var fileName = Path.Combine(directory.FullName, entry.SuggestedFileName);
            Debug.WriteLine(entry.FullName +" " + fileName);
            using (var file = File.CreateText(fileName))
            {
                var writer = new MarkdownWriter(file);
                await writer.BeginParagraphAsync();
                await writer.WriteAsync($"← {MarkdownInline.HRef("Index", "Api-Index")} ← {MarkdownInline.HRef("Namespace Index", "Namespace-Index")}");
                await writer.EndParagraphAsync();
                await writer.WriteHeaderAsync(4, $"{WebUtility.HtmlEncode(entry.ToString(ApiEntryStringFlags.GenericParameters))} {ConstructOf(entry)}");
                await writer.BeginCodeBlockAsync();
                await writer.WriteLineAsync(entry.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.Inheritance));
                await writer.EndCodeBlockAsync();
                if (entry.Documentation?.Summary != null)
                    await WriteDocumentation(api, entry.Documentation?.Summary, writer);
                await writer.BeginParagraphAsync();
                await writer.WriteLineAsync($"{MarkdownInline.Strong("Namespace:")} {MarkdownInline.HRef(entry.NamespaceName, Path.GetFileNameWithoutExtension(ToMdFileName(entry.NamespaceName)))}");
                await writer.WriteLineAsync($"{MarkdownInline.Strong("Assembly:")} {entry.AssemblyName}.dll");
                await writer.EndParagraphAsync();
                if (entry.BaseEntry != null)
                    await WriteInheritance(entry, writer);
                if (entry.InheritedEntries.Count > 0)
                    await WriteInterfaces(entry, writer);
                if (entry.InheritorEntries.Count > 0)
                    await WriteInheritors(entry, writer);

                var obsoleteAttribute = entry.Member.GetCustomAttribute<ObsoleteAttribute>(false);
                if (obsoleteAttribute != null)
                {
                    await writer.WriteHeaderAsync(2, "Obsolete");
                    await writer.BeginParagraphAsync();
                    await writer.WriteLineAsync("This type should no longer be used and may be removed in the future. If you're using it, you should replace it as soon as possible.  ");
                    await writer.WriteAsync(obsoleteAttribute.Message);
                    await file.WriteLineAsync();
                }

                if (entry.Documentation?.Example != null)
                {
                    await writer.WriteHeaderAsync(4, "Example");
                    await WriteDocumentation(api, entry.Documentation?.Example, writer);
                }

                if (entry.Documentation?.Remarks != null)
                {
                    await writer.WriteHeaderAsync(4, "Remarks");
                    await WriteDocumentation(api, entry.Documentation?.Remarks, writer);
                }

                await WriteMembers(api, entry, writer);

                await writer.FlushAsync();
            }
       }

        string ConstructOf(ApiEntry entry)
        {
            var type = (Type)entry.Member;
            if (typeof(Delegate).IsAssignableFrom(type))
                return "Delegate";
            if (type.IsEnum)
                return "Enum";
            if (type.IsValueType)
                return "Struct";
            if (type.IsInterface)
                return "Interface";
            return "Class";
        }

        async Task WriteInheritance(ApiEntry entry, MarkdownWriter writer)
        {
            await writer.BeginParagraphAsync();
            await writer.WriteAsync(MarkdownInline.Strong("Inheritance:"));
            await writer.WriteAsync(" ");
            await writer.WriteLineAsync(string.Join(" ˃ ", AncestorsOf(entry).Reverse()));
            await writer.EndParagraphAsync();
        }

        async Task WriteInterfaces(ApiEntry entry, MarkdownWriter writer)
        {
            await writer.BeginParagraphAsync();
            await writer.WriteLineAsync(MarkdownInline.Strong("Implements:"));
            foreach (var iface in entry.InheritedEntries)
                await writer.WriteUnorderedListItemAsync(MemberGenerator.LinkTo(iface.ToString(ApiEntryStringFlags.ShortDisplayName), iface));
            await writer.EndParagraphAsync();
        }

        async Task WriteInheritors(ApiEntry entry, MarkdownWriter writer)
        {
            await writer.BeginParagraphAsync();
            await writer.WriteLineAsync(MarkdownInline.Strong("Inheritors:"));
            foreach (var iface in entry.InheritorEntries)
                await writer.WriteUnorderedListItemAsync(MemberGenerator.LinkTo(iface.ToString(ApiEntryStringFlags.ShortDisplayName), iface));
            await writer.EndParagraphAsync();
        }

        async Task WriteMembers(ProgrammableBlockApi api, ApiEntry entry, MarkdownWriter writer)
        {
            var memberEntries = AllInheritedEntriesOf(entry).ToList();

            await WriteTable("Fields", memberEntries.Where(m => m.Member is FieldInfo), api, entry, writer);
            await WriteTable("Events", memberEntries.Where(m => m.Member is EventInfo), api, entry, writer);
            await WriteTable("Properties", memberEntries.Where(m => m.Member is PropertyInfo), api, entry, writer);
            await WriteTable("Constructors", memberEntries.Where(m => m.Member is ConstructorInfo), api, entry, writer);
            await WriteTable("Methods", memberEntries.Where(m => m.Member is MethodInfo), api, entry, writer);
        }

        IEnumerable<ApiEntry> AllInheritedEntriesOf(ApiEntry entry)
        {
            var visitedMembers = new HashSet<ApiEntry>();
            var stack = new Stack<ApiEntry>();
            stack.Push(entry);
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                if (item.BaseEntry != null && visitedMembers.Add(item.BaseEntry))
                    stack.Push(item.BaseEntry);
                foreach (var iface in item.InheritedEntries)
                    if (visitedMembers.Add(iface))
                        stack.Push(iface);
                foreach (var member in item.MemberEntries)
                    yield return member;
            }
        }

        async Task WriteTable(string title, IEnumerable<ApiEntry> entries, ProgrammableBlockApi api, ApiEntry entry, MarkdownWriter writer)
        {
            var items = entries.ToList();
            if (items.Count == 0)
                return;
            await writer.WriteHeaderAsync(4, title);
            await writer.BeginTableAsync("Member", "Description");
            foreach (var item in items)
            {
                await writer.BeginTableCellAsync();
                await writer.WriteAsync(MemberGenerator.LinkTo(item.ToString(ApiEntryStringFlags.ParameterTypes), item));
                await writer.EndTableCellAsync();

                await writer.BeginTableCellAsync();
                var obsoleteAttribute = item.Member.GetCustomAttribute<ObsoleteAttribute>(false);
                if (obsoleteAttribute != null)
                {
                    await writer.BeginParagraphAsync();
                    if (string.IsNullOrWhiteSpace(obsoleteAttribute.Message))
                        await writer.WriteAsync(MarkdownInline.Emphasized(MarkdownInline.Strong("Obsolete")));
                    else
                        await writer.WriteAsync(MarkdownInline.Emphasized($"{MarkdownInline.Strong("Obsolete:")} {obsoleteAttribute.Message}"));
                    await writer.EndParagraphAsync();
                }

                var context = new XmlDocWriteContext(key => ResolveTypeReference(api, key));
                item.Documentation?.Summary?.WriteMarkdown(context, writer);

                if (entry != item.DeclaringEntry)
                {
                    await writer.BeginParagraphAsync();
                    await writer.WriteAsync(MarkdownInline.Emphasized($"Inherited from {MemberGenerator.LinkTo(item.DeclaringEntry.ToString(ApiEntryStringFlags.ShortDisplayName), item.DeclaringEntry)}"));
                    await writer.EndParagraphAsync();
                }

                await writer.EndTableCellAsync();
            }

            await writer.EndTableAsync();
        }

        IEnumerable<string> AncestorsOf(ApiEntry entry)
        {
            entry = entry.BaseEntry;
            while (entry != null)
            {
                yield return MemberGenerator.LinkTo(entry.ToString(ApiEntryStringFlags.None), entry);

                entry = entry.BaseEntry;
            }
        }
    }
}