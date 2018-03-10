using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build
{
    public class MinifyingComposer: ScriptComposer
    {
        public override Task<string> Generate(Document document)
        {
            return Task.FromResult("");
        }
    }
}
