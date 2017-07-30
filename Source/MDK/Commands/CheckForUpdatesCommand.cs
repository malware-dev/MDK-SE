using System;
using System.Diagnostics;
using MDK.Properties;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class CheckForUpdatesCommand : Command
    {
        public CheckForUpdatesCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = 0x103;

        protected override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            var version = await package.CheckForUpdates(package.Options.NotifyPrereleaseUpdates);
            int result;
            if (version != null)
                result = VsShellUtilities.ShowMessageBox(ServiceProvider, $"There's a new version {version} available. Do you want to open the download page?", "New Version Available", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            else
                result = VsShellUtilities.ShowMessageBox(ServiceProvider, "No new versions are available. Do you want to open the download page anyway?", "No New Version", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND);
            if (result == 6)
                Process.Start(Settings.Default.ReleasePageUrl);
        }
    }
}
