using System.IO.Compression;
using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class ExtractUnityLibs : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        // we probably got it from the MelonLoader.UnityDependencies repo
        if (!File.Exists(patcher.Args.UnityDependenciesPath))
            return true;

        using FileStream zipStream = new(patcher.Args.UnityDependenciesPath, FileMode.Open);
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(patcher.Info.UnityNativeDirectory);

        // handle unstripped dependencies from udgb
        string libsSubDir = Path.Combine(patcher.Info.UnityNativeDirectory, "Libs");
        if (Directory.Exists(libsSubDir))
            patcher.Info.UnityNativeDirectory = libsSubDir;

        // We are going to be replacing libmain, we don't need any that are included by Unity
        foreach (string file in Directory.GetFiles(patcher.Info.UnityNativeDirectory, "*.so", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "libmain.so")
                File.Delete(file);
        }

        return true;
    }
}
