using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Malware.MDKUtilities
{
    /// <summary>
    /// A service to retrieve information about the current Steam (copyright Valve, no affiliation) installation.
    /// </summary>
    class Steam
    {
        string _exePath;
        bool _isLoaded;

        /// <summary>
        /// Gets the current executable path for Steam
        /// </summary>
        public string ExePath
        {
            get
            {
                if (!_isLoaded)
                {
                    _isLoaded = true;
                    _exePath = FindSteam();
                }
                return _exePath;
            }
        }

        /// <summary>
        /// Determines whether Steam exists
        /// </summary>
        public bool Exists => ExePath != null && File.Exists(ExePath);

        string FindSteam()
        {
            var path = Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Valve\Steam", "SteamExe", null) as string;
            if (!File.Exists(path))
                return null;
            return path;
        }

        /// <summary>
        /// Attempts to determine the installation folder for the given game
        /// </summary>
        /// <param name="subfolderName">The install folder name of the game in question</param>
        /// <param name="verificationFilePath">The relative path to a file which existence verify the correctness of the install folder.</param>
        /// <returns></returns>
        public string GetInstallFolder(string subfolderName, string verificationFilePath)
        {
            string basePath;
            try
            {
                basePath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            }
            catch (Exception)
            {
                return null;
            }
            if (basePath == null)
            {
                return null;
            }
            var configPath = Path.Combine(basePath, @"steamapps\libraryfolders.vdf");
            string configuration;
            try
            {
                configuration = File.ReadAllText(configPath);
            }
            catch (Exception)
            {
                return null;
            }

            var basePaths = new List<string>
            {
                basePath
            };

            var matches = Regex.Matches(configuration, @"""\d+""[ \t]+""([^""]+)""", RegexOptions.Singleline);
            foreach (Match match in matches)
                basePaths.Add(match.Groups[1].Value);

            // Search through the potential base path, returning the first path which contains the desired
            // verification file.
            var path = basePaths.Select(p => Path.Combine(p, "SteamApps", "common", subfolderName))
                .FirstOrDefault(p => File.Exists(Path.Combine(p, verificationFilePath)));

            if (path != null && !path.EndsWith("\\"))
                path += "\\";
            return path;
        }
    }
}
