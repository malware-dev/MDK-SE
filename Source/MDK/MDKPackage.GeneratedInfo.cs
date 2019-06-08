using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell;

namespace MDK 
{
    [InstalledProductRegistration("#110", "#112", "1.2", IconResourceID = 400)] // Info on this package for Help/About
	public partial class MDKPackage 
    {
	    /// <summary>
		/// The current package version
		/// </summary>
		public static readonly Version Version = new Version("1.2.15");

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
            
        }.ToImmutableArray();

        /// <summary>
        /// A list of the utility assemblies referenced by script projects
        /// </summary>
        public static readonly ImmutableArray<string> UtilityAssemblyNames = new string[] 
        {
            
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
            
        }.ToImmutableArray();
	}
}
