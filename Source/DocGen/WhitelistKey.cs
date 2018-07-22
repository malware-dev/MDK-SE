using System;
using System.Text.RegularExpressions;

namespace DocGen
{
    class WhitelistKey
    {
        public static bool TryParse(string text, out WhitelistKey entry)
        {
            var assemblyNameIndex = text.LastIndexOf(',');
            if (assemblyNameIndex < 0)
            {
                entry = null;
                return false;
            }

            var assemblyName = text.Substring(assemblyNameIndex + 1).Trim();
            var path = text.Substring(0, assemblyNameIndex).Trim();
            var regexPattern = "\\A" + Regex.Escape(path.Replace('+', '.')).Replace("\\*", ".*") + "\\z";
            var regex = new Regex(regexPattern, RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            entry = new WhitelistKey(assemblyName, path, regex);
            return true;
        }

        readonly Regex _regex;

        WhitelistKey(string assemblyName, string path, Regex regex)
        {
            _regex = regex;
            AssemblyName = assemblyName;
            Path = path;
        }

        public string AssemblyName { get; }

        public string Path { get; }

        public bool IsMatchFor(ApiEntry apiEntry)
        {
            if (apiEntry == null)
                return false;
            return string.Equals(AssemblyName, apiEntry.AssemblyName, StringComparison.OrdinalIgnoreCase) && _regex.IsMatch(apiEntry.WhitelistKey);
        }
    }
}