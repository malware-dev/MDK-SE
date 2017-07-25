using System;
using System.IO;
using Newtonsoft.Json;

namespace MDK.Build
{
    /// <summary>
    /// Contains build configuration
    /// </summary>
    public class BuildConfig
    {
        /// <summary>
        /// Loads build configuration from the given JSON file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static BuildConfig Load(string fileName)
        {
            return JsonConvert.DeserializeObject<BuildConfig>(File.ReadAllText(fileName));
        }

        /// <summary>
        /// The target directory where the final file should be placed
        /// </summary>
        public string Output { get; set; } = Environment.CurrentDirectory;

        /// <summary>
        /// Whether or not the minifier should be invoked on the generated script file.
        /// </summary>
        public bool Minify { get; set; }
    }
}
