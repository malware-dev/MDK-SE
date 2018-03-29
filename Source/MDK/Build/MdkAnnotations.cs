using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace MDK.Build
{
    class MdkAnnotations
    {
        public static readonly SyntaxAnnotation PreserveAnnotation = new SyntaxAnnotation("mdk_preserve");
    }
}
