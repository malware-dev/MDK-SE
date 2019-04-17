using System.Threading.Tasks;

namespace MDK.Views.BugReports
{
    /// <summary>
    /// The view model for <see cref="ErrorDialog"/>
    /// </summary>
    public class ErrorDialogModel : DialogViewModel
    {
        string _title;
        string _log;
        string _description;

        /// <summary>
        /// The error description
        /// </summary>
        public string Description
        {
            get => _description;
            set
            {
                if (value == _description)
                    return;
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The error log
        /// </summary>
        public string Log
        {
            get => _log;
            set
            {
                if (value == _log)
                    return;
                _log = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The title
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                if (value == _title)
                    return;
                _title = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc />
        protected override bool OnSave() => true;
    }
}
