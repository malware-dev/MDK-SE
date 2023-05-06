using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

using MDK.Build;

namespace MDK
{
    class BuildCLI
    {
        abstract class Command
        {
            public class Version: Command { };
            public class Unknown: Command
            {
                public string UnknownCommandName;
            };
            public class Build: Command
            {
                public string ProjectPath;
            }

            public static Command Parse(string[] args)
            {
                if (args.Length < 1)
                    return new Version();
                var commandName = args[0];
                switch (commandName.ToLower())
                {
                    case "version":
                        return new Version();
                    case "build":
                        var projectPath = args.Length >= 2 ? args[1] : ".";
                        return new Build() { ProjectPath = projectPath };
                    default:
                        return new Unknown() { UnknownCommandName = commandName };
                }
            }
        }

        static void HandleVersion()
        {
            // TODO: I'm sure MDK has some kind of version autogenerator
            // I just need to find it
            Console.WriteLine("MDKCommandLine Version X.X.X");
            Console.WriteLine("MDK.Build Version X.X.X");
        }
        static void HandleUnknown(Command.Unknown unknown)
        {
            Console.WriteLine($"Unknown MDKCommandLine command \"{unknown.UnknownCommandName}\"");
        }
        static async Task HandleBuild(Command.Build build)
        {
            var projectPath = ResolveProjectPath(build);
            MSBuildLocator.RegisterDefaults();

            using (var workspace = MSBuildWorkspace.Create())
            {
                var project = await workspace.OpenProjectAsync(projectPath);
                var builder = new ProjectBuilder(project);
                var script = await builder.BuildScript();
                builder.WriteScript(script);
                Console.WriteLine($"Written a {script.Length} long script to {builder.Destination} as {builder.ScriptName}");
            }
        }

        static string ResolveProjectPath(Command.Build build)
        {
            var dir = Directory.GetCurrentDirectory();
            string providedPath = Path.GetFullPath(Path.Combine(dir, build.ProjectPath));

            if (providedPath.EndsWith(".csproj"))
                return providedPath;
            else
            {
                var parts = providedPath.Split(Path.DirectorySeparatorChar);
                var lastPart = parts[^1];
                return Path.Combine(providedPath, $"{lastPart}.csproj");
            }
        }

        public static async Task Main(string[] args)
        {
            var command = Command.Parse(args);
            switch (command)
            {
                case Command.Version:
                    HandleVersion();
                    return;
                case Command.Unknown unknown:
                    HandleUnknown(unknown);
                    return;
                case Command.Build build:
                    await HandleBuild(build);
                    return;
                default:
                    throw new Exception("Unhandled CLI Command type");
            }
        }
    }
}