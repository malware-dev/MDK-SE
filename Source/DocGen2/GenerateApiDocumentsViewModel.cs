using System;
using System.IO;
using System.Threading.Tasks;
using Mal.DocGen2.Services;

namespace Mal.DocGen2
{
    public class GenerateApiDocumentsViewModel : Model
    {
        string _outputPath;
        bool _isWorking;

        public GenerateApiDocumentsViewModel()
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
                await ProgrammableBlockApi.Update(Path.Combine(Environment.CurrentDirectory, "whitelist.cache"), Path.Combine(OutputPath, "api"));
            }
            finally
            {
                IsWorking = false;
                RunNowCommand.IsEnabled = true;
            }
        }
    }
}