using System;
using System.Collections.Immutable;

namespace Malware.MDKServices
{
    /// <summary>
    /// Options for the <see cref="HealthAnalysis"/> analysis methods
    /// </summary>
    public struct HealthAnalysisOptions
    {
        /// <summary>
        /// The default path to the game binaries
        /// </summary>
        public string DefaultGameBinPath;

        /// <summary>
        /// The default install path (for the utility assemblies etc.)
        /// </summary>
        public string InstallPath;

        /// <summary>
        /// The default path to put the deployed scripts.
        /// </summary>
        public string DefaultOutputPath;

        /// <summary>
        /// The target extension version
        /// </summary>
        public Version TargetVersion;

        /// <summary>
        /// A list of the game assemblies referenced by script projects
        /// </summary>
        public ImmutableArray<string> GameAssemblyNames;

        /// <summary>
        /// A list of the game files included by script projects
        /// </summary>
        public ImmutableArray<string> GameFiles;

        /// <summary>
        /// A list of the utility assemblies referenced by script projects
        /// </summary>
        public ImmutableArray<string> UtilityAssemblyNames;

        /// <summary>
        /// A list of the utility files included by script projects
        /// </summary>
        public ImmutableArray<string> UtilityFiles;
    }
}
