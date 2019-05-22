using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Markup;
using JetBrains.Annotations;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Represents a notification bar in the top of the Visual Studio work area.
    /// </summary>
    [ContentProperty("Spans")]
    public class NotificationBar : IVsInfoBarUIEvents
    {
        TaskCompletionSource<string> _tcs;
        uint _cookie;
        IVsInfoBarUIElement _element;
        string _result;

        /// <summary>
        /// Determines whether the bar has a close button.
        /// </summary>
        public bool HasCloseButton { get; set; } = true;

        /// <summary>
        /// Specifies one of the <see cref="KnownMonikers"/> images.
        /// </summary>
        public ImageMoniker ImageMoniker { get; set; } = KnownMonikers.StatusInformation;

        /// <summary>
        /// A list of text spans. Can also contain <see cref="NotificationAction"/>s.
        /// </summary>
        public List<NotificationTextSpan> Spans { get; } = new List<NotificationTextSpan>();

        /// <summary>
        /// A list of actions, which will be shown in a predefined way in the bar.
        /// </summary>
        public List<NotificationAction> Actions { get; } = new List<NotificationAction>();

        /// <summary>
        /// A timeout in seconds, after which the notification bar will be automatically closed.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Indicates whether this bar is currently being displayed.
        /// </summary>
        public bool IsShown => _element != null;

        /// <summary>
        /// Called when the bar has been closed.
        /// </summary>
        /// <param name="infoBarUIElement"></param>
        protected virtual void OnClosed(IVsInfoBarUIElement infoBarUIElement) { infoBarUIElement.Unadvise(_cookie); }

        /// <summary>
        /// Called when an action item in the bar has been called.
        /// </summary>
        /// <param name="infoBarUIElement"></param>
        /// <param name="actionItem"></param>
        protected virtual void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            if (actionItem is NotificationAction action)
            {
                _result = action.ResultCode;
                action.Click();
            }
        }

        /// <summary>
        /// The service provider used when showing a bar. This property will only be available as long as <see cref="IsShown"/> is true.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        /// <summary>
        /// Shows this bar in the Visual Studio environment.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public async Task<string> ShowAsync([NotNull] IServiceProvider serviceProvider)
        {
            if (IsShown)
                throw new InvalidOperationException("Bar is already shown");
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            if (serviceProvider.GetService(typeof(SVsShell)) is IVsShell shell)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;
                if (host == null)
                    return null;

                _tcs = new TaskCompletionSource<string>();
                _result = null;
                var infoBarModel = new InfoBarModel(Spans.Where(span => span.IsVisible).ToArray(), Actions.Where(action => action.IsVisible).ToArray(), ImageMoniker, HasCloseButton);
                var factory = serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
                _element = factory.CreateInfoBar(infoBarModel);
                _element.Advise(this, out _cookie);
                host.AddInfoBar(_element);
                if (Timeout > 0)
                {
                    if (await Task.WhenAny(_tcs.Task, Task.Delay(Timeout * 1000)) != _tcs.Task)
                    {
                        Close();
                    }
                }
                var response = await _tcs.Task;
                _tcs = null;
                _element = null;
                ServiceProvider = null;
                return response;
            }

            return null;
        }

        /// <summary>
        /// Signals this bar that it should close.
        /// </summary>
        public void Close()
        {
            if (_element == null)
                return;
            _element.Close();
            _tcs.SetResult(_result);
        }

        void IVsInfoBarUIEvents.OnClosed(IVsInfoBarUIElement infoBarUIElement) => OnClosed(infoBarUIElement);
        void IVsInfoBarUIEvents.OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem) => OnActionItemClicked(infoBarUIElement, actionItem);
    }
}
