using System;
using System.Windows;
using MDK.Resources;
using MDK.Views.Options;
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
        public ProjectHealthDialog() { InitializeComponent(); }

        void OnUpgradeCompleted(object sender, EventArgs e) { MessageBox.Show(this, Text.ProjectHealthDialog_OnUpgradeCompleted_BackupsStoredMessage, "Projects Upgraded/Repaired", MessageBoxButton.OK, MessageBoxImage.Information); }

        void SetModel(ProjectHealthDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
            viewModel.UpgradeCompleted += OnUpgradeCompleted;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            var viewModel = ((ProjectHealthDialogModel)Host.DataContext);
            viewModel.Closing -= OnModelClosing;
            viewModel.UpgradeCompleted -= OnUpgradeCompleted;
            DialogResult = e.State;
            Close();
        }
    }
}
