﻿using AssetsTools.NET.Extra;
using System.IO.Compression;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace MelonLoader.Installer.App.Utils;

public static class UnityVersionFinder
{
    public static async Task<UnityVersion> ParseUnityVersion(UnityApplicationFinder.Data data, string tempPath)
    {
#if ANDROID
        if (data.Source == UnityApplicationFinder.Source.PackageManager)
            return await AndroidParseUnityVersion(data);
#endif
        if (data.Source == UnityApplicationFinder.Source.File)
            return await FileParseUnityVersion(data, data.APKPaths.First());

        if (data.Source == UnityApplicationFinder.Source.ADB)
            return await ADBParseUnityVersion(data, tempPath);

        return UnityVersion.MinVersion;
    }

#if ANDROID
    private static async Task<UnityVersion> AndroidParseUnityVersion(UnityApplicationFinder.Data data)
    {
        Android.Content.Res.AssetManager assetManager = Platform.CurrentActivity!.Assets!;
        Stream? ggmStream = null;
        try
        {
            ggmStream = assetManager.Open("bin/Data/globalgamemanagers");
        }
        catch { }

        Stream? dataStream = null;
        try
        {
            dataStream = assetManager.Open("bin/Data/data.unity3d");
        }
        catch { }

        return await GenericParseUnityVersion(data, ggmStream, dataStream);
    }
#endif

    private static async Task<UnityVersion> FileParseUnityVersion(UnityApplicationFinder.Data data, string apkPath)
    {
        using FileStream apkStream = new(apkPath, FileMode.Open);
        using ZipArchive archive = new(apkStream, ZipArchiveMode.Read);

        Stream? ggmStream = archive.GetEntry("assets/bin/Data/globalgamemanagers")?.Open();
        Stream? dataStream = archive.GetEntry("assets/bin/Data/data.unity3d")?.Open();

        return await GenericParseUnityVersion(data, ggmStream, dataStream);
    }

    private static async Task<UnityVersion> ADBParseUnityVersion(UnityApplicationFinder.Data data, string tempPath)
    {
        string deviceApkPath = data.APKPaths.First();
        string destinationPath = Path.Combine(tempPath, Path.GetFileName(deviceApkPath));

        await ADBManager.PullFileToPath(deviceApkPath, destinationPath);

        UnityVersion res = await FileParseUnityVersion(data, destinationPath);

        File.Delete(destinationPath);

        return res;
    }

    private static async Task<UnityVersion> GenericParseUnityVersion(UnityApplicationFinder.Data data, Stream? globalgamemanagers, Stream? dataUnity3d)
    {
        UnityVersion version = UnityVersion.MinVersion;

        AssetsManager uAssetsManager = new();

        // Try to read directly from file
        try
        {
            Stream stream = globalgamemanagers ?? throw new Exception();
            using MemoryStream memoryStream = new();
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            stream.Dispose();

            AssetsFileInstance instance = uAssetsManager.LoadAssetsFile(memoryStream, "/bin/Data/globalgamemanagers", true);
            version = TryParseUnityVersion(instance.file.Metadata.UnityVersion, data);
        }
        catch { }

        // If failed before, try to get the data from data.unity3d
        try
        {
            Stream stream = dataUnity3d ?? throw new Exception("data.unity3d does not exist");

            using MemoryStream memoryStream = new();
            byte[] buffer = new byte[4096];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            stream.Dispose();

            BundleFileInstance bundle = uAssetsManager.LoadBundleFile(memoryStream, "/bin/Data/data.unity3d");
            AssetsFileInstance instance = uAssetsManager.LoadAssetsFileFromBundle(bundle, "globalgamemanagers");
            version = TryParseUnityVersion(instance.file.Metadata.UnityVersion, data);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Failed to get Unity version for package " + data.PackageName);
            System.Diagnostics.Debug.WriteLine(ex);
            version = UnityVersion.MinVersion;
        }

        return version;
    }

    private static UnityVersion TryParseUnityVersion(string version, UnityApplicationFinder.Data data)
    {
        try
        {
            return UnityVersion.Parse(version);
        }
        catch
        {
            System.Diagnostics.Debug.WriteLine($"Package {data.PackageName} has unparsable version of {version}");
            return UnityVersion.MinVersion;
        }
    }
}