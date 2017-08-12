using System;
using MDK.Views.Whitelist;
using MDK.VisualStudio;

namespace MDK.Commands
{
    sealed class RefreshWhitelistCacheCommand : Command
    {
        public RefreshWhitelistCacheCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.RefreshWhitelistCache;

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected override void OnExecute()
        {
            var package = (MDKPackage)Package;

            RefreshWhitelistCacheDialog.ShowDialog(new RefreshWhitelistCacheDialogModel(package, package.DTE));
        }
    }
}
