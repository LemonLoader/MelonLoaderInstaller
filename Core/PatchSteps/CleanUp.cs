using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class CleanUp : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Cleaning up");

            File.Delete(patcher._args.LemonDataPath);
            File.Delete(patcher._args.Il2CppEtcPath);
            File.Delete(patcher._args.UnityDependenciesPath);

            Directory.Delete(patcher._info.LemonDataDirectory);
            Directory.Delete(patcher._info.UnityBaseDirectory);

            return true;
        }
    }
}
