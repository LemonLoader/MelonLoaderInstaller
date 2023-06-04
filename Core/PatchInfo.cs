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

        public string KeystorePath { get; }

        public PatchInfo(PatchArguments arguments)
        {
            LemonDataDirectory = Path.Combine(arguments.TempDirectory, "lemon_data");

            UnityBaseDirectory = Path.Combine(arguments.TempDirectory, "unity");
            UnityNativeDirectory = Path.Combine(UnityBaseDirectory, "Libs");
            UnityManagedDirectory = Path.Combine(UnityBaseDirectory, "Managed");

            KeystorePath = Path.Combine(arguments.TempDirectory, "key.keystore");
        }

        public void CreateDirectories()
        {
            Directory.CreateDirectory(arguments.TempDirectory);
            Directory.CreateDirectory(LemonDataDirectory);
        }
    }
}
