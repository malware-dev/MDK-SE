using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.UpdateDetection
{
    /// <summary>
    /// Interaction logic for RequestUpgradeDialog.xaml
    /// </summary>
    public partial class UpdateDetectedDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(UpdateDetectedDialogModel viewModel)
        {
            var dialog = new UpdateDetectedDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="UpdateDetectedDialog"/>
        /// </summary>
        public UpdateDetectedDialog()
        {
            InitializeComponent();
        }

        void SetModel(UpdateDetectedDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((UpdateDetectedDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
