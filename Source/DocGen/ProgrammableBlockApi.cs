using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using DocGen.MarkdownGenerators;
using DocGen.XmlDocs;
using Malware.MDKUtilities;

namespace DocGen
{
    class ProgrammableBlockApi
    {
        static Assembly LoadAssembly(string dllFile)
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                return Assembly.Load(assemblyName);
            }
            catch (FileLoadException)
            {
                return null;
            }
            catch (BadImageFormatException)
            {
                return null;
            }
        }

        public static async Task Update(string whitelistCacheFileName, string output)
        {
            var api = await LoadAsync(whitelistCacheFileName);
            await api.SaveAsync(output);
        }

        public static async Task<ProgrammableBlockApi> LoadAsync(string whitelistCacheFileName)
        {
            var api = new ProgrammableBlockApi();
            await Task.Run(() =>
            {
                var types = new List<Type>();
                var members = new List<MemberInfo>();
                var spaceEngineers = new SpaceEngineers();
                var installPath = Path.Combine(spaceEngineers.GetInstallPath(), "bin64");
                MDKUtilityFramework.Load(installPath);
                var dllFiles = Directory.EnumerateFiles(installPath, "*.dll", SearchOption.TopDirectoryOnly)
                    .ToList();
                foreach (var file in dllFiles)
                    LoadAssembly(file);
                //var assemblies = dllFiles.Select(LoadAssembly).Where(a => a != null).ToList();
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
                var whitelist = Whitelist.Load(whitelistCacheFileName);
                api._whitelist = whitelist;

                foreach (var assembly in assemblies)
                    Visit(whitelist, assembly, members);

                // Hack. I'm getting duplicated entries and atm I cannot be bothered to do a proper check
                // for why...
                var visitedMembers = new HashSet<MemberInfo>();

                foreach (var assemblyGroup in members.GroupBy(m => m.GetAssembly()))
                {
                    foreach (var typeGroup in assemblyGroup.GroupBy(m => m.DeclaringType))
                    {
                        if (typeGroup.Key == null)
                        {
                            foreach (var type in typeGroup)
                            {
                                var entry = api.GetEntry(type);
                                if (!api._entries.Contains(entry))
                                    api._entries.Add(entry);
                            }

                            continue;
                        }

                        var typeEntry = api.GetEntry(typeGroup.Key);
                        if (typeEntry != null)
                        {
                            if (!visitedMembers.Add(typeEntry.Member))
                                continue;
                            if (!api._entries.Contains(typeEntry))
                                api._entries.Add(typeEntry);
                            foreach (var member in typeGroup)
                            {
                                var entry = api.GetEntry(member);
                                if (entry != null)
                                {
                                    if (!visitedMembers.Add(member))
                                        continue;
                                    api._entries.Add(entry);
                                }
                            }
                        }
                    }
                }

                foreach (var entry in api.Entries)
                    entry.ResolveLinks();
            });

            return api;
        }

        static void Visit(Whitelist whitelist, Assembly assembly, List<MemberInfo> members)
        {
            if (!whitelist.IsWhitelisted(assembly))
                return;
            if (assembly.GetName().Name == "mscorlib")
                return;
            var companyAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
            if (companyAttribute?.Company == "Microsoft Corporation")
                return;
            var exportedTypes = assembly.GetExportedTypes();
            foreach (var type in exportedTypes)
                Visit(whitelist, type, members);
        }

        static void Visit(Whitelist whitelist, Type type, List<MemberInfo> members)
        {
            if (!type.IsPublic() || !whitelist.IsWhitelisted(type))
                return;
            members.Add(type);
            var typeMembers = type.GetMembers(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            foreach (var member in typeMembers)
                Visit(whitelist, member, members);
            //var nestedTypes = type.GetNestedTypes(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            //foreach (var nestedType in nestedTypes)
            //    Visit(whitelist, nestedType, members);
        }

        static void Visit(Whitelist whitelist, MemberInfo member, List<MemberInfo> members)
        {
            if (member is Type type)
            {
                Visit(whitelist, type, members);
                return;
            }
            if (!member.IsPublic() || !whitelist.IsWhitelisted(member))
                return;
            members.Add(member);
        }

        Whitelist _whitelist;
        List<ApiEntry> _entries = new List<ApiEntry>();
        Dictionary<MemberInfo, ApiEntry> _entryLookup = new Dictionary<MemberInfo, ApiEntry>();
        Dictionary<MemberInfo, ApiEntry> _blacklistedEntryLookup = new Dictionary<MemberInfo, ApiEntry>();
        Dictionary<string, XDocument> _documentationCache = new Dictionary<string, XDocument>(StringComparer.CurrentCultureIgnoreCase);

        ProgrammableBlockApi()
        {
            Entries = new ReadOnlyCollection<ApiEntry>(_entries);
        }

        public ReadOnlyCollection<ApiEntry> Entries { get; }

        ApiEntry CreateEntry(MemberInfo memberInfo)
        {
            var entry = ApiEntry.Create(this, _whitelist, memberInfo);
            if (entry == null)
                return null;

            string docFileName = null;
            if (memberInfo is Type type)
            {
                if (type.IsGenericType)
                {
                    type = type.GetGenericTypeDefinition();
                }
                docFileName = Path.ChangeExtension(new Uri(type.Assembly.CodeBase).LocalPath, "xml");
            }
            else
            {
                type = memberInfo.DeclaringType;
                if (type?.IsGenericType ?? false)
                {
                    type = type.GetGenericTypeDefinition();
                }
                var codeBase = type?.Assembly.CodeBase;
                if (codeBase != null)
                    docFileName = Path.ChangeExtension(new Uri(codeBase).LocalPath, "xml");
            }

            if (docFileName != null)
            {
                if (!_documentationCache.TryGetValue(docFileName, out var documentation))
                    _documentationCache[docFileName] = documentation = File.Exists(docFileName) ? XDocument.Load(docFileName) : null;
                var doc = documentation?.XPathSelectElement($"/doc/members/member[@name='{entry.XmlDocKey}']");
                entry.Documentation = XmlDoc.Generate(doc);
            }

            return entry;
        }

        public ApiEntry GetEntry(MemberInfo memberInfo, bool includeBlacklisted = false)
        {
            if (_entryLookup.TryGetValue(memberInfo, out var entry))
            {
                if (entry != null || includeBlacklisted && _blacklistedEntryLookup.TryGetValue(memberInfo, out entry))
                    return entry;
                return null;
            }

            entry = CreateEntry(memberInfo);
            if (entry == null)
            {
                _entryLookup[memberInfo] = null;
                return null;
            }

            if (_whitelist.IsWhitelisted(memberInfo))
            {
                _entryLookup[memberInfo] = entry;
                return entry;
            }

            _blacklistedEntryLookup[memberInfo] = null;
            return includeBlacklisted ? entry : null;
        }

        public async Task SaveAsync(string path)
        {
            var generators = new DocumentGenerator[]
            {
                new ApiIndexGenerator(),
                new TypeGenerator(),
                new MemberGenerator(),
                new NamespaceIndexGenerator(),
                new NamespaceGenerator()
            };

            var directory = new DirectoryInfo(path);
            if (!directory.Exists)
                directory.Create();
            await Task.WhenAll(generators.Select(g => g.Generate(directory, this)));
        }

        public bool IsAdditionalEntry(ApiEntry entry)
        {
            return _entries.Contains(entry);
        }
    }
}