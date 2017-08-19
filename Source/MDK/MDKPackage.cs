using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EnvDTE;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Build;
using MDK.Commands;
using MDK.Resources;
using MDK.Services;
using MDK.Views.BlueprintManager;
using MDK.Views.BugReports;
using MDK.Views.ProjectIntegrity;
using MDK.Views.UpdateDetection;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK
{
    /// <summary>
    /// The MDK Visual Studio Extension
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(MDKOptions), "MDK/SE", "Options", 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution)]
    public sealed partial class MDKPackage : ExtendedPackage
    {
        /// <summary>
        /// RunMDKToolCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7b9c2d3e-b001-4a3e-86a8-00dc6f2af032";

        bool _hasCheckedForUpdates;
        bool _isEnabled;
        SolutionManager _solutionManager;

        /// <summary>
        /// Creates a new instance of <see cref="MDKPackage" />
        /// </summary>
        public MDKPackage()
        {
            ScriptUpgrades = new ScriptUpgrades();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            _solutionManager?.Dispose();
            _solutionManager = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Fired when the MDK features are enabled
        /// </summary>
        public event EventHandler Enabled;

        /// <summary>
        /// Fired when the MDK features are disabled
        /// </summary>
        public event EventHandler Disabled;

        /// <summary>
        /// Determines whether the package is currently busy deploying scripts.
        /// </summary>
        public bool IsDeploying { get; private set; }

        /// <summary>
        /// Determines whether the MDK features are currently enabled
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            private set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                if (_isEnabled)
                    Enabled?.Invoke(this, EventArgs.Empty);
                else
                    Disabled?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets the MDK options
        /// </summary>
        public MDKOptions Options => (MDKOptions)GetDialogPage(typeof(MDKOptions));

        /// <summary>
        /// The service provider
        /// </summary>
        public IServiceProvider ServiceProvider => this;

        /// <summary>
        /// The <see cref="ScriptUpgrades"/> service
        /// </summary>
        public ScriptUpgrades ScriptUpgrades { get; }

        /// <summary>
        /// Gets the installation path for the current MDK package
        /// </summary>
        public DirectoryInfo InstallPath { get; } = new FileInfo(new Uri(typeof(MDKPackage).Assembly.CodeBase).LocalPath).Directory;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            // Make sure the dialog page is loaded, since the options are needed in other threads and not preloading it 
            // here will cause a threading exception.
            GetDialogPage(typeof(MDKOptions));

            AddCommand(
                new QuickDeploySolutionCommand(this),
                new DeployProjectCommand(this),
                new RefreshWhitelistCacheCommand(this),
                new CheckForUpdatesCommand(this),
                new ProjectOptionsCommand(this),
                new BlueprintManagerCommand(this),
                new GlobalBlueprintManagerCommand(this)
            );

            KnownUIContexts.ShellInitializedContext.WhenActivated(OnShellActivated);

            base.Initialize();
        }

        async void CheckForUpdates()
        {
            if (!Options.NotifyUpdates)
                return;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var version = await CheckForUpdates(Options.NotifyPrereleaseUpdates || IsPrerelease);
            if (version != null)
                OnUpdateDetected(version);
        }

        /// <summary>
        /// Checks the GitHub sites for any updated releases.
        /// </summary>
        /// <returns>The newest release version on GitHub, or <c>null</c> if the current version is the latest</returns>
        public async Task<Version> CheckForUpdates(bool includePrerelease)
        {
            try
            {
                var client = new GitHub("malware-dev", "mdk-se", "mdk-se");
                var latestRelease = (await client.ReleasesAsync())
                    .Where(release => !string.IsNullOrWhiteSpace(release.TagName) && (!release.Prerelease || includePrerelease))
                    .OrderByDescending(r => r.PublishedAt)
                    .FirstOrDefault();
                if (latestRelease == null)
                    return null;

                var match = Regex.Match(latestRelease.TagName, @"\d+\.\d+(\.\d+)?");
                if (match.Success)
                {
                    var detectedVersion = new Version(match.Value);
                    if (detectedVersion > Version)
                        return detectedVersion;
                }
                return null;
            }
            catch (Exception e)
            {
                LogPackageError("CheckForUpdates", e);
                // We don't want to make a fuss about this.
                return null;
            }
        }

        void OnUpdateDetected(Version detectedVersion)
        {
            if (detectedVersion == null)
                return;
            UpdateDetectedDialog.ShowDialog(new UpdateDetectedDialogModel(detectedVersion));
        }

        void OnShellActivated()
        {
            _solutionManager = new SolutionManager(this);
            _solutionManager.ProjectLoaded += OnProjectLoaded;
            _solutionManager.SolutionLoaded += OnSolutionLoaded;
            _solutionManager.SolutionClosed += OnSolutionClosed; 
        }

        private void OnSolutionClosed(object sender, EventArgs e)
        {
            IsEnabled = false;
        }

        private void OnSolutionLoaded(object sender, EventArgs e)
        {
            OnSolutionLoaded(DTE.Solution);
        }

        private void OnProjectLoaded(object sender, ProjectLoadedEventArgs e)
        {
            if (e.IsStandalone)
                OnProjectLoaded(e.Project);
        }

        async void OnProjectLoaded(Project project)
        {
            ScriptSolutionAnalysisResult result;
            try
            {
                result = await ScriptUpgrades.AnalyzeAsync(project, new ScriptUpgradeAnalysisOptions
                {
                    DefaultGameBinPath = Options.GetActualGameBinPath(),
                    InstallPath = InstallPath.FullName,
                    TargetVersion = Version,
                    GameAssemblyNames = GameAssemblyNames,
                    GameFiles = GameFiles,
                    UtilityAssemblyNames = UtilityAssemblyNames,
                    UtilityFiles = UtilityFiles
                });
            }
            catch (Exception e)
            {
                ShowError(Text.MDKPackage_OnProjectLoaded_ErrorAnalyzingProject, string.Format(Text.MDKPackage_OnProjectLoaded_ErrorAnalyzingProject_Description, project?.Name), e);
                IsEnabled = false;
                return;
            }
            if (!result.HasScriptProjects)
                return;
            IsEnabled = true;
            if (result.IsValid)
                return;

            QueryUpgrade(this, result);
        }

        void QueryUpgrade([NotNull] MDKPackage package, ScriptSolutionAnalysisResult result)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            var model = new RequestUpgradeDialogModel(package, result);
            RequestUpgradeDialog.ShowDialog(model);
        }

        async void OnSolutionLoaded(Solution solution)
        {
            ScriptSolutionAnalysisResult result;
            try
            {
                result = await ScriptUpgrades.AnalyzeAsync(solution, new ScriptUpgradeAnalysisOptions
                {
                    DefaultGameBinPath = Options.GetActualGameBinPath(),
                    InstallPath = InstallPath.FullName,
                    TargetVersion = Version,
                    GameAssemblyNames = GameAssemblyNames,
                    GameFiles = GameFiles,
                    UtilityAssemblyNames = UtilityAssemblyNames,
                    UtilityFiles = UtilityFiles
                });
            }
            catch (Exception e)
            {
                ShowError(Text.MDKPackage_OnSolutionLoaded_ErrorAnalyzingSolution, Text.MDKPackage_OnSolutionLoaded_ErrorAnalyzingSolution_Description, e);
                IsEnabled = false;
                return;
            }
            if (!result.HasScriptProjects)
            {
                IsEnabled = false;
                return;
            }
            IsEnabled = true;

            if (!result.IsValid)
                QueryUpgrade(this, result);

            if (!_hasCheckedForUpdates)
            {
                _hasCheckedForUpdates = true;
                CheckForUpdates();
            }
        }

        /// <summary>
        /// Deploys the all scripts in the solution or a single script project.
        /// </summary>
        /// <param name="project">The specific project to build</param>
        /// <param name="nonBlocking"><c>true</c> if there should be no blocking dialogs shown during deployment. Instead, an <see cref="InvalidOperationException"/> will be thrown for the more grievous errors, while other stoppers merely return false.</param>
        /// <returns></returns>
        public async Task<bool> Deploy(Project project = null, bool nonBlocking = false)
        {
            var dte = DTE;

            if (IsDeploying)
            {
                if (!nonBlocking)
                    VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_Rejected_DeploymentInProgress, Text.MDKPackage_Deploy_DeploymentRejected, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return false;
            }

            if (!dte.Solution.IsOpen)
            {
                if (!nonBlocking)
                    VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_NoSolutionOpen, Text.MDKPackage_Deploy_DeploymentRejected, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return false;
            }

            if (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
            {
                if (!nonBlocking)
                    VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_Rejected_BuildInProgress, Text.MDKPackage_Deploy_DeploymentRejected, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return false;
            }

            IsDeploying = true;
            try
            {
                var tcs = new TaskCompletionSource<int>();
                void BuildEventsOnOnBuildDone(vsBuildScope scope, vsBuildAction action) => tcs.SetResult(dte.Solution.SolutionBuild.LastBuildInfo);

                int failedProjects;
                using (new StatusBarAnimation(ServiceProvider, Animation.Build))
                {
                    dte.Events.BuildEvents.OnBuildDone += BuildEventsOnOnBuildDone;
                    if (project != null)
                    {
                        dte.Solution.SolutionBuild.BuildProject(dte.Solution.SolutionBuild.ActiveConfiguration.Name, project.FullName);
                    }
                    else
                    {
                        dte.Solution.SolutionBuild.Build();
                    }

                    failedProjects = await tcs.Task;
                    dte.Events.BuildEvents.OnBuildDone -= BuildEventsOnOnBuildDone;
                }

                if (failedProjects > 0)
                {
                    if (!nonBlocking)
                        VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_BuildFailed, Text.MDKPackage_Deploy_DeploymentRejected, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return false;
                }

                string title;
                if (project != null)
                    title = string.Format(Text.MDKPackage_Deploy_DeployingSingleScript, Path.GetFileName(project.FullName));
                else
                    title = Text.MDKPackage_Deploy_DeployingAllScripts;
                ProjectScriptInfo[] deployedScripts;
                using (var statusBar = new StatusBarProgressBar(ServiceProvider, title, 100))
                using (new StatusBarAnimation(ServiceProvider, Animation.Deploy))
                {
                    var buildModule = new BuildModule(this, dte.Solution.FileName, project?.FullName, statusBar);
                    deployedScripts = await buildModule.Run();
                }

                if (deployedScripts.Length > 0)
                {
                    if (!nonBlocking)
                    {
                        var distinctPaths = deployedScripts.Select(script => FormattedPath(script.OutputPath)).Distinct().ToArray();
                        if (distinctPaths.Length == 1)
                        {
                            var model = new BlueprintManagerDialogModel(Text.MDKPackage_Deploy_Description,
                                distinctPaths[0], deployedScripts.Select(s => s.Name));
                            BlueprintManagerDialog.ShowDialog(model);
                        }
                        else
                        {
                            VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_DeploymentCompleteDescription, Text.MDKPackage_Deploy_DeploymentComplete, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                        }
                    }
                }
                else
                {
                    if (!nonBlocking)
                        VsShellUtilities.ShowMessageBox(ServiceProvider, Text.MDKPackage_Deploy_NoMDKProjects, Text.MDKPackage_Deploy_DeploymentCancelled, OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                if (!nonBlocking)
                    ShowError(Text.MDKPackage_Deploy_DeploymentFailed, Text.MDKPackage_Deploy_UnexpectedError, e);
                else
                    throw new InvalidOperationException("An unexpected error occurred during deployment.", e);
                return false;
            }
            finally
            {
                IsDeploying = false;
            }
        }

        string FormattedPath(string scriptOutputPath)
        {
            return Path.GetFullPath(scriptOutputPath).TrimEnd('\\').ToUpper();
        }

        /// <summary>
        /// Displays an error dialog
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="exception"></param>
        public void ShowError(string title, string description, Exception exception)
        {
            var errorDialogModel = new ErrorDialogModel
            {
                Title = title,
                Description = description,
                Log = exception.ToString()
            };
            ErrorDialog.ShowDialog(errorDialogModel);
        }
    }
}
