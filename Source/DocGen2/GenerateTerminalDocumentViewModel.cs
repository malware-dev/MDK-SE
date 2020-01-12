using System;
using System.IO;
using System.Threading.Tasks;
using Mal.DocGen2.Services;

namespace Mal.DocGen2
{
    public class GenerateTerminalDocumentViewModel : Model
    {
        string _outputPath;
        string _statusText;

        public GenerateTerminalDocumentViewModel()
        {
            RunNowCommand = new AsyncModelCommand(RunNow);
        }

        public AsyncModelCommand RunNowCommand { get; }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (value == _statusText) return;
                _statusText = value;
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
                await Terminals.Update(Path.Combine(Environment.CurrentDirectory, "terminal.cache"), Path.Combine(OutputPath, "List-Of-Terminal-Properties-And-Actions.md"), s => StatusText = s);
            }
            finally
            {
                RunNowCommand.IsEnabled = true;
            }
        }
    }
}