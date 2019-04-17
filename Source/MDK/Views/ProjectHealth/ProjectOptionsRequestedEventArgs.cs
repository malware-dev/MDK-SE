using System;
using Malware.MDKServices;

namespace MDK.Views.ProjectHealth
{
    /// <summary>
    /// Contains arguments for the <see cref="ProjectHealthDialogModel.ProjectOptionsRequested"/> event.
    /// </summary>
    public class ProjectOptionsRequestedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProjectOptionsRequestedEventArgs"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="project"></param>
        public ProjectOptionsRequestedEventArgs(MDKPackage package, MDKProjectProperties project)
        {
            Package = package;
            Project = project;
        }

        /// <summary>
        /// The containing MDK package
        /// </summary>
        public MDKPackage Package { get; }

        /// <summary>
        /// The project to display project properties for
        /// </summary>
        public MDKProjectProperties Project { get; }

        /// <summary>
        /// The resuts of the dialog
        /// </summary>
        public bool? Result { get; set; }
    }
}