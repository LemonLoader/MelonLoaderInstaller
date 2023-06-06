using MelonLoaderInstaller.App.Activities;
using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json.Linq;

namespace MelonLoaderInstaller.App.Utilities
{
    public static class DependencyDownloader
    {
        public static bool Run(string destination, ViewApplication.PatchLogger patchLogger)
        {
            patchLogger.Log("Retrieving release info from GitHub");

            try
            {
                using WebClient client = new WebClient();
                client.Headers["User-Agent"] = "MelonLoaderInstaller/1.0";

                string releaseInfo = client.DownloadString("https://api.github.com/repos/LemonLoader/MelonLoader/releases/latest");
                JObject baseJson = JObject.Parse(releaseInfo);
                JToken asset = baseJson["assets"]
                    .First(a => a["name"].ToString().StartsWith("installer_deps"));
                string assetUrl = asset["browser_download_url"].ToString();

                patchLogger.Log($"Downloading [ {assetUrl} ]");
                client.DownloadFile(assetUrl, destination);
                patchLogger.Log("Done");
            }
            catch (Exception ex)
            {
                patchLogger.Log("Failed to get release info from GitHub, aborting install.\n" + ex.ToString());
                return false;
            }

            return true;
        }
    }
}