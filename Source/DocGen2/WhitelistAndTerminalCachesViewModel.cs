using System;
using System.Threading.Tasks;
using Mal.DocGen2.Services;

namespace Mal.DocGen2
{
    public class WhitelistAndTerminalCachesViewModel : Model
    {
        string _statusText;

        public WhitelistAndTerminalCachesViewModel()
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

        public async Task RunNow()
        {
            try
            {
                RunNowCommand.IsEnabled = false;
                await WhitelistAndTerminalCaches.Update(Environment.CurrentDirectory, s => StatusText = s);
            }
            finally
            {
                RunNowCommand.IsEnabled = true;
            }
        }
    }
}