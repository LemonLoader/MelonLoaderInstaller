using System.IO;
using System.Net;

namespace MelonLoaderInstaller.Core.PatchSteps
{
    internal class DownloadUnityDeps : IPatchStep
    {
        private const string DEPS_PROVIDER = "https://lemon.sircoolness.dev/android/";

        public bool Run(Patcher patcher)
        {
            if (File.Exists(patcher._args.UnityDependenciesPath))
            {
                patcher._logger.Log("Using local Unity dependencies!");
                return true;
            }

            patcher._logger.Log("Downloading Unity dependencies");

            using WebClient client = new WebClient();

            if (!patcher._args.UnityVersion.HasValue)
            {
                patcher._logger.Log("Got to DownloadUnityDeps while UnityVersion is null, how did this happen?");
                return false;
            }   
            
            string unityVersion = patcher._args.UnityVersion.Value.ToStringWithoutType();

            try
            {
                client.DownloadFile(DEPS_PROVIDER + unityVersion + ".zip", patcher._args.UnityDependenciesPath);
            }
            catch (WebException ex)
            {
                patcher._logger.Log("Failed to download Unity dependencies. You may need to generate them manually.\n" + ex.ToString());
                return false;
            }

            return true;
        }
    }
}
