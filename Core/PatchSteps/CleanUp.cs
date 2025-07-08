using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class CleanUp : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        patcher.Logger.Log("Cleaning up");

        try
        {
            DeleteFile(patcher.Args.MelonDataPath);
            DeleteFile(patcher.Args.UnityDependenciesPath);

            DeleteDir(patcher.Info.LemonDataDirectory);
            DeleteDir(patcher.Info.UnityNativeDirectory);

            string extraLibPath = Path.Combine(patcher.Args.TempDirectory, "extraLibraries.zip");
            DeleteFile(extraLibPath);
        }
        catch
        {
            patcher.Logger.Log("Failed to clean up leftover data.");
        }

        return true;
    }

    private static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void DeleteDir(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }
}
