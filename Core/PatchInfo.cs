using System.IO;

namespace MelonLoaderInstaller.Core
{
    /// <summary>
    /// Class for storing information only used internally
    /// </summary>
    internal class PatchInfo
    {
        public string LemonDataDirectory { get; }

        public string UnityBaseDirectory { get; }
        public string UnityNativeDirectory { get; }
        public string UnityManagedDirectory { get; }

        public string OutputBaseApkPath { get; }
        public string OutputLibApkPath { get; }

        public string PemData { get; set; }

        private PatchArguments _arguments;

        public PatchInfo(PatchArguments arguments)
        {
            _arguments = arguments;

            LemonDataDirectory = Path.Combine(arguments.TempDirectory, "lemon_data");

            UnityBaseDirectory = Path.Combine(arguments.TempDirectory, "unity");
            UnityNativeDirectory = Path.Combine(UnityBaseDirectory, "Libs");
            UnityManagedDirectory = Path.Combine(UnityBaseDirectory, "Managed");

            OutputBaseApkPath = Path.Combine(arguments.OutputApkDirectory, "base.apk");
            if (_arguments.IsSplit)
                OutputLibApkPath = Path.Combine(arguments.OutputApkDirectory, Path.GetFileName(arguments.LibraryApkPath));
        }

        public void CreateDirectories()
        {
            Directory.CreateDirectory(_arguments.TempDirectory);
            Directory.CreateDirectory(LemonDataDirectory);
        }
    }
}
