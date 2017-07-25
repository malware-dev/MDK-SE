using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views
{
    /// <summary>
    /// Interaction logic for ScriptOptionsDialog.xaml
    /// </summary>
    public partial class ScriptOptionsDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(ScriptOptionsDialogModel viewModel)
        {
            var dialog = new ScriptOptionsDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="RequestUpgradeDialog"/>
        /// </summary>
        public ScriptOptionsDialog()
        {
            InitializeComponent();
        }

        void SetModel(ScriptOptionsDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((ScriptOptionsDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
