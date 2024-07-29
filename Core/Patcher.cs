using MelonLoader.Installer.Core.PatchSteps;
using System;
using System.IO;

namespace MelonLoader.Installer.Core;

/// <summary>
/// Main class that handles starting patches
/// </summary>
public class Patcher(PatchArguments arguments, IPatchLogger logger)
{
    internal PatchArguments _args = arguments;
    internal PatchInfo _info = new(arguments);
    internal IPatchLogger _logger = logger;

    public bool Run()
    {
        bool success = true;

        try
        {
            _info.CreateDirectories();

            _logger.Log($"Copying [ {_args.TargetApkPath} ] to [ {_info.OutputBaseApkPath} ]");
            File.Copy(_args.TargetApkPath, _info.OutputBaseApkPath);

            if (_args.IsSplit)
            {
                _logger.Log($"Copying [ {_args.LibraryApkPath} ] to [ {_info.OutputLibApkPath} ]");
                File.Copy(_args.LibraryApkPath, _info.OutputLibApkPath);
            }

            if (_args.ExtraSplitApkPaths != null)
            {
                for (int i = 0; i < _args.ExtraSplitApkPaths.Length; i++)
                {
                    string from = _args.ExtraSplitApkPaths[i];
                    string to = _info.OutputExtraApkPaths[i];

                    _logger.Log($"Copying [ {from} ] to [ {to} ]");
                    File.Copy(from, to);
                }
            }

            IPatchStep[] steps =
            [
                new DetectUnityVersion(),
                new DownloadUnityDeps(),
                new DownloadNativeLibs(),
                new ExtractDependencies(),
                new ExtractUnityLibs(),
                new PatchManifest(),
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
            _logger.Log("[ERROR] " + ex.ToString());
            success = false;
        }

        return success;
    }
}
