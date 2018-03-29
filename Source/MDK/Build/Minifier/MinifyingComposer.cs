using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build.Minifier
{
    /// <summary>
    /// A composer which attempts to fit the given script into as small a space as possible.
    /// </summary>
    public class MinifyingComposer: ScriptComposer
    {
        public override Task<string> Generate(Document document)
        {
            return Task.FromResult("");
        }
    }
}
