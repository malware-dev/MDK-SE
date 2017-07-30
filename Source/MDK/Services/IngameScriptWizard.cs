using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnvDTE;
using Malware.MDKUtilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;

namespace MDK.Services
{
    /// <summary>
    /// A project template wizard designed to augment the ingame script templates with MDK information macros
    /// </summary>
    [ComVisible(true)]
    [Guid("0C84F679-2E43-491E-B9A6-75599C2C4AE5")]
    [ProgId("MDK.Services.IngameScriptWizard")]
    public class IngameScriptWizard : IWizard
    {
        SpaceEngineers _spaceEngineers;

        /// <summary>
        /// Creates an instance of <see cref="IngameScriptWizard"/>
        /// </summary>
        public IngameScriptWizard()
        {
            _spaceEngineers = new SpaceEngineers();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc />
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)automationObject);

            while (true)
            {
                if (!TryGetProperties(serviceProvider, out EnvDTE.Properties props))
                    throw new WizardCancelledException();

                if (!TryGetFinalUseManualGameBinPath(serviceProvider, props, out bool useManualGameBinPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkusemanualgamebinpath$"] = useManualGameBinPath ? "yes" : "no";

                if (!TryGetFinalBinPath(serviceProvider, props, out string binPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkgamebinpath$"] = binPath;

                if (!TryGetFinalOutputPath(serviceProvider, props, out string outputPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkoutputpath$"] = outputPath;

                if (!TryGetFinalInstallPath(serviceProvider, props, out string installPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkinstallpath$"] = installPath;

                if (!TryGetFinalMinify(serviceProvider, props, out bool minify))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkminify$"] = minify ? "yes" : "no";

                if (!TryGetExtensionVersion(serviceProvider, props, out Version version))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkversion$"] = version.ToString();
                return;
            }
        }

        /// <inheritdoc />
        public void ProjectFinishedGenerating(Project project)
        {
            // Visual Studio sometimes generates invalid paths. This is an attempt to work around that problem.
            // If we detect any failure, we simply ignore it - the probability is negligible, and the
            // result is merely an inconvenience.
            // ReSharper disable once SuspiciousTypeConversion.Global
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)project.DTE);

            if (!TryGetProperties(serviceProvider, out EnvDTE.Properties props))
                return;

            if (!TryGetFinalBinPath(serviceProvider, props, out string binPath))
                return;

            if (!TryGetFinalInstallPath(serviceProvider, props, out string installPath))
                return;

            var scriptUpgrades = new ScriptUpgrades();
            var result = scriptUpgrades.Analyze(project, new ScriptUpgradeAnalysisOptions
            {
                DefaultGameBinPath = binPath,
                InstallPath = installPath,
                TargetVersion = MDKPackage.Version
            });
            if (result.IsValid)
                return;
            scriptUpgrades.Upgrade(result);
        }

        void IWizard.BeforeOpeningFile(ProjectItem projectItem)
        { }

        void IWizard.ProjectItemFinishedGenerating(ProjectItem projectItem)
        { }

        void IWizard.RunFinished()
        { }

        bool IWizard.ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        bool TryGetProperties(IServiceProvider serviceProvider, out EnvDTE.Properties props)
        {
            while (true)
            {
                try
                {
                    var dte = (DTE)serviceProvider.GetService(typeof(DTE));
                    props = dte.Properties["MDK/SE", "Options"];
                }
                catch (COMException)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, "Cannot find the MDK/SE settings. The install might be corrupted. Please reinstall the extension.", "Cannot Find MDK Settings", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    props = null;
                    return false;
                }
                return true;
            }
        }

        bool TryGetFinalBinPath(IServiceProvider serviceProvider, EnvDTE.Properties props, out string binPath)
        {
            while (true)
            {
                var useBinPath = (bool)props.Item(nameof(MDKOptions.UseManualGameBinPath)).Value;
                binPath = ((string)props.Item(nameof(MDKOptions.GameBinPath))?.Value)?.Trim() ?? "";
                if (!useBinPath || binPath == "")
                    binPath = _spaceEngineers.GetInstallPath("Bin64");

                var binDirectory = new DirectoryInfo(binPath);
                if (!binDirectory.Exists)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, "Cannot find the install path of Space Engineers. Please install the game before creating an Ingame Script project.", "Cannot Find Space Engineers", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                binPath = binDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }


        bool TryGetFinalOutputPath(IServiceProvider serviceProvider, EnvDTE.Properties props, out string outputPath)
        {
            while (true)
            {
                var useOutputPath = (bool)props.Item(nameof(MDKOptions.UseManualOutputPath)).Value;
                outputPath = ((string)props.Item(nameof(MDKOptions.OutputPath))?.Value)?.Trim() ?? "";
                if (!useOutputPath || outputPath == "")
                    outputPath = _spaceEngineers.GetDataPath("IngameScripts", "local");
                var outputDirectory = new DirectoryInfo(outputPath);
                try
                {
                    if (!outputDirectory.Exists)
                        outputDirectory.Create();
                }
                catch
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, $"Could not create the desired output path.{Environment.NewLine}{outputDirectory}", "Cannot Create Folder", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                outputPath = outputDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }

        bool TryGetFinalInstallPath(IServiceProvider serviceProvider, EnvDTE.Properties props, out string installPath)
        {
            while (true)
            {
                installPath = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath)?.Trim() ?? "ERROR";
                var installDirectory = new DirectoryInfo(installPath);
                if (!installDirectory.Exists)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, "Cannot find the MDK/SE install path. The install might be corrupted. Please reinstall the extension.", "Cannot Find MDK Utility Path", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                installPath = installDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }

        bool TryGetFinalMinify(IServiceProvider serviceProvider, EnvDTE.Properties props, out bool minify)
        {
            minify = (bool)(props.Item(nameof(MDKOptions.Minify))?.Value ?? false);
            return true;
        }

        bool TryGetFinalUseManualGameBinPath(IServiceProvider serviceProvider, EnvDTE.Properties props, out bool minify)
        {
            minify = (bool)(props.Item(nameof(MDKOptions.UseManualGameBinPath))?.Value ?? false);
            return true;
        }

        bool TryGetExtensionVersion(ServiceProvider serviceProvider, EnvDTE.Properties props, out Version currentVersion)
        {
            currentVersion = MDKPackage.Version;
            return true;
        }

        /// <summary>
        /// Called when a trackable property changes
        /// </summary>
        /// <param name="propertyName">The name of the property, or <c>null</c> to indicate a global change</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
