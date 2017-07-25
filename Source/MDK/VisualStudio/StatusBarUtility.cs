using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Provides a base class for accessing various aspects of the Visual Studio status bar
    /// </summary>
    public abstract class StatusBarUtility : IDisposable
    {
        /// <summary>
        /// Creates a new instance of the <see cref="StatusBarUtility"/>
        /// </summary>
        /// <param name="serviceProvider"></param>
        protected StatusBarUtility(IServiceProvider serviceProvider)
        {
            StatusBar = (IVsStatusbar)serviceProvider.GetService(typeof(SVsStatusbar));
        }

        /// <summary>
        /// Gets the Visual Studio status bar service
        /// </summary>
        protected IVsStatusbar StatusBar { get; }

        /// <summary>
        /// Cleans up the resources used by this class.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method was called by the <see cref="Dispose()"/> method, false if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        { }

        /// <summary>
        /// Cleans up the resources used by this class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
