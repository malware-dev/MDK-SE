﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mal.DocGen2.Services.Markdown;
using System.Text.RegularExpressions;
using System.Windows.Navigation;

namespace Mal.DocGen2.Services.MarkdownGenerators
{
    class MemberGenerator : DocumentGenerator
    {
        //static bool IsMsType(MemberInfo memberInfo)
        //{
        //    var assembly = memberInfo.GetAssembly();
        //    if (assembly.GetName().Name == "mscorlib")
        //        return true;
        //    var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
        //    if (companyAttribute?.Company == "Microsoft Corporation")
        //        return true;
        //    return false;
        //}

        public static string LinkTo(string text, ApiEntry entry)
        {
            string escape(string s) => Regex.Replace(s, @"[<>[\]]", match =>
            {
                var v = match.ToString();
                switch (v)
                {
                    case "<":
                        return "&lt;";
                    case ">":
                        return "&gt;";
                    case "[":
                        return "&#91";
                    case "]":
                        return "&#93;";
                    default:
                        return $"\\{v}";
                }
            });

            text = escape(text);

            switch (ShouldBeIgnored(entry))
            {
                case IgnoreReason.NoLinkNeeded:
                    return text;
                case IgnoreReason.Prohibited:
                    return text + " <sub>prohibited</sub>";
            }

            if (MicrosoftLink.IsMsType(entry.Member))
            {
                var type = entry.Member as Type;
                var fullName = entry.FullName;
                if (type != null)
                {
                    if (type.IsGenericType && !type.IsGenericTypeDefinition)
                        type = type.GetGenericTypeDefinition();
                    fullName = type.FullName?.Replace('`', '-');
                }

                return MarkdownInline.HRef(text, escape($"https://docs.microsoft.com/en-us/dotnet/api/{fullName}?view=netframework-4.6"));
            }

            return MarkdownInline.HRef(text, escape(Path.GetFileNameWithoutExtension(entry.SuggestedFileName)));
        }

        public enum IgnoreReason
        {
            No,
            NoLinkNeeded,
            Prohibited
        }

        public static IgnoreReason ShouldBeIgnored(ApiEntry apiEntry)
        {
            //if (!apiEntry.IsWhitelisted)
            //    return true;
            if (apiEntry.Member.DeclaringType?.IsEnum ?? false)
                return IgnoreReason.NoLinkNeeded;
            if (apiEntry.Member.DeclaringType?.FullName == "Sandbox.Game.Localization.MySpaceTexts")
                return IgnoreReason.NoLinkNeeded;
            if (apiEntry.Member.DeclaringType?.FullName == "VRageMath.Color")
                return IgnoreReason.NoLinkNeeded;
            if (!apiEntry.IsWhitelisted)
                return IgnoreReason.Prohibited;
            return IgnoreReason.No;
        }

        public override async Task Generate(DirectoryInfo directory, ProgrammableBlockApi api)
        {
            var tasks = api.Entries.Where(e => !(e.Member is Type) && ShouldBeIgnored(e) == IgnoreReason.No).GroupBy(e => e.SuggestedFileName).Select(g => GeneratePage(api, directory, g));
            await Task.WhenAll(tasks);
        }

        async Task GeneratePage(ProgrammableBlockApi api, DirectoryInfo directory, IGrouping<string, ApiEntry> entries)
        {
            var fileName = Path.Combine(directory.FullName, entries.Key);
            using (var file = File.CreateText(fileName))
            {
                var writer = new MarkdownWriter(file);
                var firstEntry = entries.First();
                await writer.BeginParagraphAsync();
                await writer.WriteAsync($"← {MarkdownInline.HRef("Index", "Api-Index")} ← {MarkdownInline.HRef("Namespace Index", "Namespace-Index")} ← {LinkTo(firstEntry.DeclaringEntry.ToString(ApiEntryStringFlags.ShortDisplayName), firstEntry.DeclaringEntry)}");
                await writer.EndParagraphAsync();

                foreach (var overload in entries)
                {
                    await writer.WriteHeaderAsync(3, "Summary");
                    switch (overload.Member)
                    {
                        case ConstructorInfo constructorInfo:
                            await WriteConstructor(api, overload, writer, constructorInfo);
                            break;
                        case FieldInfo fieldInfo:
                            await WriteField(api, overload, writer, fieldInfo);
                            break;
                        case PropertyInfo propertyInfo:
                            await WriteProperty(api, overload, writer, propertyInfo);
                            break;
                        case EventInfo eventInfo:
                            await WriteEvent(api, overload, writer, eventInfo);
                            break;
                        case MethodInfo methodInfo:
                            await WriteMethod(api, overload, writer, methodInfo);
                            break;
                    }
                }

                await writer.FlushAsync();
            }
        }

        string GetTypeInfo(ProgrammableBlockApi api, Type type)
        {
            var entry = api.GetEntry(type);
            if (entry == null)
                return type.GetHumanReadableName();
            return LinkTo(entry.ToString(ApiEntryStringFlags.ShortDisplayName), entry);
        }

        async Task WriteMethod(ProgrammableBlockApi api, ApiEntry overload, MarkdownWriter writer, MethodInfo methodInfo)
        {
            await writer.BeginCodeBlockAsync();
            await writer.WriteLineAsync(overload.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.ParameterTypes | ApiEntryStringFlags.ParameterNames | ApiEntryStringFlags.ReturnValue | ApiEntryStringFlags.Accessors));
            await writer.EndCodeBlockAsync();
            if (overload.Documentation?.Summary != null)
                await WriteDocumentation(api, overload.Documentation?.Summary, writer);
            else
            {

            }
            if (methodInfo.ReturnType != typeof(void))
            {
                await writer.WriteHeaderAsync(3, "Returns");
                var returnEntry = api.GetEntry(methodInfo.ReturnType, true);
                await writer.BeginParagraphAsync();
                await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
                await writer.EndParagraphAsync();
                if (overload.Documentation?.Returns != null)
                    await WriteDocumentation(api, overload.Documentation?.Returns, writer);
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                await writer.WriteHeaderAsync(3, "Parameters");
                foreach (var parameter in parameters)
                {
                    var returnEntry = api.GetEntry(parameter.GetActualParameterType(), true);
                    await writer.WriteAsync("* ");
                    await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
                    await writer.WriteAsync(" ");
                    await writer.WriteAsync(parameter.Name);
                    await writer.WriteLineAsync();
                }
            }

            if (overload.Documentation?.Example != null)
            {
                await writer.WriteHeaderAsync(3, "Example");
                await WriteDocumentation(api, overload.Documentation?.Example, writer);
            }

            if (overload.Documentation?.Remarks != null)
            {
                await writer.WriteHeaderAsync(3, "Remarks");
                await WriteDocumentation(api, overload.Documentation?.Remarks, writer);
            }
        }

        async Task WriteProperty(ProgrammableBlockApi api, ApiEntry overload, MarkdownWriter writer, PropertyInfo propertyInfo)
        {
            await writer.BeginCodeBlockAsync();
            await writer.WriteLineAsync(overload.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.ParameterTypes | ApiEntryStringFlags.ParameterNames | ApiEntryStringFlags.ReturnValue | ApiEntryStringFlags.Accessors));
            await writer.EndCodeBlockAsync();
            if (overload.Documentation?.Summary != null)
                await WriteDocumentation(api, overload.Documentation?.Summary, writer);
            await writer.WriteHeaderAsync(3, "Returns");
            var returnEntry = api.GetEntry(propertyInfo.PropertyType, true);
            await writer.BeginParagraphAsync();
            await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
            await writer.EndParagraphAsync();
            if (overload.Documentation?.Returns != null)
                await WriteDocumentation(api, overload.Documentation?.Returns, writer);

            if (overload.Documentation?.Example != null)
            {
                await writer.WriteHeaderAsync(3, "Example");
                await WriteDocumentation(api, overload.Documentation?.Example, writer);
            }

            if (overload.Documentation?.Remarks != null)
            {
                await writer.WriteHeaderAsync(3, "Remarks");
                await WriteDocumentation(api, overload.Documentation?.Remarks, writer);
            }
        }

        async Task WriteEvent(ProgrammableBlockApi api, ApiEntry overload, MarkdownWriter writer, EventInfo eventInfo)
        {
            await writer.BeginCodeBlockAsync();
            await writer.WriteLineAsync(overload.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.ParameterTypes | ApiEntryStringFlags.ParameterNames | ApiEntryStringFlags.ReturnValue | ApiEntryStringFlags.Accessors));
            await writer.EndCodeBlockAsync();
            if (overload.Documentation?.Summary != null)
                await WriteDocumentation(api, overload.Documentation?.Summary, writer);
            await writer.WriteHeaderAsync(3, "Returns");
            var returnEntry = api.GetEntry(eventInfo.EventHandlerType, true);
            await writer.BeginParagraphAsync();
            await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
            await writer.EndParagraphAsync();
            if (overload.Documentation?.Returns != null)
                await WriteDocumentation(api, overload.Documentation?.Returns, writer);

            if (overload.Documentation?.Example != null)
            {
                await writer.WriteHeaderAsync(3, "Example");
                await WriteDocumentation(api, overload.Documentation?.Example, writer);
            }

            if (overload.Documentation?.Remarks != null)
            {
                await writer.WriteHeaderAsync(3, "Remarks");
                await WriteDocumentation(api, overload.Documentation?.Remarks, writer);
            }
        }

        async Task WriteConstructor(ProgrammableBlockApi api, ApiEntry overload, MarkdownWriter writer, ConstructorInfo constructorInfo)
        {
            await writer.BeginCodeBlockAsync();
            await writer.WriteLineAsync(overload.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.ParameterTypes | ApiEntryStringFlags.ParameterNames | ApiEntryStringFlags.ReturnValue | ApiEntryStringFlags.Accessors));
            await writer.EndCodeBlockAsync();
            if (overload.Documentation?.Summary != null)
                await WriteDocumentation(api, overload.Documentation?.Summary, writer);

            var parameters = constructorInfo.GetParameters();
            if (parameters.Length > 0)
            {
                await writer.WriteHeaderAsync(3, "Parameters");
                foreach (var parameter in parameters)
                {
                    var returnEntry = api.GetEntry(parameter.GetActualParameterType(), true);
                    await writer.WriteAsync("* ");
                    await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
                    await writer.WriteAsync(" ");
                    await writer.WriteAsync(parameter.Name);
                    await writer.WriteLineAsync();
                }
            }

            if (overload.Documentation?.Example != null)
            {
                await writer.WriteHeaderAsync(3, "Example");
                await WriteDocumentation(api, overload.Documentation?.Example, writer);
            }

            if (overload.Documentation?.Remarks != null)
            {
                await writer.WriteHeaderAsync(3, "Remarks");
                await WriteDocumentation(api, overload.Documentation?.Remarks, writer);
            }
        }

        async Task WriteField(ProgrammableBlockApi api, ApiEntry overload, MarkdownWriter writer, FieldInfo fieldInfo)
        {
            await writer.BeginCodeBlockAsync();
            await writer.WriteLineAsync(overload.ToString(ApiEntryStringFlags.Modifiers | ApiEntryStringFlags.GenericParameters | ApiEntryStringFlags.ParameterTypes | ApiEntryStringFlags.ParameterNames | ApiEntryStringFlags.ReturnValue | ApiEntryStringFlags.Accessors));
            await writer.EndCodeBlockAsync();
            if (overload.Documentation?.Summary != null)
                await WriteDocumentation(api, overload.Documentation?.Summary, writer);
            await writer.WriteHeaderAsync(3, "Returns");
            var returnEntry = api.GetEntry(fieldInfo.FieldType, true);
            await writer.BeginParagraphAsync();
            await writer.WriteAsync(LinkTo(returnEntry.ToString(ApiEntryStringFlags.ShortDisplayName), returnEntry));
            await writer.EndParagraphAsync();
            if (overload.Documentation?.Returns != null)
                await WriteDocumentation(api, overload.Documentation?.Returns, writer);

            if (overload.Documentation?.Example != null)
            {
                await writer.WriteHeaderAsync(3, "Example");
                await WriteDocumentation(api, overload.Documentation?.Example, writer);
            }

            if (overload.Documentation?.Remarks != null)
            {
                await writer.WriteHeaderAsync(3, "Remarks");
                await WriteDocumentation(api, overload.Documentation?.Remarks, writer);
            }
        }
    }
}