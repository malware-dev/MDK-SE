using System;
using System.Runtime.Serialization;

namespace MDK.Build {
    [Serializable]
    public class BuildException : Exception
    {
        public BuildException() { }
        public BuildException(string message) : base(message) { }
        public BuildException(string message, Exception inner) : base(message, inner) { }

        protected BuildException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }
}