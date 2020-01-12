using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.XPath;
using Malware.MDKUtilities;

namespace Mal.DocGen2.Services
{
    public class SpaceEngineersText
    {
        //MethodInfo _getOrComputeMethod;
        //Type _myStringIdType;
        //Assembly _sandboxGameAssembly;
        //Assembly _vrageLibraryAssembly;
        //MethodInfo _myStringIdToString;
        Dictionary<string, string> _texts = new Dictionary<string, string>();

        public SpaceEngineersText()
        {
            var spaceEngineers = new SpaceEngineers();
            var document = XDocument.Load(spaceEngineers.GetInstallPath(@"Content\Data\Localization\MyTexts.resx"));
            var stringElements = document.XPathSelectElements(@"root/data");
            foreach (var element in stringElements)
            {
                _texts[(string)element.Attribute("name")] = (string) element.Element("value");
            }
        }

        //    void LoadAssemblies(SpaceEngineers spaceEngineers)
        //    {
        //        var gameBinaryPath = spaceEngineers.GetInstallPath("Bin64");
        //        var asmName = AssemblyName.GetAssemblyName(Path.Combine(gameBinaryPath, "VRage.Library.dll"));
        //        _vrageLibraryAssembly = Assembly.Load(asmName);

        //        asmName = AssemblyName.GetAssemblyName(Path.Combine(gameBinaryPath, "Sandbox.Game.dll"));
        //        _sandboxGameAssembly = Assembly.Load(asmName);
        //    }

        //    void InitFileSystem(SpaceEngineers spaceEngineers)
        //    {
        //        var contentPath = spaceEngineers.GetInstallPath("Content");
        //        var dataPath = spaceEngineers.GetDataPath();
        //        var type = _vrageLibraryAssembly.GetType("VRage.FileSystem.MyFileSystem");
        //        //public static void Init(string contentPath, string userData, string modDirName = "Mods", string shadersBasePath = null)
        //        var method = type.GetMethod("Init", new[] {typeof(string), typeof(string), typeof(string), typeof(string)});
        //        var args = new object[] {contentPath, dataPath, "Mods", null};
        //        method.Invoke(null, args);
        //    }

        //    void InitLanguage()
        //    {
        //        var type = _sandboxGameAssembly.GetType("Sandbox.Game.Localization.MyLanguage");
        //        var method = type.GetMethod("Init");
        //        method.Invoke(null, null);
        //        _myStringIdType = _vrageLibraryAssembly.GetType("VRage.Utils.MyStringId");
        //        _getOrComputeMethod = _myStringIdType.GetMethod("GetOrCompute", new[] {typeof(string)});
        //        _myStringIdToString = _myStringIdType.GetMethod("ToString");
        //    }

        public string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return key;
            if (_texts.TryGetValue(key, out var value))
                return value;
            return key;
        }
    }
}