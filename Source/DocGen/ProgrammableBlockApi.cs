using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Malware.MDKUtilities;

namespace DocGen
{
    class ProgrammableBlockApi
    {
        List<MemberInfo> _members = new List<MemberInfo>();
        List<Assembly> _assemblies;
        List<IGrouping<Assembly, IGrouping<Type, MemberInfo>>> _groupings;

        public async Task Scan(string whitelistCacheFileName)
        {
            await Task.Run(() =>
            {
                var whitelist = Whitelist.Load(whitelistCacheFileName);
                var spaceEngineers = new SpaceEngineers();
                var installPath = Path.Combine(spaceEngineers.GetInstallPath(), "bin64");
                MDKUtilityFramework.Load(installPath);
                var dllFiles = Directory.EnumerateFiles(installPath, "*.dll", SearchOption.TopDirectoryOnly)
                    .ToList();

                foreach (var dllFile in dllFiles)
                    Visit(whitelist, dllFile);

                _groupings = _members.GroupBy(m => m.DeclaringType).GroupBy(m => m.Key.Assembly)
                    .ToList();
            });
        }

        void Visit(Whitelist whitelist, string dllFile)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                var assembly = Assembly.Load(assemblyName);
                Visit(whitelist, assembly);
            }
            catch (FileLoadException e)
            { }
            catch (BadImageFormatException e)
            { }
        }

        void Visit(Whitelist whitelist, Assembly assembly)
        {
            if (!whitelist.IsWhitelisted(assembly))
                return;
            if (assembly.GetName().Name == "mscorlib")
                return;
            var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (companyAttribute?.Company == "Microsoft Corporation")
                return;
            var types = assembly.GetExportedTypes();
            foreach (var type in types)
                Visit(whitelist, type);
        }

        void Visit(Whitelist whitelist, Type type)
        {
            if (!type.IsPublic)
                return;
            var members = type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var member in members)
                Visit(whitelist, member);
        }

        void Visit(Whitelist whitelist, MemberInfo member)
        {
            if (!whitelist.IsWhitelisted(member))
                return;
            _members.Add(member);
        }

        public async Task SaveAsync(string path)
        {
            var directory = new DirectoryInfo(Path.Combine(path, "api"));
            if (!directory.Exists)
                directory.Create();
            var fileName = Path.Combine(directory.FullName, "index.md");
            using (var file = File.CreateText(fileName))
            {
                await file.WriteLineAsync("#Index");
                await file.WriteLineAsync();

                foreach (var assemblyGroup in _groupings.OrderBy(g => g.Key.GetName().Name))
                {
                    var assemblyPath = new Uri(assemblyGroup.Key.CodeBase).LocalPath;
                    var assemblyFileName = Path.GetFileName(assemblyPath);
                    var xmlFileName = Path.ChangeExtension(assemblyPath, "xml");
                    XDocument documentation;
                    if (File.Exists(xmlFileName))
                        documentation = XDocument.Load(xmlFileName);
                    else
                        documentation = null;

                    await file.WriteLineAsync($"##{assemblyFileName}");
                    foreach (var typeGroup in assemblyGroup.OrderBy(g => g.Key.FullName))
                    {
                        var typeKey = WhitelistKey.ForType(typeGroup.Key);
                        var mdPath = ToMdFileName(typeKey.Path);
                        await file.WriteLineAsync($"**[`{typeKey.Path}`]({mdPath})**");
                        foreach (var member in typeGroup.OrderBy(m => m.Name))
                        {
                            var memberKey = WhitelistKey.ForMember(member, false);
                            await file.WriteLineAsync($"* [`{memberKey.Path}`]({mdPath})");
                        }
                        await file.WriteLineAsync();
                        await WriteTypeFileAsync(typeGroup, Path.Combine(directory.FullName, mdPath), documentation);
                    }
                }

                file.Flush();
            }
        }

        async Task WriteTypeFileAsync(IGrouping<Type, MemberInfo> typeGroup, string fileName, XDocument documentation)
        {
            using (var file = File.CreateText(fileName))
            {
                var typeKey = WhitelistKey.ForType(typeGroup.Key);
                await file.WriteLineAsync($"#{typeKey.Path}");
                await file.WriteLineAsync();
                foreach (var member in typeGroup.OrderBy(m => m.Name))
                {
                    var fullMemberKey = WhitelistKey.ForMember(member);
                    var xmlKey = fullMemberKey.ToXmlDoc();
                    var memberKey = WhitelistKey.ForMember(member, false);
                    var doc = documentation?.XPathSelectElement($"/doc/members/member[@name='{xmlKey}']");
                    string summary;
                    if (doc != null)
                        summary = doc.Element("summary")?.Value ?? "";
                    else
                        summary = "";
                    await file.WriteLineAsync($"* `{memberKey.Path}`");
                    await file.WriteLineAsync($"  " + Trim(summary));
                    await file.WriteLineAsync();
                }
            }
        }

        string Trim(string summary) => Regex.Replace(summary.Trim(), @"\s{2,}", " ");

        readonly HashSet<char> _invalidCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());

        string ToMdFileName(string path)
        {
            var builder = new StringBuilder(path);
            for (var i = 0; i < builder.Length; i++)
            {
                if (_invalidCharacters.Contains(builder[i]))
                    builder[i] = '_';
            }

            builder.Append(".md");
            return builder.ToString();
        }
    }
}