using System;
using MDK.Views;
using MDK.VisualStudio;

namespace MDK.Commands
{
    sealed class CheckForUpdatesCommand : Command
    {
        public CheckForUpdatesCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.CheckForUpdates;

        protected override void OnBeforeQueryStatus()
        { 
        }

        protected override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            var version = await package.CheckForUpdates(package.Options.NotifyPrereleaseUpdates);
            UpdateDetectedDialog.ShowDialog(new UpdateDetectedDialogModel(version));
        }
    }
}
