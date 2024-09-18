using System.IO.Compression;
using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class ExtractUnityLibs : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        using FileStream zipStream = new(patcher._args.UnityDependenciesPath, FileMode.Open);
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(patcher._info.UnityNativeDirectory);

        // handle unstripped dependencies from udgb
        string libsSubDir = Path.Combine(patcher._info.UnityNativeDirectory, "Libs");
        if (Directory.Exists(libsSubDir))
            patcher._info.UnityNativeDirectory = libsSubDir;

        // We are going to be replacing libmain, we don't need any that are included by Unity
        foreach (string file in Directory.GetFiles(patcher._info.UnityNativeDirectory, "*.so", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "libmain.so")
                File.Delete(file);
        }

        return true;
    }
}
