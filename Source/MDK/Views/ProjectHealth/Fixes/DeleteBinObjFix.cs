using Malware.MDKServices;
using System.IO;
using System.Threading.Tasks;

namespace MDK.Views.ProjectHealth.Fixes
{
    class DeleteBinObjFix: Fix
    {
        public DeleteBinObjFix(): base(int.MaxValue) { }

        public override async Task ApplyAsync(HealthAnalysis analysis, FixStatus status)
        {
            status.Description = "Deleting bin/obj caches";
            await Task.Run(() =>
            {
                var projectFolder = Path.GetDirectoryName(analysis.FileName)!;
                var binFolder = Path.Combine(projectFolder, "bin");
                var objFolder = Path.Combine(projectFolder, "obj");
                try
                {
                    Directory.Delete(binFolder, true);
                }
                catch
                {
                    // Ignore this for now.
                }

                try
                {
                    Directory.Delete(objFolder, true);
                }
                catch
                {
                    // Ignore this for now.
                }
            });
        }
    }
}