namespace MelonLoaderInstaller.Core
{
    /// <summary>
    /// Public-facing class for user set information
    /// </summary>
    public class PatchArguments
    {
        public string TargetApkPath;
        public string LibraryApkPath;

        public string OutputApkPath;

        public string TempDirectory;

        public string LemonDataPath;
        public string Il2CppEtcPath;
        public string UnityDependenciesPath;

        public PatchArguments(string targetApkPath, string libraryApkPath, string outputApkPath, string tempDirectory, string lemonDataPath, string il2CppEtcPath, string unityDependenciesPath)
        {
            TargetApkPath = targetApkPath;
            LibraryApkPath = libraryApkPath;
            OutputApkPath = outputApkPath;
            TempDirectory = tempDirectory;
            LemonDataPath = lemonDataPath;
            Il2CppEtcPath = il2CppEtcPath;
            UnityDependenciesPath = unityDependenciesPath;
        }
    }
}