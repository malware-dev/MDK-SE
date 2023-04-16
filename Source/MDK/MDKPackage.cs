using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Malware.MDKServices;
using MDK.Build;
using MDK.Commands;
using MDK.Resources;
using MDK.Services;
using MDK.Views.BlueprintManager;
using MDK.Views.BugReports;
using MDK.Views.DeploymentBar;
using MDK.Views.ProjectHealth;
using MDK.Views.UpdateDetection;
using MDK.VisualStudio;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK
{
    /// <summary>
    ///     The MDK Visual Studio Extension
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // [InstalledProductRegistration("#110", "#112", "1.2", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(MDKOptions), "MDK/SE", "Options", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionOpening_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.ShellInitialized_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed partial class MDKPackage : ExtendedPackage
    {
        /// <summary>
        ///     RunMDKToolCommandPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "7b9c2d3e-b001-4a3e-86a8-00dc6f2af032";

        bool _hasCheckedForUpdates;
        bool _isEnabled;
        SolutionManager _solutionManager;

        /// <summary>
        ///     Fired when the MDK features are enabled
        /// </summary>
        public event EventHandler Enabled;

        /// <summary>
        ///     Fired when the MDK features are disabled
        /// </summary>
        public event EventHandler Disabled;

        /// <summary>
        ///     Determines whether the package is currently busy deploying scripts.
        /// </summary>
        public bool IsDeploying { get; private set; }

        /// <summary>
        ///     Determines whether the MDK features are currently enabled
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
        ///     Gets the MDK options
        /// </summary>
        public MDKOptions Options => (MDKOptions)GetDialogPage(typeof(MDKOptions));

        /// <summary>
        ///     The service provider
        /// </summary>
        public IServiceProvider ServiceProvider => this;

        /// <summary>
        ///     Gets the installation path for the current MDK package
        /// </summary>
        public DirectoryInfo InstallPath { get; } = new FileInfo(new Uri(typeof(MDKPackage).Assembly.CodeBase).LocalPath).Directory;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            //if (_solutionManager != null)
            //{
            //    _solutionManager?.Dispose();
            //    _solutionManager.ProjectLoaded -= OnProjectLoaded;
            //    _solutionManager.SolutionLoaded -= OnSolutionLoaded;
            //    _solutionManager.SolutionClosed -= OnSolutionClosed;
            //}

            _solutionManager = null;
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Initialization of the package; this method is called right after the package is sited, so this is the place
        ///     where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            Echo(new[] { "Initializing MDK" });
            _solutionManager = new SolutionManager(this);

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

            await base.InitializeAsync(cancellationToken, progress);
        }

        void OnUpdateDetected(Version detectedVersion)
        {
            if (detectedVersion == null)
                return;
            UpdateDetectedDialog.ShowDialog(new UpdateDetectedDialogModel(detectedVersion));
        }

        async void OnShellActivated()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (_solutionManager.Status == SolutionStatus.Loaded)
            {
                Echo(new[] { "A solution is already loaded on start" });
                OnSolutionLoaded(DTE.Solution);
            }
            _solutionManager.ProjectLoaded += OnProjectLoaded;
            _solutionManager.SolutionLoaded += OnSolutionLoaded;
            _solutionManager.SolutionClosed += OnSolutionClosed;
        }

        void OnSolutionClosed(object sender, EventArgs e)
        {
            IsEnabled = false;
        }

        void OnSolutionLoaded(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Echo(new[] { "Detected the loading of a solution" });
            OnSolutionLoaded(DTE.Solution);
        }

        void OnProjectLoaded(object sender, ProjectLoadedEventArgs e)
        {
            if (e.IsStandalone)
                OnProjectLoaded(e.Project);
        }

        void OnProjectLoaded(Project project)
        {
            Echo(new[] { "Detected the loading of a project" });
            HealthAnalysis analysis;
            try
            {
                analysis = HealthAnalysis.Analyze(project, GetAnalysisOptions());
            }
            catch (Exception e)
            {
                ShowError(Text.MDKPackage_OnProjectLoaded_ErrorAnalyzingProject, string.Format(Text.MDKPackage_OnProjectLoaded_ErrorAnalyzingProject_Description, project?.Name), e);
                IsEnabled = false;
                return;
            }

            if (!analysis.IsMDKProject)
                return;
            IsEnabled = true;

            if (analysis.IsHealthy)
                return;

            PresentAnalysisResults(analysis);
        }

        void OnSolutionLoaded(Solution solution)
        {
            HealthAnalysis[] analyses;
            try
            {
                analyses = HealthAnalysis.Analyze(solution, GetAnalysisOptions());
            }
            catch (Exception e)
            {
                ShowError(Text.MDKPackage_OnSolutionLoaded_ErrorAnalyzingSolution, Text.MDKPackage_OnSolutionLoaded_ErrorAnalyzingSolution_Description, e);
                IsEnabled = false;
                return;
            }

            if (!analyses.Any(analysis => analysis.IsMDKProject))
            {
                IsEnabled = false;
                return;
            }

            IsEnabled = true;

            var unhealtyProjects = analyses.Where(analysis => analysis.IsMDKProject && !analysis.IsHealthy).ToArray();
            if (unhealtyProjects.Any())
                PresentAnalysisResults(unhealtyProjects);

            if (!_hasCheckedForUpdates)
            {
                _hasCheckedForUpdates = true;
                CheckForUpdates();
            }
        }

        async void CheckForUpdates()
        {
            if (!Options.NotifyUpdates)
                return;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var version = await CheckForUpdatesAsync(Options.NotifyPrereleaseUpdates || IsPrerelease);
            if (version != null)
                OnUpdateDetected(version);
        }

        /// <summary>
        ///     Checks the GitHub sites for any updated releases.
        /// </summary>
        /// <returns>The newest release version on GitHub, or <c>null</c> if the current version is the latest</returns>
        public async Task<Version> CheckForUpdatesAsync(bool includePrerelease)
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
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                LogPackageError("CheckForUpdates", e);
                // We don't want to make a fuss about this.
                return null;
            }
        }

        HealthAnalysisOptions GetAnalysisOptions() =>
            new HealthAnalysisOptions
            {
                DefaultGameBinPath = Options.GetActualGameBinPath(),
                DefaultOutputPath = Options.GetActualOutputPath(),
                InstallPath = InstallPath.FullName,
                TargetVersion = Version,
                GameAssemblyNames = GameAssemblyNames,
                GameFiles = GameFiles,
                UtilityAssemblyNames = UtilityAssemblyNames,
                UtilityFiles = UtilityFiles,
                Echo = v => Echo(new[] { v })
            };

        void PresentAnalysisResults(params HealthAnalysis[] analysis)
        {
            var model = new ProjectHealthDialogModel(this, analysis);
            ProjectHealthDialog.ShowDialog(model);
        }

        /// <summary>
        ///     Deploys the all scripts in the solution or a single script project.
        /// </summary>
        /// <param name="project">The specific project to build</param>
        /// <param name="nonBlocking">
        ///     <c>true</c> if there should be no blocking dialogs shown during deployment. Instead, an
        ///     <see cref="InvalidOperationException" /> will be thrown for the more grievous errors, while other stoppers merely
        ///     return false.
        /// </param>
        /// <returns></returns>
        public async Task<bool> DeployAsync(Project project = null, bool nonBlocking = false)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
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
                int failedProjects;
                using (new StatusBarAnimation(ServiceProvider, Animation.Build))
                {
                    if (project != null)
                        dte.Solution.SolutionBuild.BuildProject(dte.Solution.SolutionBuild.ActiveConfiguration.Name, project.FullName, true);
                    else
                        dte.Solution.SolutionBuild.Build(true);
                    failedProjects = dte.Solution.SolutionBuild.LastBuildInfo;
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
                MDKProjectProperties[] deployedScripts;
                using (var statusBar = new StatusBarProgressBar(ServiceProvider, title, 100))
                using (new StatusBarAnimation(ServiceProvider, Animation.Deploy))
                {
                    var buildModule = new BuildModule(this, dte.Solution.FileName, project?.FullName, statusBar);
                    deployedScripts = await buildModule.RunAsync();
                }

                if (deployedScripts.Length > 0)
                {
                    if (!nonBlocking && Options.ShowBlueprintManagerOnDeploy)
                    {
                        var bar = new DeploymentBar
                        {
                            DeployedScripts = deployedScripts,
                            CanShowMe = deployedScripts.Select(script => FormattedPath(script.Paths.OutputPath)).Distinct().Count() == 1
                        };
#pragma warning disable 4014
                        bar.ShowAsync(ServiceProvider);
#pragma warning restore 4014
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
            catch (UnauthorizedAccessException e)
            {
                if (!nonBlocking)
                    VsShellUtilities.ShowMessageBox(ServiceProvider, e.Message, Text.MDKPackage_Deploy_DeploymentCancelled, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                else
                    throw;
                return false;
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

        string FormattedPath(string scriptOutputPath) => Path.GetFullPath(Environment.ExpandEnvironmentVariables(scriptOutputPath)).TrimEnd('\\').ToUpper();

        /// <summary>
        ///     Displays an error dialog
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

        static readonly Guid OutputGuid = new Guid("9D599C61-0B50-46FD-9230-7709866F32B7");

        /// <summary>
        /// Echoes output information to the MDK dev console.
        /// </summary>
        /// <param name="values"></param>
        public void Echo(params object[] values)
        {
            string message;
            if (values == null || values.Length == 0)
                message = "";
            else
                message = string.Join(" ", values.Select(TranslateForEcho));

            Echo(message);
        }

        async void Echo(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var output =  (IVsOutputWindow)await GetServiceAsync(typeof(SVsOutputWindow));
            if (output == null)
                return;
            var paneGuid = OutputGuid;
            output.GetPane(ref paneGuid, out var pane);
            if (pane == null)
            {
                output.CreatePane(ref paneGuid, "MDK Dev Console", 1, 1);
                output.GetPane(ref paneGuid, out pane);
            }

            pane.OutputStringThreadSafe($"{message}\n");
        }

        string TranslateForEcho(object value)
        {
            return (value == null) ? "(null)" : value.ToString();
        }
    }
}
