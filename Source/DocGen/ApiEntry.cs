using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DocGen
{
    class ApiEntry
    {
        public static ApiEntry Create(MemberInfo memberInfo, XDocument documentation)
        {
            var instance = CreateInstance(memberInfo);
            if (instance == null)
                return null;
            var doc = documentation?.XPathSelectElement($"/doc/members/member[@name='{instance.XmlDocKey}']");
            instance.Documentation = XmlDoc.Generate(doc);
            return instance;
        }

        public XmlDoc Documentation { get; set; }

        static ApiEntry CreateInstance(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return ForType(type);
                case ConstructorInfo constructorInfo:
                    return ForConstructor(constructorInfo);
                case EventInfo eventInfo:
                    return ForEvent(eventInfo);
                case FieldInfo fieldInfo:
                    return ForField(fieldInfo);
                case PropertyInfo propertyInfo:
                    return ForProperty(propertyInfo);
                case MethodInfo methodInfo:
                    return ForMethod(methodInfo);
                default:
                    return null;
            }
        }

        static ApiEntry ForType(Type type)
        {
            if (!type.IsPublic && !type.IsNestedPublic && !type.IsNestedFamily && !type.IsNestedFamANDAssem && !type.IsNestedFamORAssem)
                return null;
            if (type.IsGenericTypeDefinition || type.IsGenericType)
                return ForGenericType(type);

            var assemblyName = type.Assembly.GetName().Name;
            var namespaceName = type.Namespace;
            var name = type.Name;
            var whitelistKey = $"{namespaceName}.{name}";
            var xmlDocKey = $"T:{namespaceName}.{name}";
            return new ApiEntry(type, assemblyName, namespaceName, name, name, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForGenericType(Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var namespaceName = type.IsGenericType ? type.GetGenericTypeDefinition().Namespace : type.Namespace;
            var name = type.IsGenericType ? type.GetGenericTypeDefinition().Name : type.Name;
            var separatorIndex = name.IndexOf('`');
            if (separatorIndex >= 0)
                name = name.Substring(0, separatorIndex);
            var signature = name;
            var genericArguments = type.GetGenericArguments();
            var whitelistKey = $"{namespaceName}.{name}";
            var xmlDocKey = $"T:{whitelistKey}";
            signature += "<" + string.Join(", ", genericArguments.Select(arg => arg.IsGenericParameter ? arg.Name : arg.FullName)) + ">";
            whitelistKey += "<" + string.Join(", ", genericArguments.Select(arg => arg.IsGenericParameter ? arg.Name : arg.FullName)) + ">";
            xmlDocKey += "{" + string.Join(", ", genericArguments.Select(arg => arg.IsGenericParameter ? arg.Name : arg.FullName)) + "}";
            return new ApiEntry(type, assemblyName, namespaceName, name, signature, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForConstructor(ConstructorInfo constructorInfo)
        {
            if (constructorInfo.IsSpecialName || !constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyAndAssembly && !constructorInfo.IsFamilyOrAssembly)
                return null;
            var basis = ForType(constructorInfo.DeclaringType);
            var signature = constructorInfo.Name;
            var whitelistKey = $"{basis.WhitelistKey}.{constructorInfo.Name}";
            var xmlDocKey = "C" + basis.XmlDocKey.Substring(1);
            var parameters = constructorInfo.GetParameters();
            signature += "(" + string.Join(", ", parameters.Select(SignatureParameterStr)) + ")";
            whitelistKey += "(" + string.Join(", ", parameters.Select(WhitelistParameterStr)) + ")";
            xmlDocKey += "(" + string.Join(", ", parameters.Select(XmlDocParameterStr)) + ")";
            return new ApiEntry(constructorInfo, basis.AssemblyName, basis.NamespaceName, constructorInfo.Name, signature, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForEvent(EventInfo eventInfo)
        {
            if (eventInfo.IsSpecialName || !(eventInfo.AddMethod?.IsPublic ?? false) && !(eventInfo.AddMethod?.IsFamily ?? false) && !(eventInfo.AddMethod?.IsFamilyAndAssembly ?? false) && !(eventInfo.AddMethod?.IsFamilyOrAssembly ?? false)
                && !(eventInfo.RemoveMethod?.IsPublic ?? false) && !(eventInfo.RemoveMethod?.IsFamily ?? false) && !(eventInfo.RemoveMethod?.IsFamilyAndAssembly ?? false) && !(eventInfo.RemoveMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = ForType(eventInfo.DeclaringType);
            var whitelistKey = $"{basis.WhitelistKey}.{eventInfo.Name}";
            var xmlDocKey = "E" + basis.XmlDocKey.Substring(1);
            return new ApiEntry(eventInfo, basis.AssemblyName, basis.NamespaceName, eventInfo.Name, eventInfo.Name, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForField(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsSpecialName || !fieldInfo.IsPublic && !fieldInfo.IsFamily && !fieldInfo.IsFamilyAndAssembly && !fieldInfo.IsFamilyOrAssembly)
                return null;
            var basis = ForType(fieldInfo.DeclaringType);
            var whitelistKey = $"{basis.WhitelistKey}.{fieldInfo.Name}";
            var xmlDocKey = "F" + basis.XmlDocKey.Substring(1);
            var signature = $"{ForType(fieldInfo.FieldType).Signature} {fieldInfo.Name}";
            return new ApiEntry(fieldInfo, basis.AssemblyName, basis.NamespaceName, fieldInfo.Name, signature, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForProperty(PropertyInfo propertyInfo)
        {
            if (propertyInfo.IsSpecialName || !(propertyInfo.GetMethod?.IsPublic ?? false) && !(propertyInfo.GetMethod?.IsFamily ?? false) && !(propertyInfo.GetMethod?.IsFamilyAndAssembly ?? false) && !(propertyInfo.GetMethod?.IsFamilyOrAssembly ?? false)
                && !(propertyInfo.SetMethod?.IsPublic ?? false) && !(propertyInfo.SetMethod?.IsFamily ?? false) && !(propertyInfo.SetMethod?.IsFamilyAndAssembly ?? false) && !(propertyInfo.SetMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = ForType(propertyInfo.DeclaringType);
            var whitelistKey = $"{basis.WhitelistKey}.{propertyInfo.Name}";
            var xmlDocKey = "P" + basis.XmlDocKey.Substring(1);
            var signature = $"{ForType(propertyInfo.PropertyType).Signature} {propertyInfo.Name}";
            return new ApiEntry(propertyInfo, basis.AssemblyName, basis.NamespaceName, propertyInfo.Name, signature, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForMethod(MethodInfo methodInfo)
        {
            if (methodInfo.IsSpecialName || !methodInfo.IsPublic && !methodInfo.IsFamily && !methodInfo.IsFamilyAndAssembly && !methodInfo.IsFamilyOrAssembly)
                return null;

            if (methodInfo.IsGenericMethodDefinition || methodInfo.IsGenericMethod)
                return ForGenericMethod(methodInfo);
            var basis = ForType(methodInfo.DeclaringType);
            var signature = methodInfo.Name;
            var whitelistKey = $"{basis.WhitelistKey}.{methodInfo.Name}";
            var xmlDocKey = "M" + basis.XmlDocKey.Substring(1);

            var parameters = methodInfo.GetParameters();
            signature += "(" + string.Join(", ", parameters.Select(SignatureParameterStr)) + ")";
            whitelistKey += "(" + string.Join(", ", parameters.Select(WhitelistParameterStr)) + ")";
            xmlDocKey += "(" + string.Join(", ", parameters.Select(XmlDocParameterStr)) + ")";

            if (methodInfo.ReturnType == typeof(void))
                signature = $"void {signature}";
            else
                signature = $"{ForType(methodInfo.ReturnType)?.Signature} {signature}";
            return new ApiEntry(methodInfo, basis.AssemblyName, basis.NamespaceName, methodInfo.Name, signature, whitelistKey, xmlDocKey);
        }

        static ApiEntry ForGenericMethod(MethodInfo methodInfo)
        {
            var basis = ForType(methodInfo.DeclaringType);
            var name = methodInfo.Name;
            var separatorIndex = name.IndexOf('`');
            if (separatorIndex >= 0)
                name = name.Substring(0, separatorIndex);
            var signature = name;
            var whitelistKey = $"{basis.WhitelistKey}.{methodInfo.Name}";
            var xmlDocKey = "M" + basis.XmlDocKey.Substring(1);
            var genericArguments = methodInfo.GetGenericArguments();
            signature += "<" + string.Join(", ", genericArguments.Select(arg => arg.Name)) + ">";
            whitelistKey += "<" + string.Join(", ", genericArguments.Select(arg => arg.Name)) + ">";
            xmlDocKey += "{" + string.Join(", ", genericArguments.Select(arg => arg.Name)) + "}";
            var parameters = methodInfo.GetParameters();
            signature += "(" + string.Join(", ", parameters.Select(SignatureParameterStr)) + ")";
            whitelistKey += "(" + string.Join(", ", parameters.Select(WhitelistParameterStr)) + ")";
            xmlDocKey += "(" + string.Join(", ", parameters.Select(XmlDocParameterStr)) + ")";

            if (methodInfo.ReturnType == typeof(void))
                signature = $"void {signature}";
            else
                signature = $"{ForType(methodInfo.ReturnType).Signature} {signature}";
            return new ApiEntry(methodInfo, basis.AssemblyName, basis.NamespaceName, name, signature, whitelistKey, xmlDocKey);
        }

        static string SignatureParameterStr(ParameterInfo parameterInfo)
        {
            var prefix = parameterInfo.ParameterType.IsByRef ? "ref " : parameterInfo.ParameterType.IsPointer ? "*" : parameterInfo.IsOut ? "out " : "";
            var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
            return prefix + ForType(type).Signature;
        }

        static string WhitelistParameterStr(ParameterInfo parameterInfo)
        {
            var prefix = parameterInfo.ParameterType.IsByRef ? "ref " : parameterInfo.ParameterType.IsPointer ? "*" : parameterInfo.IsOut ? "out " : "";
            var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
            return prefix + ForType(type).WhitelistKey;
        }

        static string XmlDocParameterStr(ParameterInfo parameterInfo)
        {
            var prefix = parameterInfo.ParameterType.IsByRef ? "ref " : parameterInfo.ParameterType.IsPointer ? "*" : parameterInfo.IsOut ? "out " : "";
            var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
            return prefix + ForType(type).XmlDocKey.Substring(2);
        }

        List<ApiEntry> _inheritedEntries = new List<ApiEntry>();
        List<ApiEntry> _inheritorEntries = new List<ApiEntry>();
        List<ApiEntry> _memberEntries = new List<ApiEntry>();

        public ApiEntry(MemberInfo member, string assemblyName, string namespaceName, string name, string signature, string whitelistKey, string xmlDocKey)
        {
            Member = member;
            AssemblyName = assemblyName;
            NamespaceName = namespaceName;
            Name = name;
            Signature = signature;
            WhitelistKey = whitelistKey;
            XmlDocKey = xmlDocKey;
            FullName = $"{NamespaceName}.{name}";

            InheritedEntries = new ReadOnlyCollection<ApiEntry>(_inheritedEntries);
            InheritorEntries = new ReadOnlyCollection<ApiEntry>(_inheritorEntries);
            MemberEntries = new ReadOnlyCollection<ApiEntry>(_memberEntries);
        }

        public XElement DocumentationElement { get; private set; }

        public MemberInfo Member { get; }
        public ApiEntry DeclaringEntry { get; private set; }
        public ApiEntry BaseEntry { get; private set; }
        public ReadOnlyCollection<ApiEntry> MemberEntries { get; }
        public ReadOnlyCollection<ApiEntry> InheritedEntries { get; }
        public ReadOnlyCollection<ApiEntry> InheritorEntries { get; }
        public string AssemblyName { get; }
        public string NamespaceName { get; }
        public string Name { get; }
        public string Signature { get; }
        public string FullName { get; }
        public string WhitelistKey { get; }
        public string XmlDocKey { get; }

        public void ResolveLinks(List<ApiEntry> entries)
        {
            DeclaringEntry = entries.FirstOrDefault(e => e.Member == Member.DeclaringType);
            DeclaringEntry?._memberEntries.Add(this);
            if (Member is Type type)
            {
                var entry = entries.FirstOrDefault(e => e.Member == type.BaseType);
                if (entry != null)
                {
                    BaseEntry = entry;
                    entry._inheritorEntries.Add(this);
                }

                var interfaces = type.GetInterfaces();
                foreach (var interfaceType in interfaces)
                {
                    entry = entries.FirstOrDefault(e => e.Member == interfaceType);
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