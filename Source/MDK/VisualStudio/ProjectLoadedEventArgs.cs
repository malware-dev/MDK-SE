using EnvDTE;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Arguments for an event indicating the loading of a project
    /// </summary>
    public class ProjectLoadedEventArgs : ProjectEventArgs
    {
        /// <summary>
        /// Indicates whether this project has been loaded on its own, and not as a part of a solution load.
        /// </summary>
        public bool IsStandalone { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ProjectLoadedEventArgs"/>
        /// </summary>
        /// <param name="project"></param>
        /// <param name="isStandalone"></param>
        public ProjectLoadedEventArgs(Project project, bool isStandalone) : base(project)
        {
            IsStandalone = isStandalone;
        }
    }
}