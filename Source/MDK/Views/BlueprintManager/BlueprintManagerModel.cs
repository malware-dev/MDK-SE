using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MDK.Resources;

namespace MDK.Views.BlueprintManager
{
    /// <summary>
    /// The view model for <see cref="BlueprintManagerDialog"/>
    /// </summary>
    public class BlueprintManagerDialogModel : DialogViewModel
    {
        static readonly string[] ThumbFileNames = {"thumb.png", "thumb.jpg", "thumb.jpeg"};

        string _blueprintPath;
        BlueprintModel _selectedBlueprint;
        HashSet<string> _significantBlueprints;
        string _customDescription;

        /// <summary>
        /// Creates an instance of <see cref="BlueprintManagerDialogModel"/>
        /// </summary>
        public BlueprintManagerDialogModel()
        {
            DeleteCommand = new ModelCommand(Delete, false);
            RenameCommand = new ModelCommand(Rename, false);
            OpenFolderCommand = new ModelCommand(OpenFolder, false);
            CopyToClipboardCommand = new ModelCommand(CopyToClipboard, false);
        }

        /// <summary>
        /// Creates an instance of <see cref="BlueprintManagerDialogModel"/>
        /// </summary>
        /// <param name="customDescription"></param>
        /// <param name="blueprintPath"></param>
        /// <param name="significantBlueprints"></param>
        public BlueprintManagerDialogModel(string customDescription, string blueprintPath, IEnumerable<string> significantBlueprints)
            : this()
        {
            _significantBlueprints = new HashSet<string>(significantBlueprints, StringComparer.CurrentCultureIgnoreCase);

            CustomDescription = customDescription;
            BlueprintPath = blueprintPath;
        }

        /// <summary>
        /// Occurs when a message has been sent which a user should respond to
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageRequested;

        /// <summary>
        /// Called before deleting a blueprint to allow UI confirmation
        /// </summary>
        public event EventHandler<DeleteBlueprintEventArgs> DeletingBlueprint;

        /// <summary>
        /// Called when all blueprints have been loaded
        /// </summary>
        public event EventHandler BlueprintsLoaded;

        /// <summary>
        /// A list of all available blueprints
        /// </summary>
        public ObservableCollection<BlueprintModel> Blueprints { get; } = new ObservableCollection<BlueprintModel>();

        /// <summary>
        /// Indicates the currently selected blueprint
        /// </summary>
        public BlueprintModel SelectedBlueprint
        {
            get => _selectedBlueprint;
            set
            {
                if (Equals(value, _selectedBlueprint))
                    return;
                _selectedBlueprint = value;
                var hasSelection = _selectedBlueprint != null;
                RenameCommand.IsEnabled = hasSelection;
                DeleteCommand.IsEnabled = hasSelection;
                OpenFolderCommand.IsEnabled = hasSelection;
                CopyToClipboardCommand.IsEnabled = hasSelection;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A command to rename the currently selected blueprint
        /// </summary>
        public ModelCommand RenameCommand { get; }

        /// <summary>
        /// A command to delete the currently selected blueprint
        /// </summary>
        public ModelCommand DeleteCommand { get; }

        /// <summary>
        /// A command to open the target folder of a selected blueprint
        /// </summary>
        public ModelCommand OpenFolderCommand { get; set; }

        /// <summary>
        /// A command to copy the selected script to the clipboard.
        /// </summary>
        public ModelCommand CopyToClipboardCommand { get; set; }

        /// <summary>
        /// A custom description to show at the top of the dialog.
        /// </summary>
        public string CustomDescription
        {
            get => _customDescription;
            set
            {
                if (value == _customDescription)
                    return;
                _customDescription = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The blueprint path where the scripts are stored (equivalent to the output path of the script generator)
        /// </summary>
        public string BlueprintPath
        {
            get => _blueprintPath;
            set
            {
                if (value == _blueprintPath)
                    return;
                _blueprintPath = value;
                LoadBlueprints();
                OnPropertyChanged();
            }
        }

        void OpenFolder()
        {
            var item = SelectedBlueprint;
            item?.OpenFolder();
        }

        void Rename()
        {
            var item = SelectedBlueprint;
            item?.BeginEdit();
        }

        void CopyToClipboard()
        {
            try
            {
                var item = SelectedBlueprint;
                var fileInfo = new FileInfo(Path.Combine(item.Directory.FullName, "script.cs"));
                if (fileInfo.Exists)
                {
                    var script = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);
                    Clipboard.SetText(script, TextDataFormat.UnicodeText);
                    SendMessage(Text.BlueprintManagerDialogModel_CopyToClipboard_Copy, Text.BlueprintManagerDialogModel_CopyToClipboard_Copy_Description, MessageEventType.Info);
                }
            }
            catch (Exception e)
            {
                SendMessage(Text.BlueprintManagerDialogModel_CopyToClipboard_Copy_Error, string.Format(Text.BlueprintManagerDialogModel_CopyToClipboard_Copy_Error_Description, e.Message), MessageEventType.Error);
            }
        }

        void Delete()
        {
            var item = SelectedBlueprint;
            if (item == null)
                return;
            if (item.IsBeingEdited)
                item.CancelEdit();

            var args = new DeleteBlueprintEventArgs(item);
            DeletingBlueprint?.Invoke(this, args);
            if (args.Cancel)
                return;

            try
            {
                item.Delete();
                Blueprints.Remove(item);
            }
            catch (Exception e)
            {
                SendMessage(Text.BlueprintManagerDialogModel_Delete_Error, string.Format(Text.BlueprintManagerDialogModel_Delete_Error_Description, e.Message), MessageEventType.Error);
            }
        }

        void LoadBlueprints()
        {
            Blueprints.Clear();
            var blueprintDirectory = new DirectoryInfo(BlueprintPath);
            if (blueprintDirectory.Exists)
            {
                foreach (var folder in blueprintDirectory.EnumerateDirectories())
                {
                    if (!File.Exists(Path.Combine(folder.FullName, "script.cs")))
                        continue;

                    BitmapImage icon = null;
                    var thumbFileName = ThumbFileNames.Select(name => Path.Combine(folder.FullName, name)).FirstOrDefault(File.Exists);
                    if (thumbFileName != null)
                    {
                        icon = new BitmapImage();
                        icon.BeginInit();
                        icon.DecodePixelWidth = 32;
                        icon.UriSource = new Uri(thumbFileName);
                        icon.CacheOption = BitmapCacheOption.OnLoad;
                        icon.CreateOptions = BitmapCreateOptions.DelayCreation;
                        icon.EndInit();
                    }

                    var model = new BlueprintModel(this, icon, folder, _significantBlueprints?.Contains(folder.Name) ?? false);
                    Blueprints.Add(model);
                }
            }
            OnBlueprintsLoaded();
        }

        /// <inheritdoc />
        /// <returns></returns>
        protected override bool OnSave() => true;

        /// <summary>
        /// Sends a message through the user interface to the end-user.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="description"></param>
        /// <param name="type"></param>
        /// <param name="defaultResult"></param>
        /// <returns></returns>
        public bool SendMessage(string title, string description, MessageEventType type, bool defaultResult = true)
        {
            var args = new MessageEventArgs(title, description, type, !defaultResult);
            MessageRequested?.Invoke(this, args);
            return !args.Cancel;
        }

        /// <summary>
        /// Fires the <see cref="BlueprintsLoaded"/> event.
        /// </summary>
        protected virtual void OnBlueprintsLoaded()
        {
            BlueprintsLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}
