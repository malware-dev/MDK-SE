using System;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Provides access to the animated icon on the Visual Studio status bar
    /// </summary>
    public class StatusBarAnimation : StatusBarUtility
    {
        bool _isEnabled;
        Animation _animation;
        Animation _currentAnimation;

        /// <summary>
        /// Creates an instance of the <see cref="StatusBarAnimation"/>
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        public StatusBarAnimation(IServiceProvider serviceProvider) : base(serviceProvider)
        { }

        /// <summary>
        /// Creates and shows an instance of the <see cref="StatusBarAnimation"/>
        /// </summary>
        /// <param name="serviceProvider">The Visual Studio service provider</param>
        /// <param name="animation">The animation to display</param>
        public StatusBarAnimation(IServiceProvider serviceProvider, Animation animation)
            : base(serviceProvider)
        {
            Animation = animation;
            IsEnabled = true;
        }

        /// <summary>
        /// The animation to display
        /// </summary>
        public Animation Animation
        {
            get => _animation;
            set
            {
                _animation = value;
                if (_isEnabled)
                {
                    IsEnabled = false;
                    IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Determines whether the animation should be displayed at this time
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;
                _isEnabled = value;
                if (!_isEnabled)
                {
                    object icon = (short)_currentAnimation;
                    StatusBar.Animation(0, ref icon);
                }
                else
                {
                    _currentAnimation = _animation;
                    object icon = (short)_currentAnimation;
                    StatusBar.Animation(1, ref icon);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsEnabled = false;
            }
            base.Dispose(disposing);
        }
    }
}
