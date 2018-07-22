using System;
using System.Linq;
using System.Reflection;
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
            entry = new WhitelistKey(assemblyName, path, regex, null);
            return true;
        }

        public static WhitelistKey ForType(Type type, bool includeNamespace = true)
        {
            if (!type.IsPublic && !type.IsNestedPublic && !type.IsNestedFamily && !type.IsNestedFamANDAssem && !type.IsNestedFamORAssem)
                return null;
            //if (type.IsGenericType)
            //    type = type.GetGenericTypeDefinition();
            if (type.IsGenericTypeDefinition || type.IsGenericType)
                return ForGenericType(type, includeNamespace);
            var assemblyName = type.Assembly.GetName().Name;
            var path = includeNamespace? type.FullName : type.Name;
            return new WhitelistKey(assemblyName, path, null, "T");
        }

        static WhitelistKey ForGenericType(Type type, bool includeNamespace = true)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var path = type.IsGenericType? (includeNamespace? type.GetGenericTypeDefinition().FullName : type.GetGenericTypeDefinition().Name) : (includeNamespace ? type.FullName : type.Name);
            var separatorIndex = path.IndexOf('`');
            if (separatorIndex >= 0)
                path = path.Substring(0, separatorIndex);
            var genericArguments = type.GetGenericArguments();
            path += "<" + string.Join(", ", genericArguments.Select(arg => arg.IsGenericParameter? arg.Name : arg.FullName)) + ">";
            return new WhitelistKey(assemblyName, path, null, "T");
        }

        public static WhitelistKey ForMember(MemberInfo memberInfo, bool includeTypeName = true)
        {
            switch (memberInfo)
            {
                case ConstructorInfo constructorInfo:
                    return ForConstructor(constructorInfo, includeTypeName);
                case EventInfo eventInfo:
                    return ForEvent(eventInfo, includeTypeName);
                case FieldInfo fieldInfo:
                    return ForField(fieldInfo, includeTypeName);
                case PropertyInfo propertyInfo:
                    return ForProperty(propertyInfo, includeTypeName);
                case MethodInfo methodInfo:
                    return ForMethod(methodInfo, includeTypeName);
                default:
                    return null;
            }
        }

        static WhitelistKey ForConstructor(ConstructorInfo constructorInfo, bool includeTypeName)
        {
            if (constructorInfo.IsSpecialName || !constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyAndAssembly && !constructorInfo.IsFamilyOrAssembly)
                return null;
            var basis = ForType(constructorInfo.DeclaringType);
            var path = includeTypeName? basis.Path + "." + constructorInfo.Name : constructorInfo.Name;
            var parameters = constructorInfo.GetParameters();
            path += "(" + string.Join(", ", parameters.Select(ParameterStr)) + ")";
            return new WhitelistKey(basis.AssemblyName, path, null, "C");
        }

        static WhitelistKey ForEvent(EventInfo eventInfo, bool includeTypeName)
        {
            if (eventInfo.IsSpecialName || !(eventInfo.AddMethod?.IsPublic ?? false) && !(eventInfo.AddMethod?.IsFamily ?? false) && !(eventInfo.AddMethod?.IsFamilyAndAssembly ?? false) && !(eventInfo.AddMethod?.IsFamilyOrAssembly ?? false)
                && !(eventInfo.RemoveMethod?.IsPublic ?? false) && !(eventInfo.RemoveMethod?.IsFamily ?? false) && !(eventInfo.RemoveMethod?.IsFamilyAndAssembly ?? false) && !(eventInfo.RemoveMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = ForType(eventInfo.DeclaringType);
            return new WhitelistKey(basis.AssemblyName, includeTypeName? basis.Path + "." + eventInfo.Name: eventInfo.Name, null, "E");
        }

        static WhitelistKey ForField(FieldInfo fieldInfo, bool includeTypeName)
        {
            if (fieldInfo.IsSpecialName || !fieldInfo.IsPublic && !fieldInfo.IsFamily && !fieldInfo.IsFamilyAndAssembly && !fieldInfo.IsFamilyOrAssembly)
                return null;
            var basis = ForType(fieldInfo.DeclaringType);
            return new WhitelistKey(basis.AssemblyName, includeTypeName? basis.Path + "." + fieldInfo.Name: fieldInfo.Name, null, "F");
        }

        static WhitelistKey ForProperty(PropertyInfo propertyInfo, bool includeTypeName)
        {
            if (propertyInfo.IsSpecialName || !(propertyInfo.GetMethod?.IsPublic ?? false) && !(propertyInfo.GetMethod?.IsFamily ?? false) && !(propertyInfo.GetMethod?.IsFamilyAndAssembly ?? false) && !(propertyInfo.GetMethod?.IsFamilyOrAssembly ?? false)
                && !(propertyInfo.SetMethod?.IsPublic ?? false) && !(propertyInfo.SetMethod?.IsFamily ?? false) && !(propertyInfo.SetMethod?.IsFamilyAndAssembly ?? false) && !(propertyInfo.SetMethod?.IsFamilyOrAssembly ?? false))
                return null;
            var basis = ForType(propertyInfo.DeclaringType);
            return new WhitelistKey(basis.AssemblyName, includeTypeName? basis.Path + "." + propertyInfo.Name : propertyInfo.Name, null, "P");
        }

        static WhitelistKey ForMethod(MethodInfo methodInfo, bool includeTypeName)
        {
            if (methodInfo.IsSpecialName || !methodInfo.IsPublic && !methodInfo.IsFamily && !methodInfo.IsFamilyAndAssembly && !methodInfo.IsFamilyOrAssembly)
                return null;

            //if (methodInfo.IsGenericMethod)
            //    methodInfo = methodInfo.GetGenericMethodDefinition();
            if (methodInfo.IsGenericMethodDefinition || methodInfo.IsGenericMethod)
                return ForGenericMethod(methodInfo, includeTypeName);
            var basis = ForType(methodInfo.DeclaringType);
            var path = includeTypeName? basis.Path + "." + methodInfo.Name : methodInfo.Name;
            var parameters = methodInfo.GetParameters();
            path += "(" + string.Join(", ", parameters.Select(ParameterStr)) + ")";
            return new WhitelistKey(basis.AssemblyName, path, null, "M");
        }

        static WhitelistKey ForGenericMethod(MethodInfo methodInfo, bool includeTypeName)
        {
            var basis = ForType(methodInfo.DeclaringType);
            var path = includeTypeName? basis.Path + "." + methodInfo.Name : methodInfo.Name;
            var separatorIndex = path.IndexOf('`');
            if (separatorIndex >= 0)
                path = path.Substring(0, separatorIndex);
            var genericArguments = methodInfo.GetGenericArguments();
            path += "<" + string.Join(", ", genericArguments.Select(arg => arg.Name)) + ">";
            var parameters = methodInfo.GetParameters();
            path += "(" + string.Join(", ", parameters.Select(ParameterStr)) + ")";
            return new WhitelistKey(basis.AssemblyName, path, null, "M");
        }

        static string ParameterStr(ParameterInfo parameterInfo)
        {
            var prefix = parameterInfo.ParameterType.IsByRef ? "ref " : parameterInfo.ParameterType.IsPointer ? "*" : parameterInfo.IsOut ? "out " : "";
            var type = parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
            return prefix + ForType(type).Path;
        }

        readonly Regex _regex;
        readonly string _typeChar;

        WhitelistKey(string assemblyName, string path, Regex regex, string typeChar)
        {
            _regex = regex;
            _typeChar = typeChar;
            AssemblyName = assemblyName;
            Path = path;
        }

        public string AssemblyName { get; }

        public string Path { get; }

        public bool IsMatchFor(WhitelistKey typeKey)
        {
            if (typeKey == null)
                return false;
            return string.Equals(AssemblyName, typeKey.AssemblyName, StringComparison.OrdinalIgnoreCase) && _regex.IsMatch(typeKey.Path);
        }

        public string ToXmlDoc() => $"{_typeChar}:{Path.Replace('<', '{').Replace('>', '}')}";
    }
}