using System;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Represents the progress bar area of the Visual Studio status bar.
    /// </summary>
    public class StatusBarProgressBar : StatusBarUtility, IProgress<float>, IProgress<int>
    {
        uint _cookie;
        string _text;
        int _value;
        int _maxValue;
        bool _isEnabled;

        /// <summary>
        /// Creates an instance of the <see cref="StatusBarProgressBar"/>
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        public StatusBarProgressBar(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            StatusBar.Progress(ref _cookie, 0, "", 0, 0);
        }

        /// <summary>
        /// Creates and displays an instance of the <see cref="StatusBarProgressBar"/>
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        /// <param name="text">A text label describing the action being performed</param>
        /// <param name="maxValue">The total number of steps to be performed</param>
        public StatusBarProgressBar(IServiceProvider serviceProvider, string text, int maxValue)
            : this(serviceProvider)
        {
            _text = text;
            _maxValue = maxValue;
            IsEnabled = true;
        }

        /// <summary>
        /// Whether the progress bar is visible or not
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                Update();
            }
        }

        /// <summary>
        /// A text label describing the action being performed
        /// </summary>
        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                    return;
                _text = value;
                if (_isEnabled)
                    Update();
            }
        }

        /// <summary>
        /// The current step number
        /// </summary>
        /// <seealso cref="MaxValue"/>
        public int Value
        {
            get => _value;
            set
            {
                value = Math.Max(0, Math.Min(MaxValue, value));
                if (_value == value)
                    return;
                _value = value;
                if (_isEnabled)
                    Update();
            }
        }

        /// <summary>
        /// The total number of steps to be performed
        /// </summary>
        /// <seealso cref="Value"/>
        public int MaxValue
        {
            get => _maxValue;
            set
            {
                value = Math.Max(0, value);
                if (_maxValue == value)
                    return;
                _maxValue = value;
                if (_isEnabled)
                    Update();
            }
        }

        void Update()
        {
            if (!_isEnabled && _cookie != 0)
            {
                StatusBar.Progress(ref _cookie, 0, "", 0, 0);
                return;
            }
            StatusBar.Progress(ref _cookie, 1, Text, (uint)Value, (uint)MaxValue);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsEnabled = false;
            base.Dispose(disposing);
        }

        void IProgress<float>.Report(float value)
        {
            Value = (int)(value * MaxValue);
        }

        void IProgress<int>.Report(int value)
        {
            Value = value;
        }
    }
}
