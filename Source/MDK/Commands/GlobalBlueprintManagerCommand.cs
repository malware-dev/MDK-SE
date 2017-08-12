using System;
using Malware.MDKServices;
using MDK.Resources;
using MDK.Views.BlueprintManager;
using MDK.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.Commands
{
    sealed class GlobalBlueprintManagerCommand : Command
    {
        public GlobalBlueprintManagerCommand(ExtendedPackage package) : base(package)
        { }

        public override Guid GroupId { get; } = CommandGroups.MDKGroup;

        public override int Id { get; } = CommandIds.GlobalBlueprintManager;

        protected override void OnBeforeQueryStatus()
        { }

        protected override void OnExecute()
        {
            var package = (MDKPackage)Package;
            var model = new BlueprintManagerDialogModel
            {
                BlueprintPath =  package.Options.GetActualOutputPath()
            };
            BlueprintManagerDialog.ShowDialog(model);
        }
    }
}
