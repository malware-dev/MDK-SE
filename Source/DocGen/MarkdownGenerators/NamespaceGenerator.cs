using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.MarkdownGenerators
{
    class NamespaceGenerator : DocumentGenerator
    {
        public override async Task Generate(DirectoryInfo directory, ProgrammableBlockApi api)
        {
            var namespaces = api.Entries.GroupBy(e => e.NamespaceName);
            await Task.WhenAll(namespaces.Select(ns => GenerateNamespaceDoc(directory, ns)));
        }

        async Task GenerateNamespaceDoc(DirectoryInfo directory, IGrouping<string, ApiEntry> ns)
        {
            var fileName = Path.Combine(directory.FullName, ToMdFileName(ns.Key));
            using (var file = File.CreateText(fileName))
            {
                var writer = new MarkdownWriter(file);
                await writer.BeginParagraphAsync();
                await writer.WriteAsync($"← {MarkdownInline.HRef("Index", "Api-Index")} ← {MarkdownInline.HRef("Namespace Index", "Namespace-Index")}");
                await writer.EndParagraphAsync();
                await writer.WriteHeaderAsync(1, ns.Key);

                await writer.BeginParagraphAsync();
                foreach (var typeGroup in ns.GroupBy(e => e.DeclaringEntry ?? e).OrderBy(g => g.Key.FullName))
                    await writer.WriteLineAsync(MarkdownInline.Strong(MemberGenerator.LinkTo(typeGroup.Key.Name, typeGroup.Key)));
                await writer.EndParagraphAsync();

                await writer.FlushAsync();
            }
        }
    }
}