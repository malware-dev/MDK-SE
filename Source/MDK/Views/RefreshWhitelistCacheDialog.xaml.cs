using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views
{
    /// <summary>
    /// Interaction logic for RequestUpgradeDialog.xaml
    /// </summary>
    public partial class RefreshWhitelistCacheDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(RefreshWhitelistCacheDialogModel viewModel)
        {
            var dialog = new RefreshWhitelistCacheDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RequestUpgradeDialog"/>
        /// </summary>
        public RefreshWhitelistCacheDialog()
        {
            InitializeComponent();
        }

        void SetModel(RefreshWhitelistCacheDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((RefreshWhitelistCacheDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
