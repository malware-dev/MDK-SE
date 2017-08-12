using System;
using MDK.VisualStudio;

namespace MDK.Commands
{
    sealed class QuickDeploySolutionCommand : Command
    {
        public QuickDeploySolutionCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.QuickDeploySolution;

        protected override void OnBeforeQueryStatus()
        {
            var package = (MDKPackage)Package;
            OleCommand.Visible = package.IsEnabled;
        }

        protected override async void OnExecute()
        {
            var package = (MDKPackage)Package;
            await package.Deploy();
        }
    }
}
