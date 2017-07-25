using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Provides a base class for a Visual Studio menu command.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Creates a new instance of a <see cref="Command"/>.
        /// </summary>
        /// <param name="package">The Visual Studio package this command belongs to</param>
        protected Command(ExtendedPackage package)
        {
            Package = package;
        }

        /// <summary>
        /// The Visual Studio package this command belongs to
        /// </summary>
        public ExtendedPackage Package { get; }

        /// <summary>
        /// The ID of the command group this command belongs to
        /// </summary>
        public abstract Guid GroupId { get; }

        /// <summary>
        /// The package-wide unique ID of this command
        /// </summary>
        public abstract int Id { get; }

        /// <summary>
        /// Gets the service provider from the owner MDKPackage.
        /// </summary>
        protected IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Initializes this command.
        /// </summary>
        public virtual void Initialize()
        {
            if (!(ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService))
            {
                Package.LogPackageError(GetType().FullName, new InvalidOperationException("Cannot retrieve menu command service"));
                return;
            }

            var menuCommandId = new CommandID(GroupId, Id);
            var menuItem = new MenuCommand((s, e) => OnExecute(), menuCommandId);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Called when a user invokes this command.
        /// </summary>
        protected abstract void OnExecute();
    }
}
