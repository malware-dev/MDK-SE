using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using Malware.MDKServices;

namespace Malware.MDKUtilities
{
    /// <summary>
    ///     Framework initialization and configuration
    /// </summary>
    public class MDKUtilityFramework
    {
        static readonly Dictionary<string, AssemblyName> AssemblyNames = new Dictionary<string, AssemblyName>();

        /// <summary>
        /// Gets the game binary path as defined through <see cref="Load"/>.
        /// </summary>
        public static string GameBinPath { get; internal set; }

        /// <summary>
        ///     Initializes the mock system. Pass in the path to the Space Engineers Bin64 folder.
        /// </summary>
        /// <param name="mdkOptionsPath">The path to the MDK options file</param>
        public static void Load(string mdkOptionsPath = @"mdk\mdk.paths.props")
        {
            if (string.IsNullOrEmpty(mdkOptionsPath))
                throw new ArgumentException(Resources.MDK_Load_EmptyPath, nameof(mdkOptionsPath));

            var props = MDKProjectPaths.Load(mdkOptionsPath);
            var gameBinPath = props.GameBinPath;

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

            GameBinPath = directory.FullName;
        }

        static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (AssemblyNames.TryGetValue(args.Name, out AssemblyName assemblyName))
                return Assembly.Load(assemblyName);
            return null;
        }
    }
}
