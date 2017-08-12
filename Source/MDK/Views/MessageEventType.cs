namespace MDK.Views
{
    /// <summary>
    /// Describes the severity of a message
    /// </summary>
    public enum MessageEventType
    {
        /// <summary>
        /// Just an informational message
        /// </summary>
        Info,

        /// <summary>
        /// A request for confirmation
        /// </summary>
        Confirm,

        /// <summary>
        /// A warning, should be noted but not too serious
        /// </summary>
        Warning,

        /// <summary>
        /// An error, action is required or some operation was aborted
        /// </summary>
        Error
    }
}