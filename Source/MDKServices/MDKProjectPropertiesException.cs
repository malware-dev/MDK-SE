using System;
using System.Runtime.Serialization;

namespace Malware.MDKServices
{
    /// <summary>
    /// Exception which happens during operations in <see cref="MDKProjectProperties"/>
    /// </summary>
    [Serializable]
    public class MDKProjectPropertiesException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="MDKProjectPropertiesException"/>
        /// </summary>
        public MDKProjectPropertiesException()
        { }

        /// <summary>
        /// Creates an instance of <see cref="MDKProjectPropertiesException"/>
        /// </summary>
        /// <param name="message"></param>
        public MDKProjectPropertiesException(string message) : base(message)
        { }

        /// <summary>
        /// Creates an instance of <see cref="MDKProjectPropertiesException"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public MDKProjectPropertiesException(string message, Exception inner) : base(message, inner)
        { }

        /// <summary>
        /// Creates an instance of <see cref="MDKProjectPropertiesException"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected MDKProjectPropertiesException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        { }
    }
}