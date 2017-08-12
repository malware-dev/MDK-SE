using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.Wizard
{
    /// <summary>
    /// Interaction logic for NewScriptWizardDialog.xaml
    /// </summary>
    public partial class NewScriptWizardDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(NewScriptWizardDialogModel viewModel)
        {
            var dialog = new NewScriptWizardDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectIntegrity.RequestUpgradeDialog"/>
        /// </summary>
        public NewScriptWizardDialog()
        {
            InitializeComponent();
        }

        void SetModel(NewScriptWizardDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((NewScriptWizardDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
