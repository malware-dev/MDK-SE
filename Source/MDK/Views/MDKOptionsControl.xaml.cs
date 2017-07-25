using System.Windows.Controls;
using MDK.Services;

namespace MDK.Views
{
    /// <summary>
    /// Interaction logic for MDKOptionsWindow.xaml
    /// </summary>
    public partial class MDKOptionsControl : UserControl
    {
        /// <summary>
        /// Creates a new instance of <see cref="MDKOptionsControl"/>
        /// </summary>
        public MDKOptionsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the associated Options.
        /// </summary>
        /// <remarks>The options function as this view's view mode.</remarks>
        public MDKOptions Options
        {
            get => (MDKOptions)Host.DataContext;
            set => Host.DataContext = value;
        }
    }
}
