namespace MDK.VisualStudio
{
    /// <summary>
    /// The current status of the solution
    /// </summary>
    public enum SolutionStatus
    {
        /// <summary>
        /// No solution is loaded
        /// </summary>
        Closed,

        /// <summary>
        /// A solution is currently being loaded
        /// </summary>
        Loading,

        /// <summary>
        /// The solution and all its projects are fully loaded
        /// </summary>
        Loaded,

        /// <summary>
        /// The solution is currently being closed
        /// </summary>
        Closing
    }
}