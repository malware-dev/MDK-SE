using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mal.DocGen2
{
    public class MainWindowModel: Model
    {
        string _outputPath;

        public MainWindowModel()
        {
            OutputPath = @"D:\Repos\SpaceEngineers\MDK-SE.wiki";
        }

        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath) return;
                _outputPath = value;
                this.GenerateTerminalDocumentViewModel.OutputPath = value;
                this.GenerateApiDocumentsViewModel.OutputPath = value;
                this.GenerateTypeDefListingViewModel.OutputPath = value;
                OnPropertyChanged();
            }
        }

        public GenerateApiDocumentsViewModel GenerateApiDocumentsViewModel { get; } = new GenerateApiDocumentsViewModel();
        public WhitelistAndTerminalCachesViewModel WhitelistAndTerminalCachesViewModel { get; } = new WhitelistAndTerminalCachesViewModel();
        public GenerateTerminalDocumentViewModel GenerateTerminalDocumentViewModel { get; } = new GenerateTerminalDocumentViewModel();
        public GenerateTypeDefListingViewModel GenerateTypeDefListingViewModel { get; } = new GenerateTypeDefListingViewModel();
    }
}
