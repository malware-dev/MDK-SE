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
        BadInstallPath
    }
}