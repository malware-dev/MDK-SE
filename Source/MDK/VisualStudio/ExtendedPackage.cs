using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MDK.VisualStudio
{
    /// <summary>
    /// Provides helpful extensions for a Visual Studio package.
    /// </summary>
    public abstract class ExtendedPackage : Package
    {
        List<Command> _commands = new List<Command>();
        DTE _dte;

        /// <summary>
        /// Creates an instance of an <see cref="ExtendedPackage"/>
        /// </summary>
        protected ExtendedPackage()
        {
            Commands = new ReadOnlyCollection<Command>(_commands);
        }

        /// <summary>
        /// Returns the Development Tools Environment for the Visual Studio instance this package is installed into.
        /// </summary>
        public DTE DTE
        {
            get
            {
                if (_dte == null)
                    _dte = (DTE)GetService(typeof(DTE));
                return _dte;
            }
        }

        /// <summary>
        /// The list of registered commands belonging to this package
        /// </summary>
        public ReadOnlyCollection<Command> Commands { get; }

        /// <summary>
        /// Adds one or more commands to this package.
        /// </summary>
        /// <param name="commands"></param>
        protected void AddCommand(params Command[] commands)
        {
            foreach (var command in commands)
            {
                _commands.Add(command);
                command.Initialize();
            }
        }

        /// <summary>
        /// Logs an exception to the visual studio output window.
        /// </summary>
        /// <param name="category"></param>
        /// <param name="exception"></param>
        public void LogPackageError(string category, Exception exception)
        {
            var outWindow = GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outWindow == null)
                throw new InvalidOperationException("Could not retrieve the Visual Studio output window");
            var generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;
            outWindow.GetPane(ref generalPaneGuid, out IVsOutputWindowPane generalPane);
            generalPane.OutputString($"{category}:{exception}");
            generalPane.Activate();
        }
    }
}
