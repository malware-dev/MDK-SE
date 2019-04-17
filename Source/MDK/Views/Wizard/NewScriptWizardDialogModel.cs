using System.Threading.Tasks;
using MDK.Resources;

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
        bool _trimTypes;

        /// <summary>
        /// Creates a new instance of <see cref="NewScriptWizardDialogModel"/>
        /// </summary>
        public NewScriptWizardDialogModel()
        {
            GameBinPath = null;
            OutputPath = null;
        }

        /// <summary>
        /// The path to Space Engineer's Bin64 folder
        /// </summary>
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                value = value?.Trim().TrimEnd('\\');
                ClearErrors(nameof(GameBinPath));
                if (string.IsNullOrEmpty(value))
                {
                    AddError(nameof(GameBinPath), Text.NewScriptWizardDialogModel_GameBinPath_GameBinariesRequired);
                    return;
                }

                if (value == _gameBinPath)
                    return;
                _gameBinPath = value.TrimEnd('\\');
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
                value = value?.Trim().TrimEnd('\\');
                ClearErrors(nameof(OutputPath));
                if (string.IsNullOrEmpty(value))
                {
                    AddError(nameof(OutputPath), Text.NewScriptWizardDialogModel_OutputPath_OutputPathRequired);
                    return;
                }

                if (value == _outputPath)
                    return;
                _outputPath = value.TrimEnd('\\');
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether unused type definitions will be removed from the script
        /// </summary>
        public bool TrimTypes
        {
            get => _trimTypes;
            set
            {
                if (value == _trimTypes)
                    return;
                _trimTypes = value;
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
        protected override bool OnSave()
        {
            if (!IsValid)
                return false;
            return true;
        }
    }
}
