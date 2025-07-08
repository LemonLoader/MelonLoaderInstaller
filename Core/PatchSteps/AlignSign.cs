using MelonLoader.Installer.Core.Utilities.Signing;
using System;
using System.IO;

namespace MelonLoader.Installer.Core.PatchSteps;

internal class AlignSign : IPatchStep
{
    private IPatchLogger? _logger;
    private string _pemData = "";

    public bool Run(Patcher patcher)
    {
        // Aligning occurs after V1 signing

        _logger = patcher.Logger;
        _pemData = patcher.Info.PemData;

        bool sign = Sign(patcher.Info.OutputBaseApkPath);
        if (patcher.Args.IsSplit)
            sign = sign && Sign(patcher.Info.OutputLibApkPath);

        if (patcher.Info.OutputExtraApkPaths != null)
        {
            foreach (string apk in patcher.Info.OutputExtraApkPaths)
                sign = sign && Sign(apk);
        }

        return sign;
    }

    private bool Sign(string apk)
    {
        _logger!.Log($"Signing [ {Path.GetFileName(apk)} ]");

        try
        {
            APKSigner signer = new(_pemData, _logger);
            signer.Sign(apk);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Log($"Signing failed\n{ex}");
            return false;
        }
    }
}
