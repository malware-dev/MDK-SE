using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocGen
{
    abstract class WhitelistRule
    {
        public Assembly Assembly { get; }

        public static WhitelistRule Parse(string text)
        {
            var assemblyNameIndex = text.LastIndexOf(',');
            if (assemblyNameIndex < 0)
                throw new ArgumentException("Unknown format", nameof(text));

            var assemblyName = text.Substring(assemblyNameIndex + 1).Trim();
            var path = text.Substring(0, assemblyNameIndex).Trim();
            var withAllMembers = path.EndsWith(".*") || path.EndsWith("+*");
            if (path.EndsWith(".*") || path.EndsWith("+*"))
                path = path.Substring(0, path.Length - 2);
            var genericIndex = path.IndexOf('<');
            if (genericIndex >= 0)
            {
                var genericEnd = path.IndexOf('>');
                var section = path.Substring(genericIndex + 1, genericEnd - genericIndex - 1);
                var n = section.Split(',').Length;
                path = path.Substring(0, genericIndex) + $"`{n}" + path.Substring(genericEnd + 1);
            }
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.InvariantCultureIgnoreCase));
            if (assembly == null)
                throw new InvalidOperationException($"Could not find required assembly {assemblyName}");
            path = ToCliTypeName(path);

            var paramIndex = path.IndexOf('(');
            var paramTypes = new List<string>();
            if (paramIndex >= 0)
            {
                var paramEnd = path.IndexOf(')');
                var section = path.Substring(paramIndex + 1, paramEnd - paramIndex - 1);
                paramTypes.AddRange(FindParamTypes(section));
                path = path.Substring(0, paramIndex) + path.Substring(paramEnd + 1);
            }

            Type type = null;
            var parts = new Queue<string>(path.Split('.', '+').ToList());
            var name = "";
            while (type == null && parts.Count > 0)
            {
                if (name.Length > 0)
                    name += ".";
                name += Translate(parts.Dequeue());
                type = assembly.GetType(name);
            }

            if (type == null)
                return new NamespaceRule(path, assembly);

            var members = new List<MemberInfo>();
            var memberParamTypes = new List<string>();
            while (parts.Count > 0)
            {
                name = Translate(parts.Dequeue());
                members.Clear();
                members.AddRange(type.GetMember(name, MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
                genericIndex = name.IndexOf('`');
                if (genericIndex >= 0)
                {
                    name = name.Substring(0, genericIndex);
                    members.AddRange(type.GetMember(name, MemberTypes.All, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static));
                }
                if (paramTypes.Count > 0)
                {
                    for (var index = members.Count - 1; index >= 0; index--)
                    {
                        var member = members[index];
                        if (member is MethodInfo method)
                        {
                            memberParamTypes.Clear();
                            memberParamTypes.AddRange(method.GetParameters().Select(t => ParameterPrefix(t) + t.GetActualParameterType().GetHumanReadableName()));
                            if (AreSame(memberParamTypes, paramTypes))
                                continue;
                        }
                        members.RemoveAt(index);
                    }
                }
                if (members.Count == 0)
                    throw new InvalidOperationException("No matches");
                if (members.Count > 1)
                    throw new InvalidOperationException("Too many matches");
                if (members[0] is Type nestedType)
                {
                    type = nestedType;
                    continue;
                }
                if (parts.Count > 0)
                    throw new InvalidOperationException("Impossible match - nested member?");
                return new MemberRule(members[0]);
            }

            return new TypeRule(type, withAllMembers);
            //if (path.Contains('('))
            //    return WhitelistItemType.Member;

            //    var wType = WhitelistItemType.Type;
            //    var nameParts = path.Split('.').ToList();
            //    while (nameParts.Count > 0)
            //    {
            //        var type = assembly.GetType(string.Join(".", nameParts), false, true);
            //        if (type != null)
            //            return wType;
            //        wType = WhitelistItemType.Member;
            //        nameParts.RemoveAt(nameParts.Count - 1);
            //    }

            //    return WhitelistItemType.Namespace;
        }

        static string Translate(string name)
        {
            switch (name)
            {
                case "operator ==":
                    return "op_Equality";
                case "operator !=":
                    return "op_Inequality";
            }

            return name;
        }

        static bool AreSame(List<string> memberParamTypes, List<string> paramTypes)
        {
            if (memberParamTypes.Count != paramTypes.Count)
                return false;

            for (var j = 0; j < memberParamTypes.Count; j++)
            {
                if (memberParamTypes[j] != paramTypes[j])
                    return false;
            }

            return true;
        }

        static string ParameterPrefix(ParameterInfo parameterInfo)
        {
            if (parameterInfo.IsDefined(typeof(ParamArrayAttribute), false))
                return "params ";
            if (parameterInfo.IsOut)
                return "out ";
            if (parameterInfo.ParameterType.IsPointer)
                return "*";
            if (parameterInfo.ParameterType.IsByRef)
                return "ref ";
            return "";
        }

        static IEnumerable<string> FindParamTypes(string section)
        {
            var buffer = new StringBuilder();
            var n = 0;
            foreach (var ch in section)
            {
                switch (ch)
                {
                    case '<':
                        n++;
                        break;
                    case '>':
                        n--;
                        break;
                    case ',':
                        if (n == 0)
                        {
                            yield return buffer.ToString().Trim();
                            buffer.Clear();
                            continue;
                        }
                        break;
                }

                buffer.Append(ch);
            }
            if (buffer.Length > 0)
                yield return ToCliTypeName(buffer.ToString().Trim());
        }

        static string ToCliTypeName(string name)
        {
            return Regex.Replace(name, @"\b\w+\b", match =>
            {
                switch (match.Value)
                {
                    case "bool":
                        return typeof(bool).FullName;
                    case "char":
                        return typeof(char).FullName;
                    case "byte":
                        return typeof(byte).FullName;
                    case "sbyte":
                        return typeof(sbyte).FullName;
                    case "ushort":
                        return typeof(ushort).FullName;
                    case "short":
                        return typeof(short).FullName;
                    case "uint":
                        return typeof(uint).FullName;
                    case "int":
                        return typeof(int).FullName;
                    case "ulong":
                        return typeof(ulong).FullName;
                    case "long":
                        return typeof(long).FullName;
                    case "float":
                        return typeof(float).FullName;
                    case "double":
                        return typeof(double).FullName;
                    case "decimal":
                        return typeof(decimal).FullName;
                    case "object":
                        return typeof(object).FullName;
                    case "string":
                        return typeof(string).FullName;
                }

                return match.Value;
            });
        }

        //public static bool TryLoad(string text, List<Assembly> assemblies, out WhitelistKey entry)
        //{
        //    var assemblyNameIndex = text.LastIndexOf(',');
        //    if (assemblyNameIndex < 0)
        //    {
        //        entry = null;
        //        return false;
        //    }

        //    var assemblyName = text.Substring(assemblyNameIndex + 1).Trim();
        //    var path = text.Substring(0, assemblyNameIndex).Trim();
        //    var withAllMembers = path.EndsWith(".*") || path.EndsWith("+*");
        //    if (path.EndsWith(".*") || path.EndsWith("+*"))
        //        path = path.Substring(0, path.Length - 2);
        //    var genericIndex = path.IndexOf('<');
        //    if (genericIndex >= 0)
        //    {
        //        var genericEnd = path.IndexOf('>');
        //        var section = path.Substring(genericIndex + 1, genericEnd - genericIndex - 1);
        //        var n = section.Split(',').Length;
        //        path = path.Substring(0, genericIndex) + $"`{n}" + path.Substring(genericEnd + 1);
        //    }
        //    var type = Identify(assemblies, assemblyName, path);
        //    entry = new WhitelistKey(assemblyName, path, type, withAllMembers);
        //    return true;
        //}

        //static WhitelistItemType Identify(List<Assembly> assemblies, string assemblyName, string path)
        //{
        //    var assembly = assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.InvariantCultureIgnoreCase));
        //    if (assembly == null)
        //        throw new InvalidOperationException($"Could not find required assembly {assemblyName}");

        //    switch (path.ToLowerInvariant())
        //    {
        //        case "bool":
        //        case "char":
        //        case "byte":
        //        case "sbyte":
        //        case "ushort":
        //        case "short":
        //        case "uint":
        //        case "int":
        //        case "ulong":
        //        case "long":
        //        case "float":
        //        case "double":
        //        case "decimal":
        //        case "object":
        //        case "string":
        //            return WhitelistItemType.Type;
        //    }

        //    if (path.Contains('('))
        //        return WhitelistItemType.Member;

        //    var wType = WhitelistItemType.Type;
        //    var nameParts = path.Split('.').ToList();
        //    while (nameParts.Count > 0)
        //    {
        //        var type = assembly.GetType(string.Join(".", nameParts), false, true);
        //        if (type != null)
        //            return wType;
        //        wType = WhitelistItemType.Member;
        //        nameParts.RemoveAt(nameParts.Count - 1);
        //    }

        //    return WhitelistItemType.Namespace;
        //}

        protected WhitelistRule(Assembly assembly)
        {
            Assembly = assembly;
        }

        public abstract bool IsMatch(MemberInfo memberInfo);
    }

    class MemberRule : WhitelistRule
    {
        public MemberInfo MemberInfo { get; }

        public MemberRule(MemberInfo memberInfo) : base(memberInfo.GetAssembly())
        {
            MemberInfo = memberInfo;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            return memberInfo == MemberInfo;
        }
    }

    class TypeRule : WhitelistRule
    {
        public Type Type { get; }
        public bool AllMembers { get; }

        public TypeRule(Type type, bool allMembers): base(type.Assembly)
        {
            Type = type;
            AllMembers = allMembers;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            if (!AllMembers)
                return memberInfo == Type;

            if (memberInfo is Type type && type == Type)
                return true;

            return NestsAndSelfOf(memberInfo.DeclaringType).Any(t => t == Type);
        }

        static IEnumerable<Type> NestsAndSelfOf(Type type)
        {
            if (type == null)
                yield break;
            while (type != null)
            {
                yield return type;
                type = type.DeclaringType;
            }
        }
    }

    class NamespaceRule : WhitelistRule
    {
        public string NamespaceName { get; }

        public NamespaceRule(string namespaceName, Assembly assembly): base(assembly)
        {
            NamespaceName = namespaceName;
        }

        public override bool IsMatch(MemberInfo memberInfo)
        {
            return memberInfo.GetAssembly() == Assembly && memberInfo.GetNamespace() == NamespaceName;
        }
    }
}
