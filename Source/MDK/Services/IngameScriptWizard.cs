using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EnvDTE;
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
        /// <summary>
        /// Creates an instance of <see cref="IngameScriptWizard"/>
        /// </summary>
        public IngameScriptWizard()
        {
            SpaceEngineers = new SpaceEngineers();
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The <see cref="SpaceEngineers"/> service
        /// </summary>
        public SpaceEngineers SpaceEngineers { get; }

        void IWizard.BeforeOpeningFile(ProjectItem projectItem)
        { }

        void IWizard.ProjectFinishedGenerating(Project project)
        { }

        void IWizard.ProjectItemFinishedGenerating(ProjectItem projectItem)
        { }

        void IWizard.RunFinished()
        { }

        /// <inheritdoc />
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)automationObject);

            while (true)
            {
                if (!TryGetProperties(serviceProvider, out Properties props))
                    throw new WizardCancelledException();

                if (!TryGetFinalBinPath(serviceProvider, props, out string binPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkgamebinpath$"] = binPath;

                if (!TryGetFinalOutputPath(serviceProvider, props, out string outputPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkoutputpath$"] = outputPath;

                if (!TryGetFinalUtilityPath(serviceProvider, props, out string utilityPath))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkutilitypath$"] = utilityPath;

                if (!TryGetFinalMinify(serviceProvider, props, out bool minify))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkminify$"] = minify ? "yes" : "no";

                if (!TryGetExtensionVersion(serviceProvider, props, out Version version))
                    throw new WizardCancelledException();
                replacementsDictionary["$mdkversion$"] = version.ToString();
                return;
            }
        }

        bool IWizard.ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        bool TryGetProperties(IServiceProvider serviceProvider, out Properties props)
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

        bool TryGetFinalBinPath(IServiceProvider serviceProvider, Properties props, out string binPath)
        {
            while (true)
            {
                binPath = ((string)props.Item("GameBinPath")?.Value)?.Trim() ?? "";
                if (binPath == "")
                    binPath = SpaceEngineers.GetInstallPath("Bin64");

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


        bool TryGetFinalOutputPath(IServiceProvider serviceProvider, Properties props, out string outputPath)
        {
            while (true)
            {
                outputPath = ((string)props.Item("OutputPath")?.Value)?.Trim() ?? "";
                if (outputPath == "")
                    outputPath = SpaceEngineers.GetDataPath("IngameScripts", "local");
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

        bool TryGetFinalUtilityPath(IServiceProvider serviceProvider, Properties props, out string utilityPath)
        {
            while (true)
            {
                utilityPath = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath)?.Trim() ?? "ERROR";
                var utilityDirectory = new DirectoryInfo(utilityPath);
                if (!utilityDirectory.Exists)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, "Cannot find the MDK/SE utility path. The install might be corrupted. Please reinstall the extension.", "Cannot Find MDK Utility Path", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                utilityPath = utilityDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }

        bool TryGetFinalMinify(IServiceProvider serviceProvider, Properties props, out bool minify)
        {
            minify = (bool)(props.Item("Minify")?.Value ?? false);
            return true;
        }

        bool TryGetExtensionVersion(ServiceProvider serviceProvider, Properties props, out Version currentVersion)
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
