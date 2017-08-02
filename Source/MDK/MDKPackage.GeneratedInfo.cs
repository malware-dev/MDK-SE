using System;
using System.Collections.Immutable;

namespace MDK 
{
	public partial class MDKPackage 
    {
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version("0.9.19");

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
            "\\Analyzers\\MDKAnalyzer.dll",
            "\\Analyzers\\whitelist.cache"
        }.ToImmutableArray();
	}
}
