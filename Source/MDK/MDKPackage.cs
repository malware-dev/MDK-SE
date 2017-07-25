using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using MDK.Commands;
using MDK.Services;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(MDKOptions), "MDK/SE", "Options", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed partial class MDKPackage : ExtendedPackage
    {
        /// <summary>
        /// RunMDKToolCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7b9c2d3e-b001-4a3e-86a8-00dc6f2af032";

        //SolutionEventListener _solutionEvents;
        //uint _solutionEventsCookie;

        /// <summary>
        /// Creates a new instance of <see cref="MDKPackage" />
        /// </summary>
        public MDKPackage()
        {
            ScriptUpgrades = new ScriptUpgrades(this);
            Steam = new Steam();
        }

        /// <summary>
        /// Gets the MDK options
        /// </summary>
        public MDKOptions Options => (MDKOptions)GetDialogPage(typeof(MDKOptions));

        /// <summary>
        /// The <see cref="ScriptUpgrades"/> service
        /// </summary>
        public ScriptUpgrades ScriptUpgrades { get; }

        /// <summary>
        /// The <see cref="Steam"/> service
        /// </summary>
        public Steam Steam { get; }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            KnownUIContexts.SolutionExistsAndFullyLoadedContext.UIContextChanged += SolutionExistsAndFullyLoadedContextOnUIContextChanged;

            //KnownUIContexts.ShellInitializedContext.WhenActivated(OnShellActivated);

            AddCommand(
                new DeployScriptCommand(this),
                new ScriptOptionsCommand(this),
                new RefreshWhitelistCacheCommand(this)
            );

            base.Initialize();
        }

        void SolutionExistsAndFullyLoadedContextOnUIContextChanged(object sender, UIContextChangedEventArgs e)
        {
            if (e.Activated)
            {
                var scriptProjects = new List<ProjectScriptInfo>();

                if (ScriptUpgrades.Detect(DTE.Solution, scriptProjects, Version))
                    ScriptUpgrades.QueryUpgrade(scriptProjects);
            }
        }
        //    foreach (Project project in solution.Projects)
        //        return;
        //    if (solution == null)
        //    var solution = dte?.Solution;
        //    var dte = (DTE)GetGlobalService(typeof(DTE));
        //{

        //void OnCSharpProjectContextActivated()
        //    {
        //        if (!string.IsNullOrEmpty(project.FileName))
        //            OnProjectLoaded(project);
        //    }
        //}

        //void OnShellActivated()
        //{
        //    var solutionCtl = (IVsSolution)GetGlobalService(typeof(SVsSolution));
        //    _solutionEvents = new SolutionEventListener(this);
        //    solutionCtl.AdviseSolutionEvents(_solutionEvents, out _solutionEventsCookie);
        //}

        //void OnProjectLoaded(Project project)
        //{
        //    var projectScriptInfo = new ProjectScriptInfo(this, project.FileName, project.Name);
        //    if (!projectScriptInfo.IsValid)
        //        return;
        //    if (Version == projectScriptInfo.Version)
        //        return;

        //    projectScriptInfo.UpdateOnDemand();
        //}

        //class SolutionEventListener : IVsSolutionEvents
        //{
        //    MDKPackage _package;

        //    public SolutionEventListener(MDKPackage package)
        //    {
        //        _package = package;
        //    }

        //    public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        //    {
        //        pRealHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object objProj);
        //        _package.OnProjectLoaded((Project)objProj);
        //        return VSConstants.S_OK;
        //    }

        //    public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnBeforeCloseSolution(object pUnkReserved)
        //    {
        //        return VSConstants.S_OK;
        //    }

        //    public int OnAfterCloseSolution(object pUnkReserved)
        //    {
        //        return VSConstants.S_OK;
        //    }
        //}
    }
}
