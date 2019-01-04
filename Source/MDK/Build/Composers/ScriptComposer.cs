using System.Linq;
using System.Threading.Tasks;
using Malware.MDKServices;
using MDK.Build.Solution;

namespace MDK.Build.Composers
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
        public abstract Task<string> GenerateAsync(ProgramComposition composition, MDKProjectProperties config);

        /// <summary>
        /// Trims trailing whitespace from lines and before the end of the string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        protected string TrimPointlessWhitespace(string str)
        {
            var lines = str.Split('\n').ToList();
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i].TrimEnd();
            }

            while (lines.Count > 0 && lines.Last().Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return string.Join("\n", lines);
        }
    }
}