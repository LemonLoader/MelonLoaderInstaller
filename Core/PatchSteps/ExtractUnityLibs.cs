using System.IO.Compression;
using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class ExtractUnityLibs : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            using FileStream zipStream = new FileStream(patcher._args.UnityDependenciesPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(patcher._info.UnityBaseDirectory);

            return true;
        }
    }
}
