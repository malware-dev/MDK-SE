using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using JetBrains.Annotations;
using MDK.Resources;

namespace MDK.Views.BlueprintManager
{
    /// <summary>
    /// Represents a single blueprint
    /// </summary>
    public class BlueprintModel : Model, IEditableObject, INotifyDataErrorInfo
    {
        bool _isSignificant;
        string _name;
        string _editedName;
        bool _isBeingEdited;
        bool _editedNameIsValid;
        DirectoryInfo _directory;
        string _renameError;

        /// <summary>
        /// Creates a new instance of the blueprint model
        /// </summary>
        /// <param name="thumbnail"></param>
        /// <param name="directory"></param>
        /// <param name="isSignificant"></param>
        public BlueprintModel(ImageSource thumbnail, [NotNull] DirectoryInfo directory, bool isSignificant)
        {
            Thumbnail = thumbnail;
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Name = _directory.Name;
            IsSignificant = isSignificant;
        }

        /// <inheritdoc cref="INotifyDataErrorInfo.ErrorsChanged"/>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// An optional thumbnail
        /// </summary>
        public ImageSource Thumbnail { get; private set; }

        /// <summary>
        /// The name of this thumbnail
        /// </summary>
        public string Name
        {
            get => _name;
            private set
            {
                if (value == _name)
                    return;
                _name = value;
                EditedName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets and sets the name when it's being edited.
        /// </summary>
        public string EditedName
        {
            get => _editedName;
            set
            {
                if (value == _editedName)
                    return;
                _editedName = value;
                _renameError = null;
                EditedNameIsValid = !string.IsNullOrEmpty(value) && value.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
                OnPropertyChanged();
            }
        }

        bool EditedNameIsValid
        {
            get => _editedNameIsValid;
            set
            {
                if (_editedNameIsValid == value)
                    return;
                _editedNameIsValid = value;
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(EditedName)));
            }
        }

        /// <summary>
        /// Determines whether this blueprint is currently in edit mode.
        /// </summary>
        public bool IsBeingEdited
        {
            get => _isBeingEdited;
            private set
            {
                if (value == _isBeingEdited)
                    return;
                _isBeingEdited = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this particular blueprint is significant in any way. Is used to emphasize newly deployed scripts, for instance.
        /// </summary>
        public bool IsSignificant
        {
            get => _isSignificant;
            set
            {
                if (value == _isSignificant)
                    return;
                _isSignificant = value;
                OnPropertyChanged();
            }
        }

        /// <inheritdoc cref="INotifyDataErrorInfo.HasErrors"/>
        public bool HasErrors => !EditedNameIsValid;

        /// <inheritdoc cref="IEditableObject.BeginEdit"/>
        public void BeginEdit()
        {
            _renameError = null;
            IsBeingEdited = true;
        }

        /// <inheritdoc cref="IEditableObject.EndEdit"/>
        public void EndEdit()
        {
            if (HasErrors)
                return;
            if (Name != EditedName)
            {
                try
                {
                    var newPath = Path.Combine(_directory.Parent?.FullName ?? ".", EditedName);
                    _directory.MoveTo(newPath);
                }
                catch (Exception exception)
                {
                    _renameError = exception.Message;
                    EditedNameIsValid = false;
                }
            }
            IsBeingEdited = false;
            Name = EditedName;
        }

        /// <inheritdoc cref="IEditableObject.CancelEdit"/>
        public void CancelEdit()
        {
            _renameError = null;
            EditedName = Name;
            IsBeingEdited = false;
        }

        /// <inheritdoc cref="INotifyDataErrorInfo.GetErrors"/>
        public IEnumerable GetErrors(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(EditedName):
                    if (!EditedNameIsValid)
                        yield return _renameError ?? string.Format(Text.BlueprintModel_GetErrors_InvalidName, string.Join(" ", Path.GetInvalidFileNameChars().Where(ch => !char.IsControl(ch))));
                    break;
            }
        }

        /// <summary>
        /// Deletes this blueprint
        /// </summary>
        public void Delete()
        {
            try
            {
                _directory.Delete(true);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
