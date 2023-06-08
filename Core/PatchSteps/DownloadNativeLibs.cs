using System.IO;
using System.Net;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class DownloadNativeLibs : IPatchStep
    {
        private const string REPO_BASE = "https://github.com/LemonLoader/NativeLibraries/raw/main/";

        public bool Run(Patcher patcher)
        {
            string packageName = patcher._args.PackageName;
            patcher._logger.Log($"Checking for extra native libraries [ {packageName} ]");

            try
            {
                using WebClient client = new WebClient();
                string outPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");
                client.DownloadFile(REPO_BASE + packageName + ".zip", outPath);
                patcher._logger.Log("Downloaded");
            }
            catch (WebException)
            {
                patcher._logger.Log("No extra libraries found");
            }

            return true;
        }
    }
}
