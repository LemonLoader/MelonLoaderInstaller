using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class CleanUp : IPatchStep
{
    public bool Run(Patcher patcher)
    {
        patcher._logger.Log("Cleaning up");

        try
        {
            DeleteFile(patcher._args.MelonDataPath);
            DeleteFile(patcher._args.UnityDependenciesPath);

            DeleteDir(patcher._info.LemonDataDirectory);
            DeleteDir(patcher._info.UnityNativeDirectory);

            string extraLibPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");
            DeleteFile(extraLibPath);
        }
        catch
        {
            patcher._logger.Log("Failed to clean up leftover data.");
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
