using MelonLoaderInstaller.Core.Utilities.Signing;
using System;
using System.IO;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class AlignSign : IPatchStep
    {
        private IPatchLogger _logger;
        private string _pemData;

        public bool Run(Patcher patcher)
        {
            // Aligning occurs after V1 signing

            _logger = patcher._logger;
            _pemData = patcher._info.PemData;

            bool sign = Sign(patcher._info.OutputBaseApkPath);
            if (patcher._args.IsSplit)
                sign = sign && Sign(patcher._info.OutputLibApkPath);

            if (patcher._info.OutputExtraApkPaths != null)
            {
                foreach (string apk in patcher._info.OutputExtraApkPaths)
                    sign = sign && Sign(apk);
            }

            return sign;
        }

        private bool Sign(string apk)
        {
            _logger.Log($"Signing [ {Path.GetFileName(apk)} ]");

            try
            {
                APKSigner signer = new APKSigner(_pemData, _logger);
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
}
