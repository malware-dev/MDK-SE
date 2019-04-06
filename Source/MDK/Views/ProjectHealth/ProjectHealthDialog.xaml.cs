using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.ProjectHealth
{
    /// <summary>
    /// Interaction logic for ProjectHealthDialog.xaml
    /// </summary>
    public partial class ProjectHealthDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(ProjectHealthDialogModel viewModel)
        {
            var dialog = new ProjectHealthDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectHealthDialog"/>
        /// </summary>
        public ProjectHealthDialog()
        {
            InitializeComponent();
        }

        void SetModel(ProjectHealthDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((ProjectHealthDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }
    }
}
