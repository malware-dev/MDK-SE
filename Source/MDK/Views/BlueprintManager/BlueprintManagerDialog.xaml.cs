using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MDK.Resources;
using Microsoft.VisualStudio.PlatformUI;

namespace MDK.Views.BlueprintManager
{
    /// <summary>
    /// Interaction logic for BlueprintManagerDialog.xaml
    /// </summary>
    public partial class BlueprintManagerDialog : DialogWindow
    {
        /// <summary>
        /// Shows this dialog with the provided view model.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        public static bool? ShowDialog(BlueprintManagerDialogModel viewModel)
        {
            var dialog = new BlueprintManagerDialog();
            dialog.SetModel(viewModel);
            return dialog.ShowModal();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProjectIntegrity.RequestUpgradeDialog"/>
        /// </summary>
        public BlueprintManagerDialog()
        {
            InitializeComponent();
            blueprintsListBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        void SetModel(BlueprintManagerDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.DeletingBlueprint += OnDeletingBlueprint;
            viewModel.MessageRequested += ViewModelOnMessageRequested;
            viewModel.Closing += OnModelClosing;
        }

        void ViewModelOnMessageRequested(object sender, MessageEventArgs e)
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

        void OnDeletingBlueprint(object sender, DeleteBlueprintEventArgs e)
        {
            e.Cancel = MessageBox.Show(this, string.Format(Text.BlueprintManagerDialog_OnDeletingBlueprint_Description, e.Blueprint.Name), Text.BlueprintManagerDialog_OnDeletingBlueprint_Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            ((BlueprintManagerDialogModel)Host.DataContext).Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }

        void EditBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            if (!element.IsVisible)
                return;
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var textBox = (TextBox)sender;
                textBox.SelectAll();
                textBox.Focus();
            }));
        }

        void EditBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            ((BlueprintModel)((FrameworkElement)sender).DataContext).EndEdit();
        }

        void EditBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    ((BlueprintModel)((FrameworkElement)sender).DataContext).CancelEdit();
                    break;

                case Key.Enter:
                    ((BlueprintModel)((FrameworkElement)sender).DataContext).EndEdit();
                    break;
            }
        }
    }
}
