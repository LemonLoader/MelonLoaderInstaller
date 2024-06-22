using AssetsTools.NET.Extra;
using System;
using System.IO;
using System.IO.Compression;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace MelonLoader.Installer.Core.PatchSteps
{
    internal class DetectUnityVersion : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            if (patcher._args.UnityVersion != null && patcher._args.UnityVersion != UnityVersion.MinVersion)
                return true;

            using FileStream apkStream = new FileStream(patcher._info.OutputBaseApkPath, FileMode.Open);
            using ZipArchive archive = new ZipArchive(apkStream, ZipArchiveMode.Read);

            AssetsManager uAssetsManager = new AssetsManager();

            // Try to read directly from file
            try
            {
                ZipArchiveEntry assetEntry = archive.GetEntry("bin/Data/globalgamemanagers");
                using Stream stream = assetEntry.Open();

                AssetsFileInstance instance = uAssetsManager.LoadAssetsFile(stream, "/bin/Data/globalgamemanagers", true);
                patcher._args.UnityVersion = UnityVersion.Parse(instance.file.Metadata.UnityVersion);

                return true;
            }
            catch { }

            // If failed before, try to get the data from data.unity3d
            try
            {
                ZipArchiveEntry assetEntry = archive.GetEntry("bin/Data/data.unity3d");
                using Stream stream = assetEntry.Open();

                BundleFileInstance bundle = uAssetsManager.LoadBundleFile(stream, "/bin/Data/data.unity3d");
                AssetsFileInstance instance = uAssetsManager.LoadAssetsFileFromBundle(bundle, "globalgamemanagers");
                patcher._args.UnityVersion = UnityVersion.Parse(instance.file.Metadata.UnityVersion);
            }
            catch (Exception ex)
            {
                patcher._logger.Log("Failed to get Unity version, cannot patch.\n" + ex.ToString());
                patcher._args.UnityVersion = UnityVersion.MinVersion;
                return false;
            }

            return true;
        }
    }
}
