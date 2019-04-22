using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using JetBrains.Annotations;
using Malware.MDKServices;
using MDK.Services;
using MDK.Views.Options;

namespace MDK.Views.Whitelist
{
    /// <summary>
    /// The view model for <see cref="Options.ScriptOptionsDialog"/>
    /// </summary>
    public class RefreshWhitelistCacheDialogModel : DialogViewModel
    {
        MDKPackage _package;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptOptionsDialogModel"/>
        /// </summary>
        /// <param name="package"></param>
        /// <param name="dte"></param>
        public RefreshWhitelistCacheDialogModel([NotNull] MDKPackage package, [NotNull] DTE dte)
        {
            if (dte == null)
                throw new ArgumentNullException(nameof(dte));
            _package = package ?? throw new ArgumentNullException(nameof(package));
        }

        /// <summary>
        /// Saves any changed options
        /// </summary>
        /// <returns></returns>
        protected override bool OnSave()
        {
            if (_isDone)
                return true;
            if (_isWorking)
                return false;
            Refresh();
            return false;
        }

        bool _isWorking;
        bool _isDone;

        async void Refresh()
        {
            SaveAndCloseCommand.IsEnabled = false;
            CancelCommand.IsEnabled = false;
            _isWorking = true;
            var cache = new WhitelistCache();
            await cache.RefreshAsync(_package.InstallPath.FullName);

            var dte2 = (EnvDTE80.DTE2)_package.DTE;
            var projects = ((IEnumerable)dte2.ToolWindows.SolutionExplorer.SelectedItems)
                .OfType<UIHierarchyItem>()
                .Select(item => item.Object)
                .OfType<Project>();
            foreach (var project in projects)
            {
                var projectProperties = MDKProjectProperties.Load(project.FullName, project.Name);
                if (projectProperties.IsValid)
                {
                    var targetCacheFile = Path.Combine(Path.GetDirectoryName(projectProperties.FileName) ?? ".", "MDK\\whitelist.cache");
                    var sourceCacheFile = Path.Combine(_package.InstallPath.FullName, "Analyzers\\whitelist.cache");
                    if (File.Exists(sourceCacheFile))
                        File.Copy(sourceCacheFile, targetCacheFile, true);
                }
            }

            _isDone = true;
            SaveAndCloseCommand.IsEnabled = true;
            SaveAndClose();
        }
    }
}
