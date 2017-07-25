using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using MDK.Views;
using Microsoft.VisualStudio.Shell;

namespace MDK.Services
{
    [CLSCompliant(false)]
    [ComVisible(true)]
    public class MDKOptions : UIElementDialogPage, INotifyPropertyChanged
    {
        string _gameBinPath;
        bool _minify;
        string _outputPath;
        bool _useManualGameBinPath;
        bool _useManualOutputPath;

        public MDKOptions()
        {
            SpaceEngineers = new SpaceEngineers();

            ((MDKOptionsControl)Child).Options = this;
            _gameBinPath = SpaceEngineers.GetInstallPath("Bin64");
            _outputPath = SpaceEngineers.GetDataPath("IngameScripts", "local");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public SpaceEngineers SpaceEngineers { get; }

        [Category("MDK/SE")]
        [DisplayName("Use manual binary path")]
        [Description("If checked, use the manually specified binary path")]
        public bool UseManualGameBinPath
        {
            get => _useManualGameBinPath;
            set
            {
                if (_useManualGameBinPath == value)
                    return;
                _useManualGameBinPath = value;
                OnPropertyChanged();
            }
        }

        [Category("MDK/SE")]
        [DisplayName("Space Engineers binary path")]
        [Description("A manual assignment of the path to the binary files of Space Engineers.")]
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (_gameBinPath == value)
                    return;
                _gameBinPath = value;
                if (string.IsNullOrWhiteSpace(_gameBinPath))
                    _gameBinPath = SpaceEngineers.GetInstallPath("Bin64");
                OnPropertyChanged();
            }
        }

        [Category("MDK/SE")]
        [DisplayName("Use manual output path")]
        [Description("If checked, use the manually specified output path")]
        public bool UseManualOutputPath
        {
            get => _useManualOutputPath;
            set
            {
                if (_useManualOutputPath == value)
                    return;
                _useManualOutputPath = value;
                OnPropertyChanged();
            }
        }

        [Category("MDK/SE")]
        [DisplayName("Script Output Path")]
        [Description("A manual assignment of the path to the default output path for the final scripts.")]
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (_outputPath == value)
                    return;
                _outputPath = value;
                if (string.IsNullOrWhiteSpace(_outputPath))
                    _outputPath = SpaceEngineers.GetDataPath("IngameScripts", "local");
                OnPropertyChanged();
            }
        }

        public bool Minify
        {
            get => _minify;
            set
            {
                if (_minify == value)
                    return;
                _minify = value;
                OnPropertyChanged();
            }
        }

        protected sealed override UIElement Child { get; } = new MDKOptionsControl();

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
