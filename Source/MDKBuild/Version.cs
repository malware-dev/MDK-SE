using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace MDK.Build
{
    public static class AssemblyVersion
    {
        public static string Version {
            get {
                var assembly = Assembly.GetExecutingAssembly();
                var info = FileVersionInfo.GetVersionInfo(assembly.Location);
                return info.ProductVersion;
            }
        }
    }
}
