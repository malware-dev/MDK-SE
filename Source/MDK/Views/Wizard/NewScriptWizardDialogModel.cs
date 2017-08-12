namespace MDK.Views.Wizard
{
    /// <summary>
    /// The view model for <see cref="NewScriptWizardDialog"/>
    /// </summary>
    public class NewScriptWizardDialogModel : DialogViewModel
    {
        string _gameBinPath;
        string _outputPath;
        bool _minify;
        bool _promoteMDK;

        /// <summary>
        /// The path to Space Engineer's Bin64 folder
        /// </summary>
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (value == _gameBinPath)
                    return;
                _gameBinPath = value?.TrimEnd('\\');
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The path where the generated scripts will be placed
        /// </summary>
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath)
                    return;
                _outputPath = value?.TrimEnd('\\');
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the script minifier will be enabled on this script
        /// </summary>
        public bool Minify
        {
            get => _minify;
            set
            {
                if (value == _minify)
                    return;
                _minify = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the script thumbnail will promote MDK
        /// </summary>
        public bool PromoteMDK
        {
            get => _promoteMDK;
            set
            {
                if (value == _promoteMDK)
                    return;
                _promoteMDK = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        /// <returns></returns>
        protected override bool OnSave()
        {
            return true;
        }
    }
}
