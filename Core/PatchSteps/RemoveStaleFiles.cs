using System;
using System.Collections.Generic;
using System.Text;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class RemoveStaleFiles : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            return true;
        }
    }
}
