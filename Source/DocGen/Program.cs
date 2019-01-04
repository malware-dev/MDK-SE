using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Malware.MDKWhitelistExtractor;

namespace DocGen
{
    public class Program
    {
        public static async Task<int> Main()
        {
            var commandLine = new CommandLine(Environment.CommandLine);
            try
            {
                var program = new Program();
                return await program.Run(commandLine);
            }
            catch (Exception e)
            {
                if (commandLine.IndexOf("-verbose") >= 0)
                    Console.WriteLine(e);
                else
                    Console.WriteLine(e.Message);
                return -1;
            }
        }

        async Task<int> Run(CommandLine commandLine)
        {
            var cts = new CancellationTokenSource();
            var spinTask = Spin(cts.Token);
            try
            {
                var path = Environment.CurrentDirectory;
                var cacheIndex = commandLine.IndexOf("-cache");
                if (cacheIndex >= 0)
                    path = Path.GetFullPath(commandLine[cacheIndex + 1]);

                var update = commandLine.IndexOf("-update") >= 0;

                if (update)
                    await WhitelistAndTerminalCaches.Update(path);

                string output = null;
                var outputIndex = commandLine.IndexOf("-output");
                if (outputIndex >= 0)
                    output = Path.GetFullPath(commandLine[outputIndex + 1]);
                if (output != null)
                    await GenerateDocs(path, output);

                return 0;
            }
            finally
            {
                cts.Cancel();
                await spinTask;
            }
        }

        async Task GenerateDocs(string path, string output)
        {
            //var whitelist = Whitelist.Load(Path.Combine(path, "whitelist.cache"));
            await ProgrammableBlockApi.Update(Path.Combine(path, "whitelist.cache"), Path.Combine(output, "api"));

            Terminals.Update(Path.Combine(path, "terminal.cache"), Path.Combine(output, "List-Of-Terminal-Properties-And-Actions.md"));
        }

        async Task Spin(CancellationToken cancellationToken)
        {
            var l = Console.CursorLeft;
            var t = Console.CursorTop;

            var animations = new[] { '|', '/', '-', '\\' };
            long i = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var rl = Console.CursorLeft;
                var rt = Console.CursorTop;
                Console.SetCursorPosition(l, t);
                Console.Write(animations[i % animations.Length]);
                i++;
                Console.SetCursorPosition(rl, rt);
                try
                {
                    // ReSharper disable once MethodSupportsCancellation
                    await Task.Delay(250);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
    }
}