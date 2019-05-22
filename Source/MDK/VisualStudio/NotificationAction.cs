using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.VisualStudio
{
    /// <summary>
    /// A base class for the notification bar actions
    /// </summary>
    public abstract class NotificationAction : NotificationTextSpan, IVsInfoBarActionItem
    {
        bool _isButton;

        /// <summary>
        /// Creates a new <see cref="NotificationAction"/>
        /// </summary>
        /// <param name="isButton">Whether this action should be represented as a button <c>true</c> or a hyperlink <c>false</c></param>
        protected NotificationAction(bool isButton) { _isButton = isButton; }

        /// <summary>
        /// Invoked when this action is executed
        /// </summary>
        public event EventHandler Clicked;

        /// <summary>
        /// An optional result code to set on the bar. Will be the result value of a <see cref="NotificationBar.ShowAsync"/> call
        /// </summary>
        public string ResultCode { get; set; }

        /// <summary>
        /// A custom context
        /// </summary>
        public object ActionContext { get; set; }

        bool IVsInfoBarActionItem.IsButton => _isButton;

        /// <summary>
        /// Called to invoke this action
        /// </summary>
        /// <param name="bar"></param>
        public virtual void Click() { Clicked?.Invoke(this, EventArgs.Empty); }
    }
}