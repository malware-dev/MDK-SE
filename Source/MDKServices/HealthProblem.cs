namespace Malware.MDKServices
{
    /// <summary>
    /// A class describing a project health probjem.
    /// </summary>
    public class HealthProblem
    {
        /// <summary>
        /// The health code
        /// </summary>
        public HealthCode Code { get; }

        /// <summary>
        /// The severity of this health problem
        /// </summary>
        public HealthSeverity Severity { get; }

        /// <summary>
        /// Creates a new instance of <see cref="HealthProblem"/>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="severity"></param>
        /// <param name="description"></param>
        public HealthProblem(HealthCode code, HealthSeverity severity, string description)
        {
            Code = code;
            Severity = severity;
            Description = description;
        }

        /// <summary>
        /// A textual description of this problem
        /// </summary>
        public string Description { get; }
    }
}