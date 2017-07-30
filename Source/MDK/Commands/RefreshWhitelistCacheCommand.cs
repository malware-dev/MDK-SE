using System;
using MDK.Views;
using MDK.VisualStudio;

namespace MDK.Commands
{
    sealed class RefreshWhitelistCacheCommand : Command
    {
        public RefreshWhitelistCacheCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = 0x0102;

        protected override void OnExecute()
        {
            var package = (MDKPackage)Package;

            RefreshWhitelistCacheDialog.ShowDialog(new RefreshWhitelistCacheDialogModel(package, package.DTE));
        }
    }
}
