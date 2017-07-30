using System;
using System.Collections.ObjectModel;

namespace MDK 
{
	public partial class MDKPackage 
    {
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version("0.9.18");

        /// <summary>
        /// A list of the game assemblies referenced by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> GameAssemblyNames = new ReadOnlyCollection<string>(new string[] 
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
        });

        /// <summary>
        /// A list of the utility assemblies referenced by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> UtilityAssemblyNames = new ReadOnlyCollection<string>(new string[] 
        {
            "MDKUtilities"
        });

        /// <summary>
        /// A list of the game files included by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> GameFiles = new ReadOnlyCollection<string>(new string[] 
        {
            
        });

        /// <summary>
        /// A list of the utility files included by script projects
        /// </summary>
        public static readonly ReadOnlyCollection<string> UtilityFiles = new ReadOnlyCollection<string>(new string[] 
        {
            "\\Analyzers\\MDKAnalyzer.dll",
            "\\Analyzers\\whitelist.cache"
        });
	}
}
