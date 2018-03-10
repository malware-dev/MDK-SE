using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build
{
    /// <summary>
    /// Base class for script composers
    /// </summary>
    public abstract class ScriptComposer
    {
        /// <summary>
        /// Generate a Space Engineers script file from the given document
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public abstract Task<string> Generate(Document document);
    }
}