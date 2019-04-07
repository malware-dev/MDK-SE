using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DocGen.XmlDocs;

namespace DocGen
{
    class ApiEntry
    {
        static readonly HashSet<char> InvalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());

        public static ApiEntry Create(ProgrammableBlockApi api, Whitelist whitelist, MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return ForType(api, whitelist, type);
                case ConstructorInfo constructorInfo:
                    return ForConstructor(api, whitelist, constructorInfo);
                case EventInfo eventInfo:
                    return ForEvent(api, whitelist, eventInfo);
                case FieldInfo fieldInfo:
                    return ForField(api, whitelist, fieldInfo);
                case PropertyInfo propertyInfo:
                    return ForProperty(api, whitelist, propertyInfo);
                case MethodInfo methodInfo:
                    return ForMethod(api, whitelist, methodInfo);
                default:
                    return null;
            }
        }

        static ApiEntry ForType(ProgrammableBlockApi api, Whitelist whitelist, Type type)
        {
            if (!type.IsPublic && !type.IsNestedPublic && !type.IsNestedFamily && !type.IsNestedFamANDAssem && !type.IsNestedFamORAssem)
                return null;
            if (type.IsGenericTypeDefinition || type.IsGenericType)
                return ForGenericType(api, whitelist, type);

            var assemblyName = type.Assembly.GetName().Name;
            var namespaceName = type.Namespace;
            var name = type.Name;
            var xmlDocKey = $"T:{namespaceName}.{name}";

            return new ApiEntry(api, type, assemblyName, namespaceName, name, xmlDocKey, whitelist.IsWhitelisted(type));
        }

        static ApiEntry ForGenericType(ProgrammableBlockApi api, Whitelist whitelist, Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var namespaceName = type.IsGenericType ? type.GetGenericTypeDefinition().Namespace : type.Namespace;
            var name = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
            var separatorIndex = name.IndexOf('`');
            if (separatorIndex >= 0)
                name = name.Substring(0, separatorIndex);
            var genericArguments = type.GetGenericArguments();
            var whitelistKey = $"{namespaceName}.{name}";
            var xmlDocKey = $"T:{whitelistKey}";
            xmlDocKey += "{" + string.Join(",", genericArguments.Select(arg => arg.IsGenericParameter ? arg.Name : arg.FullName)) + "}";
            return new ApiEntry(api, type, assemblyName, namespaceName, name, xmlDocKey, whitelist.IsWhitelisted(type));
        }

        static ApiEntry ForConstructor(ProgrammableBlockApi api, Whitelist whitelist, ConstructorInfo constructorInfo)
        {
            //if (constructorInfo.IsSpecialName || !constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyOrAssembly && !constructorInfo.IsFamilyOrAssembly)
            //    return null;
            if (!constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyOrAssembly && !constructorInfo.IsFamilyOrAssembly)
                return null;
            var basis = api.GetEntry(constructorInfo.DeclaringType);
            var xmlDocKey = $"C{basis.XmlDocKey.Substring(1)}.{constructorInfo.Name}";
            var parameters = constructorInfo.GetParameters();
            xmlDocKey += "(" + string.Join(",", parameters.Select(p => XmlDocParameterStr(api, p))) + ")";
            return new ApiEntry(api, constructorInfo, basis.AssemblyName, basis.NamespaceName, constructorInfo.Name, xmlDocKey, whitelist.IsWhitelisted(constructorInfo));
        }

        static ApiEntry ForEvent(ProgrammableBlockApi api, Whitelist whitelist, EventInfo eventInfo)
        {
            if (eventInfo.IsSpecialName || !(eventInfo.AddMethod?.IsPublic ?? false) && !(eventInfo.AddMethod?.IsFamily ?? false) && !(eventInfo.AddMethod?.IsFamilyOrAssembly ?? false) && !(eventInfo.AddMethod?.IsFamilyOrAssembly ?? false)
                && !(eventInfo.RemoveMethod?.IsPublic ?? false) && !(eventInfo.RemoveMethod?.IsFamily ?? false) && !(eventInfo.RemoveMethod?.IsFamilyOrAssembly ?? false) && !(eventInfo.RemoveMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = api.GetEntry(eventInfo.DeclaringType);
            var xmlDocKey = $"E{basis.XmlDocKey.Substring(1)}.{eventInfo.Name}";
            return new ApiEntry(api, eventInfo, basis.AssemblyName, basis.NamespaceName, eventInfo.Name, xmlDocKey, whitelist.IsWhitelisted(eventInfo));
        }

        static ApiEntry ForField(ProgrammableBlockApi api, Whitelist whitelist, FieldInfo fieldInfo)
        {
            if (fieldInfo.IsSpecialName || !fieldInfo.IsPublic && !fieldInfo.IsFamily && !fieldInfo.IsFamilyOrAssembly && !fieldInfo.IsFamilyOrAssembly)
                return null;
            var basis = api.GetEntry(fieldInfo.DeclaringType);
            var xmlDocKey = $"F{basis.XmlDocKey.Substring(1)}.{fieldInfo.Name}";
            return new ApiEntry(api, fieldInfo, basis.AssemblyName, basis.NamespaceName, fieldInfo.Name, xmlDocKey, whitelist.IsWhitelisted(fieldInfo));
        }

        static ApiEntry ForProperty(ProgrammableBlockApi api, Whitelist whitelist, PropertyInfo propertyInfo)
        {
            if (propertyInfo.IsSpecialName || !(propertyInfo.GetMethod?.IsPublic ?? false) && !(propertyInfo.GetMethod?.IsFamily ?? false) && !(propertyInfo.GetMethod?.IsFamilyOrAssembly ?? false) && !(propertyInfo.GetMethod?.IsFamilyOrAssembly ?? false)
                && !(propertyInfo.SetMethod?.IsPublic ?? false) && !(propertyInfo.SetMethod?.IsFamily ?? false) && !(propertyInfo.SetMethod?.IsFamilyOrAssembly ?? false) && !(propertyInfo.SetMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = api.GetEntry(propertyInfo.DeclaringType);
            var xmlDocKey = $"P{basis.XmlDocKey.Substring(1)}.{propertyInfo.Name}";
            return new ApiEntry(api, propertyInfo, basis.AssemblyName, basis.NamespaceName, propertyInfo.Name, xmlDocKey, whitelist.IsWhitelisted(propertyInfo));
        }

        static ApiEntry ForMethod(ProgrammableBlockApi api, Whitelist whitelist, MethodInfo methodInfo)
        {
            if (methodInfo.IsSpecialName || !methodInfo.IsPublic && !methodInfo.IsFamily && !methodInfo.IsFamilyOrAssembly && !methodInfo.IsFamilyOrAssembly)
                return null;

            if (methodInfo.IsGenericMethodDefinition || methodInfo.IsGenericMethod)
                return ForGenericMethod(api, whitelist, methodInfo);
            var basis = api.GetEntry(methodInfo.DeclaringType);
            var xmlDocKey = $"M{basis.XmlDocKey.Substring(1)}.{methodInfo.Name}";
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
                xmlDocKey += $"({string.Join(",", parameters.Select(p => XmlDocParameterStr(api, p)))})";

            return new ApiEntry(api, methodInfo, basis.AssemblyName, basis.NamespaceName, methodInfo.Name, xmlDocKey, whitelist.IsWhitelisted(methodInfo));
        }

        static ApiEntry ForGenericMethod(ProgrammableBlockApi api, Whitelist whitelist, MethodInfo methodInfo)
        {
            var basis = api.GetEntry(methodInfo.DeclaringType);
            var name = methodInfo.Name;
            var separatorIndex = name.IndexOf('`');
            if (separatorIndex >= 0)
                name = name.Substring(0, separatorIndex);
            var xmlDocKey = $"M{basis.XmlDocKey.Substring(1)}.{methodInfo.Name}";
            var genericArguments = methodInfo.GetGenericArguments();
            xmlDocKey += $"{{{string.Join(",", genericArguments.Select(arg => arg.Name))}}}";
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
                xmlDocKey += $"({string.Join(",", parameters.Select(p => XmlDocParameterStr(api, p)))})";

            return new ApiEntry(api, methodInfo, basis.AssemblyName, basis.NamespaceName, name, xmlDocKey, whitelist.IsWhitelisted(methodInfo));
        }

        static string XmlDocParameterStr(ProgrammableBlockApi api, ParameterInfo parameterInfo)
        {
            var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
                type = type.GetGenericTypeDefinition();
            if (parameterInfo.ParameterType.IsByRef)
            {
                if (type.FullName == null)
                    return type.Name + "@";
                return api.GetEntry(type).XmlDocKey.Substring(2) + "@";
            }

            if (type.FullName == null)
                return type.Name;
            return api.GetEntry(type, true).XmlDocKey.Substring(2);
        }

        static IEnumerable<Type> DeclarersOf(Type type, bool inclusive)
        {
            if (!inclusive)
                type = type.DeclaringType;
            while (type != null)
            {
                yield return type;
                type = type.DeclaringType;
            }
        }

        List<ApiEntry> _inheritedEntries = new List<ApiEntry>();
        List<ApiEntry> _inheritorEntries = new List<ApiEntry>();
        List<ApiEntry> _memberEntries = new List<ApiEntry>();
        Dictionary<ApiEntryStringFlags, string> _stringCache = new Dictionary<ApiEntryStringFlags, string>();

        ApiEntry(ProgrammableBlockApi api, MemberInfo member, string assemblyName, string namespaceName, string name, string xmlDocKey, bool isWhitelisted)
        {
            Api = api ?? throw new ArgumentNullException(nameof(api));
            Member = member ?? throw new ArgumentNullException(nameof(member));
            AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
            NamespaceName = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            XmlDocKey = xmlDocKey ?? throw new ArgumentNullException(nameof(xmlDocKey));
            IsWhitelisted = isWhitelisted;
            FullName = $"{NamespaceName}.{Name}";

            InheritedEntries = new ReadOnlyCollection<ApiEntry>(_inheritedEntries);
            InheritorEntries = new ReadOnlyCollection<ApiEntry>(_inheritorEntries);
            MemberEntries = new ReadOnlyCollection<ApiEntry>(_memberEntries);

            SuggestedFileName = ToSafeFileName(member.GetFullName());
        }

        public string SuggestedFileName { get; set; }

        static string ToSafeFileName(string path)
        {
            var builder = new StringBuilder(path);
            for (var i = 0; i < builder.Length; i++)
                if (InvalidCharacters.Contains(builder[i]))
                    builder[i] = '_';

            builder.Append(".md");
            return builder.ToString();
        }

        public XmlDoc Documentation { get; set; }
        public ProgrammableBlockApi Api { get; }
        public MemberInfo Member { get; }
        public ApiEntry DeclaringEntry { get; private set; }
        public ApiEntry BaseEntry { get; private set; }
        public ReadOnlyCollection<ApiEntry> MemberEntries { get; }
        public ReadOnlyCollection<ApiEntry> InheritedEntries { get; }
        public ReadOnlyCollection<ApiEntry> InheritorEntries { get; }
        public string AssemblyName { get; }
        public string NamespaceName { get; }
        public string Name { get; }

        public string FullName { get; }

        //public string WhitelistKey { get; }
        public string XmlDocKey { get; }
        public bool IsWhitelisted { get; }

        public bool IsStatic => Member is MethodBase methodBase && methodBase.IsStatic
                                || Member is EventInfo eventInfo && (eventInfo.GetAddMethod(true)?.IsStatic ?? eventInfo.GetRemoveMethod(true)?.IsStatic ?? false)
                                || Member is PropertyInfo propertyInfo && (propertyInfo.GetGetMethod(true)?.IsStatic ?? propertyInfo.GetSetMethod()?.IsStatic ?? false)
                                || Member is FieldInfo fieldInfo && fieldInfo.IsStatic;

        public override string ToString() => ToString(ApiEntryStringFlags.Default);

        public string ToString(ApiEntryStringFlags flags)
        {
            if (_stringCache.TryGetValue(flags, out var str))
                return str;

            switch (Member)
            {
                case Type type:
                    _stringCache[flags] = str = ToTypeString(type, flags);
                    return str;
                case EventInfo eventInfo:
                    _stringCache[flags] = str = ToEventString(eventInfo, flags);
                    return str;
                case FieldInfo fieldInfo:
                    _stringCache[flags] = str = ToFieldString(fieldInfo, flags);
                    return str;
                case PropertyInfo propertyInfo:
                    _stringCache[flags] = str = ToPropertyString(propertyInfo, flags);
                    return str;
                case ConstructorInfo constructorInfo:
                    _stringCache[flags] = str = ToConstructorString(constructorInfo, flags);
                    return str;
                case MethodInfo methodInfo:
                    _stringCache[flags] = str = ToMethodString(methodInfo, flags);
                    return str;
            }

            throw new NotSupportedException();
        }

        string ToParameterString(ParameterInfo parameterInfo, int index, ApiEntryStringFlags flags)
        {
            var segments = new List<string>();

            if (flags.HasFlag(ApiEntryStringFlags.ParameterTypes))
            {
                var prefix = "";
                if (parameterInfo.Member.IsDefined(typeof(ExtensionAttribute), false) && index == 0)
                    prefix += "this\u00A0";
                prefix += parameterInfo.ParameterType.IsByRef ? "ref\u00A0" : parameterInfo.ParameterType.IsPointer ? "*" : parameterInfo.IsOut ? "out\u00A0" : "";
                var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
                segments.Add(prefix + Api.GetEntry(type, true).ToString(ForSubCalls(flags)));
            }

            if (flags.HasFlag(ApiEntryStringFlags.ParameterNames))
                segments.Add(parameterInfo.Name);

            return string.Join(" ", segments);
        }

        string ToConstructorString(ConstructorInfo constructorInfo, ApiEntryStringFlags flags)
        {
            var buffer = new StringBuilder();
            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
            {
                buffer.Append(constructorInfo.GetModifiers().ToCodeString());
                buffer.Append(" ");
            }

            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(constructorInfo.DeclaringType, true).Reverse())
                {
                    buffer.Append(Api.GetEntry(item).ToString(ForSubCalls(flags)));
                    buffer.Append(".");
                }
            }
            if (flags.HasFlag(ApiEntryStringFlags.CliNames))
                buffer.Append(constructorInfo.Name);
            else
                buffer.Append(Api.GetEntry(constructorInfo.DeclaringType).ToString(ApiEntryStringFlags.None));

            if (flags.HasFlag(ApiEntryStringFlags.ParameterTypes) || flags.HasFlag(ApiEntryStringFlags.ParameterNames))
            {
                var parameters = constructorInfo.GetParameters();
                buffer.Append("(");
                buffer.Append(string.Join(", ", parameters.Select((p, i) => ToParameterString(p, i, flags))));
                buffer.Append(")");
            }

            return buffer.ToString();
        }

        string ToMethodString(MethodInfo methodInfo, ApiEntryStringFlags flags)
        {
            var segments = new List<string>();
            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
                segments.Add(methodInfo.GetModifiers().ToCodeString());

            if (flags.HasFlag(ApiEntryStringFlags.ReturnValue))
            {
                if (methodInfo.ReturnType == typeof(void))
                    segments.Add("void");
                else
                    segments.Add(Api.GetEntry(methodInfo.ReturnType, true).ToString(ForSubCalls(flags)));
            }

            var buffer = new StringBuilder();
            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(methodInfo.DeclaringType, true).Reverse())
                {
                    buffer.Append(Api.GetEntry(item).ToString(ForSubCalls(flags)));
                    buffer.Append(".");
                }
            }

            if (flags.HasFlag(ApiEntryStringFlags.CliNames) || (!methodInfo.IsGenericMethodDefinition && !methodInfo.IsGenericMethod))
                buffer.Append(methodInfo.Name);
            else
            {
                var name = methodInfo.Name;
                var separatorIndex = name.IndexOf('`');
                if (separatorIndex >= 0)
                    name = name.Substring(0, separatorIndex);
                buffer.Append(name);
                if (flags.HasFlag(ApiEntryStringFlags.GenericParameters))
                {
                    var genericArguments = methodInfo.GetGenericArguments();
                    buffer.Append("<");
                    buffer.Append(string.Join(", ", genericArguments.Select(arg => arg.Name)));
                    buffer.Append(">");
                }
            }

            if (flags.HasFlag(ApiEntryStringFlags.ParameterTypes) || flags.HasFlag(ApiEntryStringFlags.ParameterNames))
            {
                var parameters = methodInfo.GetParameters();
                buffer.Append("(");
                buffer.Append(string.Join(", ", parameters.Select((p, i) => ToParameterString(p, i, flags))));
                buffer.Append(")");
            }

            segments.Add(buffer.ToString());

            return string.Join(" ", segments);
        }

        string ToPropertyString(PropertyInfo propertyInfo, ApiEntryStringFlags flags)
        {
            var segments = new List<string>();

            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
                segments.Add(propertyInfo.GetModifiers().ToCodeString());

            if (flags.HasFlag(ApiEntryStringFlags.ReturnValue))
                segments.Add(Api.GetEntry(propertyInfo.PropertyType, true).ToString(ForSubCalls(flags)));

            var buffer = new StringBuilder();
            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(propertyInfo.DeclaringType, true).Reverse())
                {
                    buffer.Append(Api.GetEntry(item).ToString(ForSubCalls(flags)));
                    buffer.Append(".");
                }
            }

            buffer.Append(propertyInfo.Name);
            segments.Add(buffer.ToString());

            if (flags.HasFlag(ApiEntryStringFlags.Accessors))
            {
                segments.Add("{");
                var getter = propertyInfo.GetMethod;
                if (getter != null && !getter.IsPrivate)
                {
                    var modifier = getter.GetModifiers() & ~propertyInfo.GetModifiers();
                    if (modifier != TypeModifiers.None)
                        segments.Add(modifier.ToCodeString());
                    segments.Add("get;");
                }
                var setter = propertyInfo.SetMethod;
                if (setter != null && !setter.IsPrivate)
                {
                    var modifier = setter.GetModifiers() & ~propertyInfo.GetModifiers();
                    if (modifier != TypeModifiers.None)
                        segments.Add(modifier.ToCodeString());
                    segments.Add("set;");
                }
                segments.Add("}");
            }

            return string.Join(" ", segments);
        }

        string ToFieldString(FieldInfo fieldInfo, ApiEntryStringFlags flags)
        {
            var segments = new List<string>();

            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
                segments.Add(fieldInfo.GetModifiers().ToCodeString());

            if (flags.HasFlag(ApiEntryStringFlags.ReturnValue))
                segments.Add(Api.GetEntry(fieldInfo.FieldType, true).ToString(ForSubCalls(flags)));

            var buffer = new StringBuilder();
            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(fieldInfo.DeclaringType, true).Reverse())
                {
                    buffer.Append(Api.GetEntry(item, true).ToString(ForSubCalls(flags)));
                    buffer.Append(".");
                }
            }

            buffer.Append(fieldInfo.Name);
            segments.Add(buffer.ToString());

            return string.Join(" ", segments);
        }

        string ToEventString(EventInfo eventInfo, ApiEntryStringFlags flags)
        {
            var segments = new List<string>();

            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
                segments.Add(eventInfo.GetModifiers().ToCodeString());

            if (flags.HasFlag(ApiEntryStringFlags.ReturnValue))
                segments.Add(Api.GetEntry(eventInfo.EventHandlerType, true).ToString(ForSubCalls(flags)));

            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
                segments.Add("event");

            var buffer = new StringBuilder();
            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(eventInfo.DeclaringType, true).Reverse())
                {
                    buffer.Append(Api.GetEntry(item, true).ToString(ForSubCalls(flags)));
                    buffer.Append(".");
                }
            }

            buffer.Append(eventInfo.Name);
            segments.Add(buffer.ToString());

            return string.Join(" ", segments);
        }

        string ToTypeString(Type type, ApiEntryStringFlags flags)
        {
            if (!flags.HasFlag(ApiEntryStringFlags.CliTypeNames))
            {
                var alias = type.GetLanguageAlias();
                if (alias != null)
                    return alias;
            }

            var segments = new List<string>();

            if (flags.HasFlag(ApiEntryStringFlags.Namespaces))
                segments.Add(type.Namespace);

            if (flags.HasFlag(ApiEntryStringFlags.DeclaringTypes))
            {
                foreach (var item in DeclarersOf(type, false).Reverse())
                    segments.Add(Api.GetEntry(item, true).ToString(ForSubCalls(flags)));
            }

            if (flags.HasFlag(ApiEntryStringFlags.CliNames) || (!type.IsGenericType && !type.IsGenericTypeDefinition))
                segments.Add(type.Name);
            else
            {
                var name = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
                var separatorIndex = name.IndexOf('`');
                if (separatorIndex >= 0)
                    name = name.Substring(0, separatorIndex);
                if (flags.HasFlag(ApiEntryStringFlags.GenericParameters))
                {
                    var genericArguments = type.GetGenericArguments();
                    name += $"<{string.Join(", ", genericArguments.Select(arg => arg.IsGenericParameter ? arg.Name : arg.FullName))}>";
                }

                segments.Add(name);
            }
            var fullName = string.Join(".", segments);

            if (flags.HasFlag(ApiEntryStringFlags.Inheritance) && (BaseEntry != null || InheritedEntries.Count > 0))
            {
                segments.Clear();
                fullName += ": ";
                if (BaseEntry != null)
                    segments.Add(BaseEntry.ToString(ForSubCalls(flags)));
                foreach (var iface in InheritedEntries)
                    segments.Add(iface.ToString(ForSubCalls(flags)));
                fullName += string.Join(", ", segments);
            }

            if (flags.HasFlag(ApiEntryStringFlags.Modifiers))
            {
                var n = type.GetModifiers().ToCodeString();
                if (n.Length > 0)
                    n += " ";
                if (type.IsEnum)
                    n += "enum ";
                else if (type.IsInterface)
                    n += "interface ";
                else if (type.IsClass)
                    n += "class ";
                else
                    n += "struct ";

                fullName = n + fullName;
            }

            return fullName;
        }

        ApiEntryStringFlags ForSubCalls(ApiEntryStringFlags flags) => flags & ~(ApiEntryStringFlags.Accessors | ApiEntryStringFlags.Inheritance | ApiEntryStringFlags.Modifiers);

        public void ResolveLinks()
        {
            if (Member.DeclaringType != null)
            {
                DeclaringEntry = Api.GetEntry(Member.DeclaringType, true);
                DeclaringEntry?._memberEntries.Add(this);
            }

            if (Member is Type type && !type.IsEnum)
            {
                if (type.BaseType != null && type.BaseType != typeof(object) && !type.IsValueType)
                {
                    var entry = Api.GetEntry(type.BaseType, true);
                    if (entry != null)
                    {
                        BaseEntry = entry;
                        entry._inheritorEntries.Add(this);
                    }
                }

                var interfaces = type.GetInterfaces();
                foreach (var interfaceType in interfaces)
                {
                    var entry = Api.GetEntry(interfaceType, true);
                    if (entry != null)
                    {
                        _inheritedEntries.Add(entry);
                        entry._inheritorEntries.Add(this);
                    }
                }
            }
        }
    }
}