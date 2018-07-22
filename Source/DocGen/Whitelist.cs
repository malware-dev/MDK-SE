using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DocGen
{
    class Whitelist
    {
        public static Whitelist Load(string fileName)
        {
            var lines = File.ReadAllLines(fileName).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim()).ToList();
            return new Whitelist(lines);
        }

        List<WhitelistKey> _entries;
        HashSet<string> _assemblyNames;

        Whitelist(List<string> lines)
        {
            _entries = lines.Select(line =>
                {
                    if (WhitelistKey.TryParse(line, out var entry))
                        return entry;
                    return null;
                }).Where(entry => entry != null)
                .ToList();
            _assemblyNames = new HashSet<string>(_entries.Select(e => e.AssemblyName).Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsWhitelisted(Assembly assembly)
        {
            return IsWhitelisted(assembly.GetName());
        }

        public bool IsWhitelisted(AssemblyName assemblyName)
        {
            return _assemblyNames.Contains(assemblyName.Name);
        }

        public bool IsWhitelisted(Type type)
        {
            var typeKey = WhitelistKey.ForType(type);
            if (typeKey == null)
                return false;
            return _entries.Any(key => key.IsMatchFor(typeKey));
        }

        public bool IsWhitelisted(MemberInfo memberInfo)
        {
            if (!IsWhitelisted(memberInfo.DeclaringType.Assembly))
                return false;
            var typeKey = WhitelistKey.ForMember(memberInfo);
            return _entries.Any(key => key.IsMatchFor(typeKey));
        }
    }
}