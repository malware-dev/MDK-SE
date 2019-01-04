using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
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
        string _renameError;

        /// <summary>
        /// Creates a new instance of the blueprint model
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="thumbnail"></param>
        /// <param name="directory"></param>
        /// <param name="isSignificant"></param>
        public BlueprintModel([NotNull] BlueprintManagerDialogModel manager, ImageSource thumbnail, [NotNull] DirectoryInfo directory, bool isSignificant)
        {
            Manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Thumbnail = thumbnail;
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Name = Directory.Name;
            IsSignificant = isSignificant;
        }

        /// <inheritdoc cref="INotifyDataErrorInfo.ErrorsChanged"/>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// The script directory
        /// </summary>
        public DirectoryInfo Directory { get; }

        /// <summary>
        /// Gets the script blueprint manager model this blueprint belongs to
        /// </summary>
        public BlueprintManagerDialogModel Manager { get; }

        /// <summary>
        /// An optional thumbnail
        /// </summary>
        public ImageSource Thumbnail { get; }

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
            if (IsBeingEdited)
                return;
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
                    var newPath = Path.Combine(Directory.Parent?.FullName ?? ".", EditedName);
                    Directory.MoveTo(newPath);
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
                Directory.Delete(true);
            }
            catch (Exception e)
            {
                Manager.SendMessage(Text.BlueprintModel_Delete_Error, string.Format(Text.BlueprintModel_Error_Description, Name, e.Message), MessageEventType.Error);
            }
        }

        /// <summary>
        /// Opens the target folder of this blueprint
        /// </summary>
        public void OpenFolder()
        {
            if (IsBeingEdited)
                CancelEdit();
            var process = new Process
            {
                StartInfo =
                {
                    FileName = Directory.FullName
                }
            };
            process.Start();
        }
    }
}
