namespace Malware.MDKServices
{
    /// <summary>
    /// Describes the severity of a health problem
    /// </summary>
    public enum HealthSeverity
    {
        /// <summary>
        /// A warning. This problem can be ignored, at least temporarily.
        /// </summary>
        Warning,

        /// <summary>
        /// This is a critical issue and must be dealt with before the project can be loaded.
        /// </summary>
        Critical
    }
}