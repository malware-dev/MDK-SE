using System;

namespace MDK.Views
{
    /// <summary>
    /// Event arguments informing about the state during a close event.
    /// </summary>
    public class DialogClosingEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="DialogClosingEventArgs"/>
        /// </summary>
        /// <param name="state"></param>
        public DialogClosingEventArgs(bool? state)
        {
            State = state;
        }

        /// <summary>
        /// The requested dialog state
        /// </summary>
        public bool? State { get; }
    }
}
