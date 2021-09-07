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
            Dictionary<string, object> configuration;
            try
            {
                configuration = ReadVdf(configPath);
            }
            catch (Exception)
            {
                return null;
            }

            if (!configuration.ContainsKey("libraryfolders"))
                return null;

            configuration = (Dictionary<string, object>)configuration["libraryfolders"];
            int n = 1;
            var basePaths = new List<string>
            {
                basePath
            };
            string path;
            while (true)
            {
                var key = n.ToString();
                n++;
                if (!configuration.ContainsKey(key))
                    break;
                var folder = (Dictionary<string, object>)configuration[key];
                path = (string)folder["path"];
                basePaths.Add(path);
            }

            // Search through the potential base path, returning the first path which contains the desired
            // verification file.
            path = basePaths.Select(p => Path.Combine(p, "SteamApps", "common", subfolderName))
                .FirstOrDefault(p => File.Exists(Path.Combine(p, verificationFilePath)));

            if (path != null && !path.EndsWith("\\"))
                path += "\\";
            return path;
        }

        private Dictionary<string, object> ReadVdf(string configPath)
        {
            var lines = File.ReadAllLines(configPath);
            int pos = 0;
            return ReadVdfObject(lines, ref pos, false);
        }

        private Dictionary<string, object> ReadVdfObject(string[] lines, ref int pos, bool nested)
        {
            if (nested)
            {
                if (pos >= lines.Length && !Regex.IsMatch(lines[pos], @"\A\s*{\s*\z", RegexOptions.Singleline))
                    throw new InvalidDataException("Steam .vdf file format failure");
                pos++;
            }

            var obj = new Dictionary<string, object>();
            while (true)
            {
                if (nested)
                {
                    if (pos < lines.Length && Regex.IsMatch(lines[pos], @"\A\s*}\s*\z", RegexOptions.Singleline))
                    {
                        pos++;
                        return obj;
                    }
                }

                var part = ReadVdfLine(lines, ref pos);
                if (part == null)
                    break;
                if (part.Value.Value == null)
                {
                    obj[Unquote(part.Value.Key)] = ReadVdfObject(lines, ref pos, true);
                    continue;
                }

                obj[Unquote(part.Value.Key)] = Unquote(part.Value.Value);
            }
            if (nested)
                throw new InvalidDataException("Steam .vdf file format failure");
            return obj;
        }

        private string Unquote(string value)
        {
            return value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
        }

        private static KeyValuePair<string, string>? ReadVdfLine(string[] lines, ref int pos)
        {
            if (pos >= lines.Length)
                return null;
            var matches = Regex.Matches(lines[pos], @"\s*(""[^""\\]*(?:\\.[^""\\]*)*"")", RegexOptions.Singleline);
            switch (matches.Count)
            {
                case 1:
                    pos++;
                    return new KeyValuePair<string, string>(matches[0].Groups[1].Value, null);
                case 2:
                    pos++;
                    return new KeyValuePair<string, string>(matches[0].Groups[1].Value, matches[1].Groups[1].Value);
                default:
                    return null;
            }
        }
    }
}
