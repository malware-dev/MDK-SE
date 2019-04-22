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


        void SetModel(ProjectHealthDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.Closing += OnModelClosing;
            viewModel.MessageRequested += OnMessageRequested;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            var viewModel = ((ProjectHealthDialogModel)Host.DataContext);
            viewModel.Closing -= OnModelClosing;
            viewModel.MessageRequested -= OnMessageRequested;
            DialogResult = e.State;
            Close();
        }

        void OnMessageRequested(object sender, MessageEventArgs e)
        {
            MessageBoxImage image;
            MessageBoxButton buttons;
            switch (e.EventType)
            {
                case MessageEventType.Confirm:
                    image = MessageBoxImage.Question;
                    buttons = MessageBoxButton.YesNo;
                    break;
                case MessageEventType.Warning:
                    image = MessageBoxImage.Warning;
                    buttons = MessageBoxButton.OK;
                    break;
                case MessageEventType.Error:
                    image = MessageBoxImage.Error;
                    buttons = MessageBoxButton.OK;
                    break;
                default:
                    image = MessageBoxImage.Information;
                    buttons = MessageBoxButton.OK;
                    break;
            }
            var response = MessageBox.Show(this, e.Description, e.Title, buttons, image);
            switch (response)
            {
                case MessageBoxResult.Cancel:
                case MessageBoxResult.No:
                    e.Cancel = true;
                    break;
            }
        }
    }
}
