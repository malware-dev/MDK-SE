using System;
using EnvDTE;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Extension helper method for the Visual Studio DTE
    /// </summary>
    public static class DTEExtensions
    {
        /// <summary>
        /// Determines whether a project is currently loaded.
        /// </summary>
        /// <param name="project"></param>
        /// <returns></returns>
        public static bool IsLoaded(this Project project)
        {
            // This is downright dirty, but it's the only way to determine if a project is loaded or not.
            try
            {
                return !string.IsNullOrEmpty(project.FullName);
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }
    }
}
