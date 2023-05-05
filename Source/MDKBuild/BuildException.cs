using System;
using System.Runtime.Serialization;

namespace MDK.Build
{
    /// <summary>
    /// Represents exceptions happening during a build
    /// </summary>
    [Serializable]
    public class BuildException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="BuildException"/>
        /// </summary>
        public BuildException()
        { }

        /// <summary>
        /// Creates an instance of <see cref="BuildException"/>
        /// </summary>
        /// <param name="message"></param>
        public BuildException(string message) : base(message)
        { }

        /// <summary>
        /// Creates an instance of <see cref="BuildException"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public BuildException(string message, Exception inner) : base(message, inner)
        { }

        /// <summary>
        /// Creates an instance of <see cref="BuildException"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected BuildException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        { }
    }
}
