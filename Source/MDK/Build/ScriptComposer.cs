using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;
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
        /// <param name="composition"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public abstract Task<string> Generate(ProgramComposition composition, ProjectScriptInfo config);
    }
}