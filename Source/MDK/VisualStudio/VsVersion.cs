using System;
using System.Diagnostics;
using System.IO;

namespace MDK.VisualStudio
{
    /// <summary>
    ///     Retrieval of the actual Visual Studio version
    /// </summary>
    /// <remarks>
    ///     Original class written by Daniel Peñalba
    ///     (https://stackoverflow.com/questions/11082436/detect-the-visual-studio-version-inside-a-vspackage)
    /// </remarks>
    public static class VsVersion
    {
        static readonly object Lock = new object();

        static Version _vsVersion;

        /// <summary>
        ///     The full Visual Studio version
        /// </summary>
        public static Version Full
        {
            get
            {
                lock (Lock)
                {
                    if (_vsVersion == null)
                    {
                        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");

                        if (File.Exists(path))
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(path);

                            var verName = fvi.ProductVersion;

                            for (var i = 0; i < verName.Length; i++)
                            {
                                if (!char.IsDigit(verName, i) && verName[i] != '.')
                                {
                                    verName = verName.Substring(0, i);
                                    break;
                                }
                            }

                            _vsVersion = new Version(verName);
                        }
                        else
                            _vsVersion = new Version(0, 0); // Not running inside Visual Studio!
                    }
                }

                return _vsVersion;
            }
        }
    }
}
