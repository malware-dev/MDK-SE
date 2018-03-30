using System;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.VisualStudio
{
    /// <summary>
    /// A helper class designed to make it easier to deal with certain solution events and status management.
    /// </summary>
    public class SolutionManager : IDisposable, IVsSolutionEvents, IVsSolutionLoadEvents
    {
        bool _isDisposed;
        uint _solutionEventsCookie;
        IVsSolution _solutionCtl;

        /// <summary>
        /// Creates a new instance of <see cref="SolutionManager"/>
        /// </summary>
        /// <param name="serviceProvider">The package service manager</param>
        public SolutionManager(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _solutionCtl = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            _solutionCtl.AdviseSolutionEvents(this, out _solutionEventsCookie);
        }

        /// <summary>
        /// Fires when a new solution is being loaded.
        /// </summary>
        public event EventHandler SolutionLoading;

        /// <summary>
        /// Fires when a solution is fully loaded, included all its projects.
        /// </summary>
        public event EventHandler SolutionLoaded;

        /// <summary>
        /// Fires when a solution is closing.
        /// </summary>
        public event EventHandler SolutionClosing;

        /// <summary>
        /// Fires when a solution has been fully closed.
        /// </summary>
        public event EventHandler SolutionClosed;

        /// <summary>
        /// Fires when a project is fully loaded.
        /// </summary>
        public event EventHandler<ProjectLoadedEventArgs> ProjectLoaded;

        /// <summary>
        /// Gets the current solution status
        /// </summary>
        public SolutionStatus Status { get; private set; }

        /// <inheritdoc />
        ~SolutionManager()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of all connections and resources associated with this object.
        /// </summary>
        /// <param name="disposing"><c>true</c> if this method is being called from the <see cref="Dispose()"/> method.</param>
        protected virtual void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _solutionCtl.UnadviseSolutionEvents(_solutionEventsCookie);
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            // I cannot rely on the fAdded argument, because it's behavior changes between normal and lightweight solution load,
            // and I need a reliable system.

            ThreadHelper.ThrowIfNotOnUIThread();
            pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
            var project = (Project)objProj;
            if (project == null)
                return VSConstants.S_OK;
            OnProjectLoaded((Project)objProj, Status != SolutionStatus.Loading);
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            OnSolutionClosing();
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            OnSolutionClosed();
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            OnSolutionLoading();
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
        {
            OnSolutionLoaded();
            return VSConstants.S_OK;
        }

        /// <summary>
        /// Fires the <see cref="SolutionLoading"/> event
        /// </summary>
        protected virtual void OnSolutionLoading()
        {
            Status = SolutionStatus.Loading;
            SolutionLoading?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires the <see cref="SolutionLoaded"/> event
        /// </summary>
        protected virtual void OnSolutionLoaded()
        {
            Status = SolutionStatus.Loaded;
            SolutionLoaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires the <see cref="SolutionClosed"/> event
        /// </summary>
        protected virtual void OnSolutionClosing()
        {
            Status = SolutionStatus.Closing;
            SolutionClosing?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires the <see cref="SolutionClosed"/> event
        /// </summary>
        protected virtual void OnSolutionClosed()
        {
            Status = SolutionStatus.Closed;
            SolutionClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires the <see cref="ProjectLoaded"/> event
        /// </summary>
        /// <param name="project">The loaded project</param>
        /// <param name="isStandalone">Whether this project was loaded by itself or during a solution load</param>
        protected virtual void OnProjectLoaded(Project project, bool isStandalone)
        {
            ProjectLoaded?.Invoke(this, new ProjectLoadedEventArgs(project, isStandalone));
        }
    }
}
