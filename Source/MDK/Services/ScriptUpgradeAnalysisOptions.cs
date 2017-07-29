using System;

namespace MDK.Services
{
    /// <summary>
    /// Options for the <see cref="ScriptUpgrades"/> analysis methods
    /// </summary>
    public struct ScriptUpgradeAnalysisOptions
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
        /// The target extension version
        /// </summary>
        public Version TargetVersion;
    }
}