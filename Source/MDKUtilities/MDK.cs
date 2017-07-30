using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;

namespace Malware.MDKUtilities
{
    /// <summary>
    ///     Factory class for constructing various mockups and utilities
    /// </summary>
    public class MDK
    {
        /// <summary>
        /// Gets the game binary path as defined through <see cref="MDKUtilityFramework.Load"/>.
        /// </summary>
        public static string GameBinPath => MDKUtilityFramework.GameBinPath;

        /// <summary>
        ///     The default implementation of the mocked Echo function. Simply dumps the content to the console and the debug
        ///     output.
        /// </summary>
        /// <param name="text">The text to output</param>
        public static void DefaultEcho(string text)
        {
            Console.WriteLine(text);
            Debug.WriteLine(text);
        }

        /// <summary>
        ///     Properly create and configure a Grid Program the way it is done in the game.
        /// </summary>
        /// <typeparam name="T">The type of the program to create</typeparam>
        /// <param name="config">Various runtime configuration</param>
        /// <returns></returns>
        public static T CreateProgram<T>(ProgramConfig config = default(ProgramConfig)) where T: MyGridProgram
        {
            return (T)CreateProgram(typeof(T), config);
        }

        /// <summary>
        ///     Properly create and configure
        /// </summary>
        /// <param name="type">The type of the program to create. Must derive from <see cref="MyGridProgram"/></param>
        /// <param name="config">Program configuration</param>
        /// <returns></returns>
        public static MyGridProgram CreateProgram(Type type, ProgramConfig config = default(ProgramConfig))
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!typeof(MyGridProgram).IsAssignableFrom(type))
                throw new ArgumentException(
                    string.Format(Resources.MDK_CreateProgram_InvalidTypeBase, typeof(MyGridProgram).FullName),
                    nameof(type));

            var instance = FormatterServices.GetUninitializedObject(type) as IMyGridProgram;
            var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, Type.EmptyTypes, null);
            if (instance == null || constructor == null)
                throw new ArgumentException(Resources.MDK_CreateProgram_NoValidConstructor, nameof(type));

            var runtime = config.Runtime ?? new SimpleRuntimeInfo();
            instance.Runtime = runtime;
            instance.Storage = config.Storage ?? "";
            instance.Me = config.ProgrammableBlock;
            instance.Echo = config.Echo ?? DefaultEcho;
            constructor.Invoke(instance, null);
            if (!instance.HasMainMethod)
                throw new ArgumentException(Resources.MDK_CreateProgram_NoMainMethod, nameof(type));

            return (MyGridProgram)instance;
        }

        /// <summary>
        ///     Runs the given program. If the program's runtime derives from <see cref="SimpleRuntimeInfo" />, it's metrics will
        ///     be updated.
        /// </summary>
        /// <param name="gridProgram">The program to run</param>
        /// <param name="argument">The argument to pass to the program</param>
        /// <param name="timeSinceLastRun">The simulated time since the last time the program was run</param>
        public static void Run(IMyGridProgram gridProgram, string argument = "", TimeSpan timeSinceLastRun = default(TimeSpan))
        {
            if (gridProgram == null)
                throw new ArgumentNullException(nameof(gridProgram));

            var runtime = gridProgram.Runtime as SimpleRuntimeInfo;
            if (runtime != null)
                runtime.TimeSinceLastRun = timeSinceLastRun;
            var stopwatch = Stopwatch.StartNew();
            gridProgram.Main(argument ?? "");
            if (runtime != null)
                runtime.LastRunTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        }

        /// <summary>
        ///     Runs the Save method of the given program, if there is any.
        /// </summary>
        /// <param name="gridProgram"></param>
        /// <returns></returns>
        public static string Save(IMyGridProgram gridProgram)
        {
            if (gridProgram == null)
                throw new ArgumentNullException(nameof(gridProgram));
            if (gridProgram.HasSaveMethod)
                gridProgram.Save();
            return gridProgram.Storage;
        }

        /// <summary>
        ///     This type contains optional runtime configuration for a grid program instance.
        /// </summary>
        public struct ProgramConfig
        {
            /// <summary>
            ///     The programmable block which is to be simulated as running the program (the <c>Me</c> property)
            /// </summary>
            public IMyProgrammableBlock ProgrammableBlock;

            /// <summary>
            ///     A custom runtime instance
            /// </summary>
            public IMyGridProgramRuntimeInfo Runtime;

            /// <summary>
            ///     The contents of the <c>Storage</c> property
            /// </summary>
            public string Storage;

            /// <summary>
            ///     A replacement Echo method
            /// </summary>
            public Action<string> Echo;
        }
    }
}
