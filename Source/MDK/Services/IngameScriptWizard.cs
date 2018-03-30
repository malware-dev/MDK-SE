using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using Malware.MDKUtilities;
using MDK.Resources;
using MDK.Views.Wizard;
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
        const string SourceWhitelistSubPath = @"Analyzers\whitelist.cache";
        const string TargetWhitelistSubPath = @"MDK\whitelist.cache";

        SpaceEngineers _spaceEngineers;
        bool _promoteMDK = true;

        /// <summary>
        /// Creates an instance of <see cref="IngameScriptWizard"/>
        /// </summary>
        public IngameScriptWizard()
        {
            _spaceEngineers = new SpaceEngineers();
        }

        /// <inheritdoc />
        public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)automationObject);

            if (!TryGetProperties(serviceProvider, out Properties props))
                throw new WizardCancelledException();

            if (!TryGetFinalBinPath(serviceProvider, props, out string binPath))
                throw new WizardCancelledException();

            if (!TryGetFinalOutputPath(serviceProvider, props, out string outputPath))
                throw new WizardCancelledException();

            if (!TryGetFinalInstallPath(serviceProvider, out string installPath))
                throw new WizardCancelledException();

            if (!TryGetFinalMinify(props, out bool minify))
                throw new WizardCancelledException();

            if (!TryGetFinalPromoteMDK(props, out bool promoteMDK))
                _promoteMDK = true;

            var model = new NewScriptWizardDialogModel
            {
                GameBinPath = binPath,
                OutputPath = outputPath,
                Minify = minify,
                PromoteMDK = promoteMDK
            };
            var result = NewScriptWizardDialog.ShowDialog(model);
            if (result == false)
                throw new WizardCancelledException();

            replacementsDictionary["$mdkusemanualgamebinpath$"] = !string.Equals(model.GameBinPath, binPath, StringComparison.CurrentCultureIgnoreCase) ? "yes" : "no";
            replacementsDictionary["$mdkgamebinpath$"] = model.GameBinPath;
            replacementsDictionary["$mdkoutputpath$"] = model.OutputPath;
            replacementsDictionary["$mdkinstallpath$"] = installPath;
            replacementsDictionary["$mdkminify$"] = model.Minify ? "yes" : "no";
            replacementsDictionary["$mdkversion$"] = MDKPackage.Version.ToString();
            _promoteMDK = model.PromoteMDK;
        }

        void IWizard.ProjectItemFinishedGenerating(ProjectItem projectItem)
        {
        }

        /// <inheritdoc />
        public void ProjectFinishedGenerating(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)project.DTE);

            if (!TryGetFinalInstallPath(serviceProvider, out string installPath))
                throw new WizardCancelledException();

            var sourceWhitelistFile = Path.Combine(installPath, SourceWhitelistSubPath);
            if (!File.Exists(sourceWhitelistFile))
            {
                VsShellUtilities.ShowMessageBox(serviceProvider, Text.IngameScriptWizard_TryGetFinalInstallPath_CannotFindMDKPathDescription, Text.IngameScriptWizard_TryGetFinalInstallPath_CannotMDKPath, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                throw new WizardCancelledException();
            }

            while (true)
            {
                try
                {
                    var projectFileInfo = new FileInfo(project.FullName);
                    var targetWhitelistFileInfo = new FileInfo(Path.Combine(projectFileInfo.Directory.FullName, TargetWhitelistSubPath));
                    if (!targetWhitelistFileInfo.Directory.Exists)
                        targetWhitelistFileInfo.Directory.Create();
                    File.Copy(sourceWhitelistFile, targetWhitelistFileInfo.FullName, true);
                    break;
                }
                catch (Exception e)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, string.Format(Text.IngameScriptWizard_ProjectItemFinishedGenerating_CannotWriteWhitelistCacheDescription, e.Message), Text.IngameScriptWizard_ProjectItemFinishedGenerating_CannotWriteWhitelistCache, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    throw new WizardCancelledException();
                }
            }
        }

        void IWizard.BeforeOpeningFile(ProjectItem projectItem)
        { }

        void IWizard.RunFinished()
        { }

        bool IWizard.ShouldAddProjectItem(string filePath)
        {
            switch (filePath.ToLowerInvariant())
            {
                case "thumb.png":
                    return !_promoteMDK;

                case "thumbwithpromotion.png":
                    return _promoteMDK;
            }
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
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, Text.IngameScriptWizard_TryGetProperties_MDKSettingsNotFoundDescription, Text.IngameScriptWizard_TryGetProperties_MDKSettingsNotFound, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
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
                var useBinPath = (bool)props.Item(nameof(MDKOptions.UseManualGameBinPath)).Value;
                binPath = ((string)props.Item(nameof(MDKOptions.GameBinPath))?.Value)?.Trim() ?? "";
                if (!useBinPath || binPath == "")
                {
                    binPath = _spaceEngineers.GetInstallPath("Bin64");
                    if (binPath == null)
                    {
                        // We don't have a path. Just exit, let the dialog take care of it
                        return true;
                    }
                }

                var binDirectory = new DirectoryInfo(binPath);
                if (!binDirectory.Exists)
                {
                    // We have a configured path, but it fails.
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, Text.IngameScriptWizard_TryGetFinalBinPath_SEBinPathNotFoundDescription, Text.IngameScriptWizard_TryGetFinalBinPath_SEBinPathNotFound, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    binPath = null;
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
                var useOutputPath = (bool)props.Item(nameof(MDKOptions.UseManualOutputPath)).Value;
                outputPath = ((string)props.Item(nameof(MDKOptions.OutputPath))?.Value)?.Trim() ?? "";
                if (!useOutputPath || outputPath == "")
                {
                    outputPath = _spaceEngineers.GetDataPath("IngameScripts", "local");
                    if (outputPath == null)
                    {
                        // We don't have a path. Just exit, let the dialog take care of it
                        return true;
                    }
                }
                var outputDirectory = new DirectoryInfo(outputPath);
                try
                {
                    if (!outputDirectory.Exists)
                        outputDirectory.Create();
                }
                catch
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, string.Format(Text.IngameScriptWizard_TryGetFinalOutputPath_CannotCreateOutputPathDescription, outputDirectory), Text.IngameScriptWizard_TryGetFinalOutputPath_CannotCreateOutputPath, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                outputPath = outputDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }

        bool TryGetFinalInstallPath(IServiceProvider serviceProvider, out string installPath)
        {
            while (true)
            {
                installPath = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath)?.Trim() ?? "ERROR";
                var installDirectory = new DirectoryInfo(installPath);
                if (!installDirectory.Exists)
                {
                    var res = VsShellUtilities.ShowMessageBox(serviceProvider, Text.IngameScriptWizard_TryGetFinalInstallPath_CannotFindMDKPathDescription, Text.IngameScriptWizard_TryGetFinalInstallPath_CannotMDKPath, OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_RETRYCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
                    if (res == 4)
                        continue;
                    return false;
                }
                installPath = installDirectory.ToString().TrimEnd('\\');
                return true;
            }
        }

        bool TryGetFinalMinify(Properties props, out bool minify)
        {
            minify = (bool)(props.Item(nameof(MDKOptions.Minify))?.Value ?? false);
            return true;
        }

        bool TryGetFinalPromoteMDK(Properties props, out bool promoteMDK)
        {
            promoteMDK = (bool)(props.Item(nameof(MDKOptions.PromoteMDK))?.Value ?? false);
            return true;
        }
    }
}
