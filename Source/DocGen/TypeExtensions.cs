using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DocGen
{
    [Flags]
    enum TypeModifiers
    {
        None      = 0,
        Private   = 0b00000001,
        Protected = 0b00000010,
        Internal  = 0b00000100,
        Public    = 0b00001000,
        Sealed    = 0b00010000,
        Abstract  = 0b00100000,
        Virtual   = 0b01000000,
    }

    static class TypeExtensions
    {
        public static bool IsPublic(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return type.IsPublic || type.IsNestedPublic;
                case MethodBase methodBase:
                    return methodBase.IsPublic;
                case FieldInfo fieldInfo:
                    return fieldInfo.IsPublic;
                case EventInfo eventInfo:
                    return (eventInfo.AddMethod?.IsPublic ?? false) || (eventInfo.RemoveMethod?.IsPublic ?? false);
                case PropertyInfo propertyInfo:
                    return (propertyInfo.GetMethod?.IsPublic ?? false) || (propertyInfo.SetMethod?.IsPublic ?? false);
            }

            return false;
        }

        public static string ToCodeString(this TypeModifiers modifiers)
        {
            var segments = new List<string>();
            if ((modifiers & TypeModifiers.Private) != 0)
                segments.Add("private");
            if ((modifiers & TypeModifiers.Protected) != 0)
                segments.Add("protected");
            if ((modifiers & TypeModifiers.Internal) != 0)
                segments.Add("internal");
            if ((modifiers & TypeModifiers.Public) != 0)
                segments.Add("public");
            if ((modifiers & TypeModifiers.Abstract) != 0)
                segments.Add("abstract");
            if ((modifiers & TypeModifiers.Virtual) != 0)
                segments.Add("virtual");
            if ((modifiers & TypeModifiers.Sealed) != 0)
                segments.Add("sealed");
            return string.Join(" ", segments);
        }

        public static TypeModifiers GetModifiers(this MemberInfo memberInfo)
        {
            var modifiers = TypeModifiers.None;
            switch (memberInfo)
            {
                case Type type:
                    if (type.IsPublic || type.IsNestedPublic)
                        modifiers |= TypeModifiers.Public;
                    if (type.IsNestedFamily)
                        modifiers |= TypeModifiers.Protected;
                    if (type.IsNestedAssembly || type.IsNotPublic)
                        modifiers |= TypeModifiers.Internal;
                    if (type.IsNestedFamORAssem)
                    {
                        modifiers |= TypeModifiers.Protected;
                        modifiers |= TypeModifiers.Internal;
                    }

                    if (modifiers == TypeModifiers.None)
                        modifiers = TypeModifiers.Private;
                    if (type.IsAbstract && !type.IsInterface && !type.IsValueType)
                        modifiers |= TypeModifiers.Abstract;
                    if (type.IsSealed && !type.IsValueType)
                        modifiers |= TypeModifiers.Sealed;
                    break;
                case MethodBase methodBase:
                    if (methodBase.IsPublic)
                        modifiers |= TypeModifiers.Public;
                    if (methodBase.IsFamily)
                        modifiers |= TypeModifiers.Protected;
                    if (methodBase.IsAssembly)
                        modifiers |= TypeModifiers.Internal;
                    if (methodBase.IsFamilyOrAssembly)
                    {
                        modifiers |= TypeModifiers.Protected;
                        modifiers |= TypeModifiers.Internal;
                    }
                    if (modifiers == TypeModifiers.None)
                        modifiers = TypeModifiers.Private;
                    if (!methodBase.DeclaringType?.IsInterface ?? false)
                    {
                        if (methodBase.IsAbstract && !methodBase.IsVirtual)
                            modifiers |= TypeModifiers.Abstract;
                        if (methodBase.IsVirtual && !methodBase.IsFinal)
                            modifiers |= TypeModifiers.Virtual;
                    }

                    break;
                case FieldInfo fieldInfo:
                    if (fieldInfo.IsPublic)
                        modifiers |= TypeModifiers.Public;
                    if (fieldInfo.IsFamily)
                        modifiers |= TypeModifiers.Protected;
                    if (fieldInfo.IsAssembly)
                        modifiers |= TypeModifiers.Internal;
                    if (fieldInfo.IsFamilyOrAssembly)
                    {
                        modifiers |= TypeModifiers.Protected;
                        modifiers |= TypeModifiers.Internal;
                    }
                    if (modifiers == TypeModifiers.None)
                        modifiers = TypeModifiers.Private;
                    break;
                case PropertyInfo propertyInfo:
                {
                    var getterModifier = propertyInfo.GetMethod == null ? TypeModifiers.None : propertyInfo.GetMethod.GetModifiers();
                    var setterModifier = propertyInfo.SetMethod == null ? TypeModifiers.None : propertyInfo.SetMethod.GetModifiers();

                    var baseModifiers = getterModifier & setterModifier;
                    getterModifier = getterModifier & ~baseModifiers;
                    setterModifier = setterModifier & ~baseModifiers;

                    modifiers = baseModifiers | (TypeModifiers)Math.Max((int)getterModifier, (int)setterModifier);
                    break;
                }
                case EventInfo eventInfo:
                {
                    var getterModifier = eventInfo.AddMethod == null ? TypeModifiers.Private : eventInfo.AddMethod.GetModifiers();
                    var setterModifier = eventInfo.RemoveMethod == null ? TypeModifiers.Private : eventInfo.RemoveMethod.GetModifiers();

                    var baseModifiers = getterModifier & setterModifier;
                    getterModifier = getterModifier & ~baseModifiers;
                    setterModifier = setterModifier & ~baseModifiers;

                    modifiers = baseModifiers | (TypeModifiers)Math.Max((int)getterModifier, (int)setterModifier);
                    break;
                }
            }


            return modifiers;
        }

        public static Type GetActualParameterType(this ParameterInfo parameterInfo)
        {
            return parameterInfo.ParameterType.IsByRef || parameterInfo.ParameterType.IsPointer ? parameterInfo.ParameterType.GetElementType() : parameterInfo.ParameterType;
        }

        public static string GetHumanReadableName(this Type type)
        {
            if (!type.IsGenericType && !type.IsGenericTypeDefinition)
                return type.FullName ?? type.Name;
            var baseName = type.FullName ?? type.Name;
            var genericIndex = baseName.IndexOf('`');
            baseName = baseName.Substring(0, genericIndex);

            var genericParams = type.GetGenericArguments()
                .Select(t => t.GetHumanReadableName());

            return $"{baseName}<{string.Join(", ", genericParams)}>";
        }

        public static Assembly GetAssembly(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return type.Assembly;
                case ConstructorInfo constructorInfo:
                    return constructorInfo.DeclaringType?.Assembly;
                case FieldInfo fieldInfo:
                    return fieldInfo.DeclaringType?.Assembly;
                case PropertyInfo propertyInfo:
                    return propertyInfo.DeclaringType?.Assembly;
                case EventInfo eventInfo:
                    return eventInfo.DeclaringType?.Assembly;
                case MethodInfo methodInfo:
                    return methodInfo.DeclaringType?.Assembly;
            }

            return null;
        }

        public static string GetNamespace(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return type.Namespace;
                case ConstructorInfo constructorInfo:
                    return constructorInfo.DeclaringType?.Namespace;
                case FieldInfo fieldInfo:
                    return fieldInfo.DeclaringType?.Namespace;
                case PropertyInfo propertyInfo:
                    return propertyInfo.DeclaringType?.Namespace;
                case EventInfo eventInfo:
                    return eventInfo.DeclaringType?.Namespace;
                case MethodInfo methodInfo:
                    return methodInfo.DeclaringType?.Namespace;
            }

            return null;
        }

        public static string GetFullName(this MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case Type type:
                    return type.FullName;
                case ConstructorInfo constructorInfo:
                    return $"{constructorInfo.DeclaringType?.FullName}.{memberInfo.Name}";
                case FieldInfo fieldInfo:
                    return $"{memberInfo.DeclaringType?.FullName}.{fieldInfo.Name}";
                case PropertyInfo propertyInfo:
                    return $"{memberInfo.DeclaringType?.FullName}.{propertyInfo.Name}";
                case EventInfo eventInfo:
                    return $"{memberInfo.DeclaringType?.FullName}.{eventInfo.Name}";
                case MethodInfo methodInfo:
                    return $"{memberInfo.DeclaringType?.FullName}.{methodInfo.Name}";
            }

            return null;
        }

        public static string GetLanguageAlias(this Type type)
        {
            if (!type.IsEnum)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        return "bool";
                    case TypeCode.Char:
                        return "char";
                    case TypeCode.SByte:
                        return "sbyte";
                    case TypeCode.Byte:
                        return "byte";
                    case TypeCode.Int16:
                        return "short";
                    case TypeCode.UInt16:
                        return "ushort";
                    case TypeCode.Int32:
                        return "int";
                    case TypeCode.UInt32:
                        return "uint";
                    case TypeCode.Int64:
                        return "long";
                    case TypeCode.UInt64:
                        return "ulong";
                    case TypeCode.Single:
                        return "float";
                    case TypeCode.Double:
                        return "double";
                    case TypeCode.Decimal:
                        return "decimal";
                    case TypeCode.String:
                        return "string";
                    case TypeCode.Object:
                        return type == typeof(object) ? "object" : null;
                }
            }

            return null;
        }
    }
}