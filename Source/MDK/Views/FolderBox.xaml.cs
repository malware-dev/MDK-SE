using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;

namespace MDK.Views
{
    /// <summary>
    ///     Interaction logic for FolderBox.xaml
    /// </summary>
    public partial class FolderBox : UserControl
    {
        /// <summary>
        /// The dependency property backend for <see cref="DialogTitle"/>
        /// </summary>
        public static readonly DependencyProperty DialogTitleProperty = DependencyProperty.Register(
            "DialogTitle", typeof(string), typeof(FolderBox), new FrameworkPropertyMetadata(default(string)));

        /// <summary>
        /// The dependency property backend for <see cref="SelectedFolder"/>
        /// </summary>
        public static readonly DependencyProperty SelectedFolderProperty = DependencyProperty.Register(
            "SelectedFolder", typeof(string), typeof(FolderBox), new FrameworkPropertyMetadata(default(string), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, NotifySelectedFolderChanged));

        static void NotifySelectedFolderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((FolderBox)sender).OnSelectedFolderChanged(e);
        }

        /// <summary>
        /// Creates a new instance of <see cref="FolderBox"/>
        /// </summary>
        public FolderBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The title of the dialog shown when a user presses the browse button.
        /// </summary>
        public string DialogTitle
        {
            get => (string)GetValue(DialogTitleProperty);
            set => SetValue(DialogTitleProperty, value);
        }

        /// <summary>
        /// The currently selected folder path
        /// </summary>
        public string SelectedFolder
        {
            get => (string)GetValue(SelectedFolderProperty);
            set => SetValue(SelectedFolderProperty, value);
        }

        /// <summary>
        /// Called whenever the <see cref="SelectedFolder"/> property changes
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSelectedFolderChanged(DependencyPropertyChangedEventArgs e)
        { }

        void OnBrowseButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = DialogTitle,
                UseDescriptionForTitle = true,
                SelectedPath = SelectedFolder,
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == true)
                SelectedFolder = dialog.SelectedPath;
        }
    }
}
