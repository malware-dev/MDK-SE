using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocGen.Markdown;
using DocGen.XmlDocs;

namespace DocGen.MarkdownGenerators
{
    abstract class DocumentGenerator
    {
        static readonly HashSet<char> InvalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());

        protected static string ToMdFileName(string path)
        {
            var builder = new StringBuilder(path);
            for (var i = 0; i < builder.Length; i++)
                if (InvalidCharacters.Contains(builder[i]))
                    builder[i] = '_';

            builder.Append(".md");
            return builder.ToString();
        }

        protected static async Task WriteDocumentation(ProgrammableBlockApi api, XmlDocNode docs, MarkdownWriter writer)
        {
            if (docs != null)
            {
                var context = new XmlDocWriteContext(key => ResolveTypeReference(api, key));
                await docs.WriteMarkdown(context, writer);
            }
        }

        protected static KeyValuePair<string, string> ResolveTypeReference(ProgrammableBlockApi api, string key)
        {
            if (key.StartsWith("!:"))
                return new KeyValuePair<string, string>(null, key.Substring(2));

            var entry = api.Entries.FirstOrDefault(e => e.XmlDocKey == key);
            if (entry == null)
            {
                // Assume MS type
                var name = key.Substring(2);
                return new KeyValuePair<string, string>($"https://docs.microsoft.com/en-us/dotnet/api/{name.ToLower()}?view=netframework-4.6", name);
            }

            return new KeyValuePair<string, string>(Path.GetFileNameWithoutExtension(entry.SuggestedFileName), entry.ToString(ApiEntryStringFlags.ShortDisplayName));
        }

        public abstract Task Generate(DirectoryInfo directory, ProgrammableBlockApi api);
    }
}