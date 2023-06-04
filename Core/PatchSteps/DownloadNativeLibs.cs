using System;
using System.Collections.Generic;
using System.Text;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class DownloadNativeLibs : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            return true;
        }
    }
}
