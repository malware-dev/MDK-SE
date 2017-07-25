using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Malware.MDKUtilities
{
    /// <summary>
    ///     Framework initialization and configuration
    /// </summary>
    public class MDKMockups
    {
        static readonly Dictionary<string, AssemblyName> AssemblyNames = new Dictionary<string, AssemblyName>();

        /// <summary>
        ///     Initializes the mock system. Pass in the path to the Space Engineers Bin64 folder.
        /// </summary>
        /// <param name="gameBinPath">The path to the Space Engineers Bin64 folder</param>
        public static void Load(string gameBinPath)
        {
            if (string.IsNullOrEmpty(gameBinPath))
                throw new ArgumentException(Resources.MDK_Load_EmptyPath, nameof(gameBinPath));

            var directory = new DirectoryInfo(gameBinPath);
            if (!directory.Exists)
                throw new ArgumentException(string.Format(Resources.MDK_Load_PathNotFound, gameBinPath), nameof(gameBinPath));

            foreach (var dllFileName in directory.EnumerateFiles("*.dll"))
            {
                AssemblyName assemblyName;
                try
                {
                    assemblyName = AssemblyName.GetAssemblyName(dllFileName.FullName);
                }
                catch (BadImageFormatException)
                {
                    // Not a .NET assembly or wrong platform, ignore
                    continue;
                }
                AssemblyNames[assemblyName.FullName] = assemblyName;
            }
            AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
        }

        static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (AssemblyNames.TryGetValue(args.Name, out AssemblyName assemblyName))
                return Assembly.Load(assemblyName);
            return null;
        }
    }
}
