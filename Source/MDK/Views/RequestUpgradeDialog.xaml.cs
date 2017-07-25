using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views
{
    /// <summary>
    /// Interaction logic for RequestUpgradeDialog.xaml
    /// </summary>
    public partial class RequestUpgradeDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(RequestUpgradeDialogModel viewModel)
        {
            var dialog = new RequestUpgradeDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RequestUpgradeDialog"/>
        /// </summary>
        public RequestUpgradeDialog()
        {
            InitializeComponent();
        }

        void SetModel(RequestUpgradeDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((RequestUpgradeDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
