using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.VersionDetection
{
    /// <summary>
    /// Interaction logic for RequestUpgradeDialog.xaml
    /// </summary>
    public partial class VisualStudioVersionDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(VisualStudioVersionDialogModel viewModel)
        {
            var dialog = new VisualStudioVersionDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectIntegrity.RequestUpgradeDialog"/>
        /// </summary>
        public VisualStudioVersionDialog()
        {
            InitializeComponent();
        }

        void SetModel(VisualStudioVersionDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((VisualStudioVersionDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
