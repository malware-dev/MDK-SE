using Sandbox.ModAPI.Ingame;
using Malware.MDKUtilities;

namespace IngameScript
{
    public class TestBootstrapper
    {
        // This file, as well as any file containing the word ".debug." in it, will be excluded
        // from the build process. You can use this to directly test your scripts.

        static TestBootstrapper()
        {
            // WARNING: This path is _not_ automatically updated if you change the game binary location.
            // You will have to change it yourself.
            MDKUtilityFramework.Load(@"$mdkgamebinpath$\");
        }

        public static void Main()
        {
            // In order for your program to actually run, you will need to provide a mockup of all the facilities 
            // your script uses from the game, since they're not available outside of the game.
            // <pending nuget package reference here>

            // Create and configure the desired program.
            var program = MDK.CreateProgram<Program>();
            MDK.Run(program);
        }
    }
}