using System;
using System.Runtime.Serialization;

namespace MDK.Services
{
    /// <summary>
    /// Exception which happens during operations in <see cref="ProjectScriptInfo"/>
    /// </summary>
    [Serializable]
    public class ProjectScriptInfoException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="ProjectScriptInfoException"/>
        /// </summary>
        public ProjectScriptInfoException()
        { }

        /// <summary>
        /// Creates an instance of <see cref="ProjectScriptInfoException"/>
        /// </summary>
        /// <param name="message"></param>
        public ProjectScriptInfoException(string message) : base(message)
        { }

        /// <summary>
        /// Creates an instance of <see cref="ProjectScriptInfoException"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public ProjectScriptInfoException(string message, Exception inner) : base(message, inner)
        { }

        /// <summary>
        /// Creates an instance of <see cref="ProjectScriptInfoException"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ProjectScriptInfoException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        { }
    }
}