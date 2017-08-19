using System;
using EnvDTE;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Arguments for an event related to projects
    /// </summary>
    public class ProjectEventArgs : EventArgs
    {
        /// <summary>
        /// The project this event is connected to
        /// </summary>
        public Project Project { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ProjectEventArgs "/>
        /// </summary>
        /// <param name="project"></param>
        public ProjectEventArgs(Project project)
        {
            Project = project;
        }
    }
}