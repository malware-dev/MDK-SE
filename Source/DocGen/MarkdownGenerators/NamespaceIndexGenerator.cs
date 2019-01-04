using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DocGen.Markdown;

namespace DocGen.MarkdownGenerators
{
    class NamespaceIndexGenerator : DocumentGenerator
    {
        public override async Task Generate(DirectoryInfo directory, ProgrammableBlockApi api)
        {
            var fileName = Path.Combine(directory.FullName, "Namespace-Index.md");
            using (var file = File.CreateText(fileName))
            {
                var writer = new MarkdownWriter(file);
                await writer.BeginParagraphAsync();
                await writer.WriteAsync($"← {MarkdownInline.HRef("Index", "Api-Index")}");
                await writer.EndParagraphAsync();

                await writer.WriteHeaderAsync(3, "Assemblies");
                await writer.BeginParagraphAsync();
                await writer.WriteLineAsync("These are the game assemblies used by programmable block scripts.");
                await writer.WriteLineAsync();
                foreach (var assembly in api.Entries.Select(e => e.AssemblyName).Distinct().OrderBy(n => n))
                    await writer.WriteLineAsync($"{assembly}.dll  ");
                await writer.EndParagraphAsync();

                await writer.WriteHeaderAsync(3, "Types");
                await writer.BeginParagraphAsync();
                await writer.WriteAsync("This index contains all types and members available to ingame scripting - with exception to the .NET types, because including those would have made the listing far too big. There will be links to Microsoft's own documentation for those types where appropriate.");
                await writer.EndParagraphAsync();

                foreach (var blockGroup in api.Entries.Where(e => e.Member is Type).GroupBy(e => e.Member.GetNamespace()).OrderBy(g => g.Key))
                {
                    await writer.WriteHeaderAsync(3, blockGroup.Key);
                    await writer.BeginParagraphAsync();
                    foreach (var type in blockGroup.OrderBy(e => e.ToString(ApiEntryStringFlags.ShortDisplayName | ApiEntryStringFlags.DeclaringTypes)))
                        await writer.WriteLineAsync(MemberGenerator.LinkTo(WebUtility.HtmlEncode(type.ToString(ApiEntryStringFlags.ShortDisplayName | ApiEntryStringFlags.DeclaringTypes)), type));
                    await writer.EndParagraphAsync();
                }

                await writer.FlushAsync();
            }
        }
    }
}