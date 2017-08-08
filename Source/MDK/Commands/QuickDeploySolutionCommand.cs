using System.Threading.Tasks;
using EnvDTE;

namespace MDK.Commands
{
    sealed class QuickDeploySolutionCommand : DeployCommand
    {
        public QuickDeploySolutionCommand(MDKPackage package) : base(package, CommandIds.QuickDeploySolution)
        { }

        protected override async Task OnExecute(MDKPackage package, DTE dte)
        {
            await Deploy();
        }
    }
}
