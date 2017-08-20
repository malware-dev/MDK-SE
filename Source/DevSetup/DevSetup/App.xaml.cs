using System.Windows;

namespace Malware.DevSetup
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_OnStartup(object sender, StartupEventArgs e)
        {
            MainWindow = new MainWindow(new MainWindowViewModel());
            MainWindow.Show();
        }
    }
}
