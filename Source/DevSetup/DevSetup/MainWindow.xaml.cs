using System.Windows;

namespace Malware.DevSetup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public MainWindow(MainWindowViewModel model)
            : this()
        {
            Host.DataContext = model;
            model.Closing += OnViewModelClosing;
        }

        public MainWindowViewModel ViewModel => (MainWindowViewModel)Host.DataContext;

        void OnViewModelClosing(object sender, DialogClosingEventArgs e)
        {
            Close();
        }
    }
}