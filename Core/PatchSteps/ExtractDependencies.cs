using System.IO.Compression;
using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class ExtractDependencies : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Extracting Dependencies");

            {
                using FileStream zipStream = new FileStream(patcher._args.LemonDataPath, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(patcher._info.LemonDataDirectory);

                // We are going to be replacing libmain, we don't need any that are included by Unity
                foreach (string file in Directory.GetFiles(patcher._info.UnityBaseDirectory, "*.so", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(file) == "libmain.so")
                        File.Delete(file);
                }
            }

            patcher._logger.Log("Extracting il2cpp/etc");

            {
                using FileStream zipStream = new FileStream(patcher._args.Il2CppEtcPath, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(patcher._info.LemonDataDirectory);
            }

            patcher._logger.Log("Extracting Libraries");

            {
                string outPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");
                if (!File.Exists(outPath))
                    return true;

                using FileStream zipStream = new FileStream(outPath, FileMode.Open);
                using ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(patcher._info.LemonDataDirectory);
            }

            return true;
        }
    }
}
