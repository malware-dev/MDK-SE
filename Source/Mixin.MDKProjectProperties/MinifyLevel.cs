namespace Malware.MDKServices
{
    /// <summary>
    ///     Describes script minification level.
    ///     <para>Stored as enum item name, not as numeric value, in case a reordering/extension is needed.</para>
    ///     <para>However, due to complications with WPF, values of items in combobox should match these values.</para>
    /// </summary>
    public enum MinifyLevel
    {
        /// <summary>No changes</summary>
        None = 0,

        /// <summary>Only strip comments</summary>
        StripComments = 1,

        /// <summary>Full minification</summary>
        Full = 255
    }
}