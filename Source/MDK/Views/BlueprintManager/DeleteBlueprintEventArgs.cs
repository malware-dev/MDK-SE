using System.ComponentModel;

namespace MDK.Views.BlueprintManager
{
    /// <summary>
    /// Event arguments for the <see cref="BlueprintManagerDialogModel.DeletingBlueprint"/> event
    /// </summary>
    public class DeleteBlueprintEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="DeleteBlueprintEventArgs"/>
        /// </summary>
        /// <param name="blueprint"></param>
        public DeleteBlueprintEventArgs(BlueprintModel blueprint)
            : base(false)
        {
            Blueprint = blueprint;
        }

        /// <summary>
        /// The blueprint about to be deleted
        /// </summary>
        public BlueprintModel Blueprint { get; }
    }
}