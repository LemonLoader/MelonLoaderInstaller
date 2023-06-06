using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class CleanUp : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            patcher._logger.Log("Cleaning up");
            return true;
        }
    }
}
