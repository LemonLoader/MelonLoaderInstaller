using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps
{
    internal class CleanUp : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Cleaning up");

            try
            {
                DeleteFile(patcher._args.LemonDataPath);
                DeleteFile(patcher._args.Il2CppEtcPath);
                DeleteFile(patcher._args.UnityDependenciesPath);

                DeleteDir(patcher._info.LemonDataDirectory);
                DeleteDir(patcher._info.UnityBaseDirectory);

                string extraLibPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");
                DeleteFile(extraLibPath);
            }
            catch
            {
                patcher._logger.Log("Failed to clean up leftover data.");
            }

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
