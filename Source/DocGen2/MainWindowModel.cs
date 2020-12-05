namespace Mal.DocGen2
{
    public class MainWindowModel: Model
    {
        private string _outputPath;

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
                GenerateTerminalDocumentViewModel.OutputPath = value;
                GenerateApiDocumentsViewModel.OutputPath = value;
                GenerateTypeDefListingViewModel.OutputPath = value;
                GenerateSpritesViewModel.OutputPath = value;
                OnPropertyChanged();
            }
        }

        public GenerateApiDocumentsViewModel GenerateApiDocumentsViewModel { get; } = new GenerateApiDocumentsViewModel();
        public WhitelistAndTerminalCachesViewModel WhitelistAndTerminalCachesViewModel { get; } = new WhitelistAndTerminalCachesViewModel();
        public GenerateTerminalDocumentViewModel GenerateTerminalDocumentViewModel { get; } = new GenerateTerminalDocumentViewModel();
        public GenerateTypeDefListingViewModel GenerateTypeDefListingViewModel { get; } = new GenerateTypeDefListingViewModel();
        public GenerateSpritesViewModel GenerateSpritesViewModel { get; } = new GenerateSpritesViewModel();
    }
}