using Malware.MDKUtilities;
using Microsoft.Win32;

namespace Malware.DevSetup
{
    public class MainWindowViewModel : DialogViewModel
    {
        const string RegistryKey = @"HKEY_CURRENT_USER\Software\Malware\MDK";
        const string RegistryValueName = "SEBinPath";
        string _bin64Path;

        public MainWindowViewModel()
        {
            AutoDetectCommand = new ModelCommand(AutoDetect);

            AutoDetect();
            Bin64Path = (string)Registry.GetValue(RegistryKey, RegistryValueName, Bin64Path);
        }

        public string Bin64Path
        {
            get => _bin64Path;
            set
            {
                if (value == _bin64Path)
                    return;
                _bin64Path = value;
                OnPropertyChanged();
            }
        }

        public ModelCommand AutoDetectCommand { get; }

        void AutoDetect()
        {
            var se = new SpaceEngineers();
            Bin64Path = se.GetInstallPath("Bin64");
        }

        protected override bool OnSave()
        {
            var path = Bin64Path?.TrimEnd('\\') ?? "";
            Registry.SetValue(RegistryKey, RegistryValueName, path);
            return true;
        }
    }
}