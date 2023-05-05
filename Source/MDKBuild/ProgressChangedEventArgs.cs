using System;

namespace MDK.Build
{
    /// <summary>
    /// Provides information about a progress change event.
    /// </summary>
    public class ProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProgressChangedEventArgs"/>
        /// </summary>
        /// <param name="text">A text describing the current action</param>
        /// <param name="step">The current step number</param>
        /// <param name="stepCount">The total number of steps</param>
        public ProgressChangedEventArgs(string text, int step, int stepCount)
        {
            Text = text;
            Step = step;
            StepCount = stepCount;
        }

        /// <summary>
        /// A text describing the current action
        /// </summary>
        public string Text { get; protected set; }

        /// <summary>
        /// The current step number
        /// </summary>
        public int Step { get; protected set; }

        /// <summary>
        /// The total number of steps
        /// </summary>
        public int StepCount { get; protected set; }
    }
}
