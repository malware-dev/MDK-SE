using System;
using System.Runtime.Serialization;

namespace Malware.MDKServices
{
    /// <summary>
    /// Exception which occurs when trying to access game information, and the game path cannot be reached.
    /// </summary>
    [Serializable]
    public class GamePathUnavailableException : MDKProjectPropertiesException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        /// <summary>
        /// Creates an instance of <see cref="GamePathUnavailableException"/>
        /// </summary>
        public GamePathUnavailableException()
            : this("The game folder could not be reached. Please make sure the game is installed, and / or check the folder settings of MDK.")
        { }

        /// <summary>
        /// Creates an instance of <see cref="GamePathUnavailableException"/>
        /// </summary>
        /// <param name="message"></param>
        public GamePathUnavailableException(string message) : base(message)
        { }

        /// <summary>
        /// Creates an instance of <see cref="GamePathUnavailableException"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public GamePathUnavailableException(string message, Exception inner) : base(message, inner)
        { }

        /// <summary>
        /// Creates an instance of <see cref="GamePathUnavailableException"/>
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected GamePathUnavailableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        { }
    }
}