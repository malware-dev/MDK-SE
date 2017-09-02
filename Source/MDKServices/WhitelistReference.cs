namespace Malware.MDKServices
{
    /// <summary>
    /// Represents the analysis results of a whitelist verification
    /// </summary>
    public struct WhitelistReference
    {
        /// <summary>
        /// Creates a new <see cref="WhitelistReference"/> value
        /// </summary>
        /// <param name="hasValidWhitelistElement">Whether the project has a valid whitelist element</param>
        /// <param name="hasValidWhitelistFile">Whether the project has a valid whitelist file</param>
        /// <param name="sourceWhitelistFilePath">The full path to the whitelist cache source file</param>
        /// <param name="targetWhitelistFilePath">The full path to where the whitelist cache target file should be</param>
        public WhitelistReference(bool hasValidWhitelistElement, bool hasValidWhitelistFile, string sourceWhitelistFilePath, string targetWhitelistFilePath)
        {
            HasValidWhitelistElement = hasValidWhitelistElement;
            HasValidWhitelistFile = hasValidWhitelistFile;
            SourceWhitelistFilePath = sourceWhitelistFilePath;
            TargetWhitelistFilePath = targetWhitelistFilePath;
        }

        /// <summary>
        /// Determines whether this project has a valid whitelist element in the project file.
        /// </summary>
        public readonly bool HasValidWhitelistElement;

        /// <summary>
        /// Determines whether this project has a valid whitelist file.
        /// </summary>
        public readonly bool HasValidWhitelistFile;

        /// <summary>
        /// The full path to the whitelist cache source file
        /// </summary>
        public readonly string SourceWhitelistFilePath;

        /// <summary>
        /// The full path to where the whitelist cache target file should be
        /// </summary>
        public readonly string TargetWhitelistFilePath;

        /// <summary>
        /// Determines if the whitelist is currently deemed valid.
        /// </summary>
        public bool IsValid => HasValidWhitelistElement && HasValidWhitelistFile;
    }
}