namespace Malware.MDKServices
{
    /// <summary>
    /// Codes indicating health problems
    /// </summary>
    public enum HealthCode
    {
        /// <summary>
        /// This is a healthy project.
        /// </summary>
        Healthy,

        /// <summary>
        /// This is not an MDK project.
        /// </summary>
        NotAnMDKProject,

        /// <summary>
        /// This project format is outdated.
        /// </summary>
        Outdated,

        /// <summary>
        /// The MDK.Paths.props file is missing.
        /// </summary>
        MissingPathsFile,

        /// <summary>
        /// The install path is invalid.
        /// </summary>
        BadInstallPath,

        /// <summary>
        /// The game path is invalid.
        /// </summary>
        BadGamePath,

        /// <summary>
        /// The output path is invalid.
        /// </summary>
        BadOutputPath,

        /// <summary>
        /// The whitelist cache file is missing.
        /// </summary>
        MissingWhitelist,

        /// <summary>
        /// The whitelist cache file is outdated.
        /// </summary>
        OutdatedWhitelist
    }
}