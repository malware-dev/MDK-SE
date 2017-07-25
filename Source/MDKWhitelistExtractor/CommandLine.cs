using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Malware.MDKWhitelistExtractor
{
    public class CommandLine
    {
        static Regex _regex = new Regex(@"((""[^""]*(""""[^""]*)*"")|[^\s])+", RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.ExplicitCapture);
        List<string> _arguments = new List<string>();

        public CommandLine(string commandLine)
        {
            if (!string.IsNullOrWhiteSpace(commandLine))
                _arguments.AddRange(_regex.Matches(commandLine).Cast<Match>().Select(m => m.Value.Replace("\"", "")));
        }

        public string this[int index] => _arguments[index];

        public int Count => _arguments.Count;

        public int IndexOf(string argument)
        {
            return _arguments.FindIndex(a => string.Equals(argument, a, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}