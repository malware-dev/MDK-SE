using System;
using System.IO;
using Newtonsoft.Json;

namespace MDK.Build
{
    public class BuildConfig
    {
        public static BuildConfig Load(string fileName)
        {
            return JsonConvert.DeserializeObject<BuildConfig>(File.ReadAllText(fileName));
        }

        public string Output { get; set; } = Environment.CurrentDirectory;

        public bool Minify { get; set; }

    }
}