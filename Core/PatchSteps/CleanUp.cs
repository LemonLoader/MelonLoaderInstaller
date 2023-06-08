using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class CleanUp : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Cleaning up");

            DeleteFile(patcher._args.LemonDataPath);
            DeleteFile(patcher._args.Il2CppEtcPath);
            DeleteFile(patcher._args.UnityDependenciesPath);

            DeleteDir(patcher._info.LemonDataDirectory);
            DeleteDir(patcher._info.UnityBaseDirectory);

            string extraLibPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");
            DeleteFile(extraLibPath);

            return true;
        }

        private void DeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private void DeleteDir(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }
}
