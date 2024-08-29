using System.IO.Compression;
using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class ExtractDependencies : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        patcher._logger.Log("Extracting Dependencies");

        {
            using FileStream zipStream = new(patcher._args.MelonDataPath, FileMode.Open);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(patcher._info.LemonDataDirectory);
        }

        return true;
    }
}
