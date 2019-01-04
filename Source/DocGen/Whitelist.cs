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

        List<WhitelistRule> _entries;
        HashSet<string> _assemblyNames;

        Whitelist(List<string> lines)
        {
            _entries = new List<WhitelistRule>();
            foreach (var entry in lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(WhitelistRule.Parse))
            {
                if (entry is MemberRule memberRule && !_entries.Any(e => e is TypeRule typeRule && typeRule.Type == memberRule.MemberInfo.DeclaringType))
                    _entries.Add(new TypeRule(memberRule.MemberInfo.DeclaringType, false));
                _entries.Add(entry);
            }

            _assemblyNames = new HashSet<string>(_entries.Select(e => e.Assembly.GetName().Name).Distinct(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
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
            if (!IsWhitelisted(type.Assembly))
                return false;
            return _entries.Any(key => key.IsMatch(type));
        }

        public bool IsWhitelisted(MemberInfo memberInfo)
        {
            if (memberInfo is Type type)
                return IsWhitelisted(type);
            Debug.Assert(memberInfo.DeclaringType != null, "memberInfo.DeclaringType != null");
            if (!IsWhitelisted(memberInfo.DeclaringType.Assembly))
                return false;
            return _entries.Any(key => key.IsMatch(memberInfo));
        }
    }
}