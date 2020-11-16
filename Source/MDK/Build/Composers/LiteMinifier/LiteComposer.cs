using Malware.MDKServices;
using MDK.Build.Solution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDK.Build.Composers.Minifying
{
    class LiteComposer : MinifyingComposer
    {
        public async override Task<string> GenerateAsync(ProgramComposition composition, MDKProjectProperties config)
        {
            //simplify
            var simplifier = new CodeSimplifier();
            composition = await simplifier.ProcessAsync(composition, config);

            //Compact
            var compactor = new WhitespaceCompactor();
            composition = await compactor.ProcessAsync(composition, config);

            //Line Wrapper
            var lineWrapper = new LineWrapper();
            composition = await lineWrapper.ProcessAsync(composition, config);

            return await base.GenerateScriptAsync(composition);
        }
    }
}
