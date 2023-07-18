using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class DownloadNativeLibs : IPatchStep
    {
        private const string REPO_BASE = "https://github.com/LemonLoader/NativeLibraries/raw/main/";

        public bool Run(Patcher patcher)
        {
            return RunAsync(patcher).GetAwaiter().GetResult();
        }

        private async Task<bool> RunAsync(Patcher patcher)
        {
            string packageName = patcher._args.PackageName;
            patcher._logger.Log($"Checking for extra native libraries [ {packageName} ]");

            try
            {
                string outPath = Path.Combine(patcher._args.TempDirectory, "extraLibraries.zip");

                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    using HttpResponseMessage response = await httpClient.GetAsync(REPO_BASE + packageName + ".zip");
                    response.EnsureSuccessStatusCode();

                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = File.Create(outPath);
                    await contentStream.CopyToAsync(fileStream);
                }

                patcher._logger.Log("Downloaded");
            }
            catch (HttpRequestException)
            {
                patcher._logger.Log("No extra libraries found");
            }
            catch (TaskCanceledException)
            {
                patcher._logger.Log("Timed out after 30 seconds, skipping extra libraries.");
            }

            return true;
        }
    }
}
