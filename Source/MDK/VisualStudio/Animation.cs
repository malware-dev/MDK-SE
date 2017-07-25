namespace MDK.VisualStudio
{
    /// <summary>
    /// The available selection of toolbar animations
    /// </summary>
    public enum Animation : short
    {
        /// <summary>
        ///     Standard animation icon.
        /// </summary>
        General = 0,

        /// <summary>
        ///     Animation when printing.
        /// </summary>
        Print = 1,

        ///<summary>
        ///     Animation when saving files.
        /// </summary>
        Save = 2,

        ///<summary>
        ///     Animation when deploying the solution.
        /// </summary>
        Deploy = 3,

        ///<summary>
        ///     Animation when synchronizing files over the network.
        /// </summary>
        Synch = 4,

        ///
        /// <summary>
        ///     Animation when building the solution.
        /// </summary>
        Build = 5,

        /// <summary>
        ///     Animation when searching.
        /// </summary>
        Find = 6
    }
}
