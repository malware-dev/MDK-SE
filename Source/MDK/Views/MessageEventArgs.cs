using System;
using System.ComponentModel;

namespace MDK.Views
{
    /// <summary>
    /// Represents the arguments of a message event
    /// </summary>
    public class MessageEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="MessageEventArgs"/>
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="eventType"></param>
        /// <param name="cancel"></param>
        public MessageEventArgs(string title, string description, MessageEventType eventType, bool cancel = false)
            : base(cancel)
        {
            Title = title;
            Description = description;
            EventType = eventType;
        }

        /// <summary>
        /// The title of the message
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///  The message description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The message event type
        /// </summary>
        public MessageEventType EventType { get; }
    }
}
