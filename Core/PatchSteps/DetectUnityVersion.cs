using System;
using System.Collections.Generic;
using System.Text;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class DetectUnityVersion : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            if (patcher._args.UnityVersion != null)
                return true;

            return true;
        }
    }
}
