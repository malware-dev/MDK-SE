using System;
using System.IO;
using System.Threading.Tasks;
using Mal.DocGen2.Services;
using Malware.MDKUtilities;

namespace Mal.DocGen2
{
    public class GenerateTypeDefListingViewModel : Model
    {
        bool _isWorking;
        string _outputPath;

        public GenerateTypeDefListingViewModel()
        {
            RunNowCommand = new AsyncModelCommand(RunNow);
        }

        public AsyncModelCommand RunNowCommand { get; }

        public bool IsWorking
        {
            get => _isWorking;
            set
            {
                if (value == _isWorking) return;
                _isWorking = value;
                OnPropertyChanged();
            }
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath) return;
                _outputPath = value;
                OnPropertyChanged();
            }
        }

        public async Task RunNow()
        {
            try
            {
                RunNowCommand.IsEnabled = false;
                IsWorking = true;
                var spaceEngineers = new SpaceEngineers();
                await TypeDefinitions.UpdateAsync(spaceEngineers.GetInstallPath(@"Content\Data"), Path.Combine(OutputPath, "Type-Definition-Listing.md"), spaceEngineers.GetInstallPath(@"Bin64"));
            }
            finally
            {
                IsWorking = false;
                RunNowCommand.IsEnabled = true;
            }
        }
    }
}