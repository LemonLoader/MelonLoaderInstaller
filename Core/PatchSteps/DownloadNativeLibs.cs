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
                var downloadTask = client.DownloadFileTaskAsync(REPO_BASE + packageName + ".zip", outPath);

                int waitedMilliseconds = 0;
                while (downloadTask.IsCompleted)
                {
                    downloadTask.Wait(2000);
                    waitedMilliseconds += 2000;

                    if (waitedMilliseconds >= 30000) // 30 seconds
                    {
                        throw new WebException("Timed out after 30 seconds.");
                    }
                }

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
