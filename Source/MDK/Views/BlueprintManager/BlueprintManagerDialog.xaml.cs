using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using MDK.Resources;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

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
        /// Creates a new instance of the <see cref="BlueprintManagerDialog"/>
        /// </summary>
        public BlueprintManagerDialog()
        {
            InitializeComponent();
            blueprintsListBox.Items.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            SelectFirstSignificant();
            blueprintsListBox.Focus();
        }

        void SetModel(BlueprintManagerDialogModel viewModel)
        {
            Host.DataContext = viewModel;
            viewModel.DeletingBlueprint += OnDeletingBlueprint;
            viewModel.MessageRequested += OnMessageRequested;
            viewModel.Closing += OnModelClosing;
        }

        void SelectFirstSignificant()
        {
            var firstSignificantModel = blueprintsListBox.Items.Cast<BlueprintModel>().FirstOrDefault(blueprint => blueprint.IsSignificant) ?? blueprintsListBox.Items.Cast<BlueprintModel>().FirstOrDefault();
            if (firstSignificantModel != null)
            {
                blueprintsListBox.ScrollIntoView(firstSignificantModel);
                blueprintsListBox.SelectedItem = firstSignificantModel;
            }
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

        void OnDeletingBlueprint(object sender, DeleteBlueprintEventArgs e)
        {
            e.Cancel = MessageBox.Show(this, string.Format(Text.BlueprintManagerDialog_OnDeletingBlueprint_Description, e.Blueprint.Name), Text.BlueprintManagerDialog_OnDeletingBlueprint_Title, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No;
        }

        void OnModelClosing(object sender, DialogClosingEventArgs e)
        {
            var viewModel = (BlueprintManagerDialogModel)Host.DataContext;
            viewModel.DeletingBlueprint -= OnDeletingBlueprint;
            viewModel.MessageRequested -= OnMessageRequested;
            viewModel.Closing += OnModelClosing;
            DialogResult = e.State;
            Close();
        }

        async void EditBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var element = (FrameworkElement)sender;
            if (!element.IsVisible)
                return;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var textBox = (TextBox)sender;
            textBox.SelectAll();
            textBox.Focus();
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
