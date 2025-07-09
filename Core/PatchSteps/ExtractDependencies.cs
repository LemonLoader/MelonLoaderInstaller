using System.IO.Compression;
using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class ExtractDependencies : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        patcher.Logger.Log("Extracting Dependencies");

        {
            using FileStream zipStream = new(patcher.Args.MelonDataPath, FileMode.Open);
            using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            archive.ExtractToDirectory(patcher.Info.LemonDataDirectory);
        }

        return true;
    }
}
