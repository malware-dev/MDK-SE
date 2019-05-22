using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.VisualStudio
{
    /// <summary>
    /// A simple text span for notifications
    /// </summary>
    public class NotificationTextSpan : IVsInfoBarTextSpan
    {
        /// <summary>
        /// Determines whether this particular item should be visible
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// The text of this span
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Whether this span should be bold
        /// </summary>
        public bool Bold { get; set; }
        /// <summary>
        /// Whether this span should be italic
        /// </summary>
        public bool Italic { get; set; }

        /// <summary>
        /// Whether this span should be underlined
        /// </summary>
        public bool Underline { get; set; }
    }
}