using MelonLoaderInstaller.Core.Utilities.Signing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class AlignSign : IPatchStep
    {
        private IPatchLogger _logger;
        private string _pemData;

        public bool Run(Patcher patcher)
        {
            _logger = patcher._logger;
            _pemData = patcher._info.PemData;

            bool align = Align(patcher._info.OutputBaseApkPath);
            if (patcher._args.IsSplit)
                align = align && Align(patcher._info.OutputLibApkPath);

            bool sign = Sign(patcher._info.OutputBaseApkPath);
            if (patcher._args.IsSplit)
                sign = sign && Sign(patcher._info.OutputLibApkPath);

            // TODO: sign all other APKs for >2 splits

            return align && sign;
        }

        private bool Align(string apk)
        {
            _logger.Log($"Aligning [ {Path.GetFileName(apk)} ]");

            try
            {
                APKAligner.AlignApk(apk);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log($"Aligning failed\n{ex}");
                return false;
            }
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
