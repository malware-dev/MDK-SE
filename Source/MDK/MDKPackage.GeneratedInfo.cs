using System;
using System.Collections.Immutable;

namespace MDK 
{
	public partial class MDKPackage 
    {
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version("1.1.31");

        /// <summary>
		/// The required IDE version
		/// </summary>
		public static readonly Version RequiredIdeVersion = new Version("15.0");

	    /// <summary>
		/// Determines whether this version is a prerelease version
		/// </summary>
        public const bool IsPrerelease = false;

	    /// <summary>
		/// Gets the help page navigation URL
		/// </summary>
        public const string HelpPageUrl = "https://github.com/malware-dev/MDK-SE/wiki";

	    /// <summary>
		/// Gets the release page navigation URL
		/// </summary>
        public const string ReleasePageUrl = "https://github.com/malware-dev/MDK-SE/releases";

	    /// <summary>
		/// Gets the issues page navigation URL
		/// </summary>
        public const string IssuesPageUrl = "https://github.com/malware-dev/MDK-SE/issues";

        /// <summary>
        /// A list of the game assemblies referenced by script projects
        /// </summary>
        public static readonly ImmutableArray<string> GameAssemblyNames = new string[] 
        {
            "Sandbox.Common",
            "Sandbox.Game",
            "Sandbox.Graphics",
            "SpaceEngineers.Game",
            "SpaceEngineers.ObjectBuilders",
            "VRage",
            "VRage.Audio",
            "VRage.Game",
            "VRage.Input",
            "VRage.Library",
            "VRage.Math",
            "VRage.Render",
            "VRage.Render11",
            "VRage.Scripting"
        }.ToImmutableArray();

        /// <summary>
        /// A list of the utility assemblies referenced by script projects
        /// </summary>
        public static readonly ImmutableArray<string> UtilityAssemblyNames = new string[] 
        {
            "MDKUtilities"
        }.ToImmutableArray();

        /// <summary>
        /// A list of the game files included by script projects
        /// </summary>
        public static readonly ImmutableArray<string> GameFiles = new string[] 
        {
            
        }.ToImmutableArray();

        /// <summary>
        /// A list of the utility files included by script projects
        /// </summary>
        public static readonly ImmutableArray<string> UtilityFiles = new string[] 
        {
            "\\Analyzers\\MDKAnalyzer.dll"
        }.ToImmutableArray();
	}
}
