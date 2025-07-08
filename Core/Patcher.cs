using MelonLoader.Installer.Core.PatchSteps;
using System;
using System.IO;

namespace MelonLoader.Installer.Core;

/// <summary>
/// Main class that handles starting patches
/// </summary>
public class Patcher(PatchArguments arguments, IPatchLogger logger)
{
    public PatchArguments Args = arguments;
    public IPatchLogger Logger = logger;
    public PatchInfo Info = new(arguments);

    public bool Run()
    {
        bool success = true;

        try
        {
            Info.CreateDirectories();

            if (!Path.Exists(Info.OutputBaseApkPath))
            {
                Logger.Log($"Copying [ {Args.TargetApkPath} ] to [ {Info.OutputBaseApkPath} ]");
                File.Copy(Args.TargetApkPath, Info.OutputBaseApkPath);
            }

            if (Args.IsSplit && !Path.Exists(Info.OutputLibApkPath))
            {
                Logger.Log($"Copying [ {Args.LibraryApkPath} ] to [ {Info.OutputLibApkPath} ]");
                File.Copy(Args.LibraryApkPath, Info.OutputLibApkPath!);
            }

            if (Args.ExtraSplitApkPaths != null)
            {
                for (int i = 0; i < Args.ExtraSplitApkPaths.Length; i++)
                {
                    string from = Args.ExtraSplitApkPaths[i];
                    string to = Info.OutputExtraApkPaths![i];

                    if (!File.Exists(to))
                    {
                        Logger.Log($"Copying [ {from} ] to [ {to} ]");
                        File.Copy(from, to);
                    }
                }
            }

            IPatchStep[] steps =
            [
                new DetectUnityVersion(),
                new DownloadUnityDeps(),
                new ExtractDependencies(),
                new ExtractUnityLibs(),
                new PatchManifest(),
                new InstallPlugins(),
                new RepackAPK(),
                new GenerateCertificate(),
                new AlignSign(),
                new CleanUp(),
            ];

            foreach (IPatchStep step in steps)
            {
                bool status = step.Run(this);
                if (!status)
                    throw new Exception($"Failed to complete patching.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log("[ERROR] " + ex.ToString());
            success = false;
        }

        return success;
    }
}
