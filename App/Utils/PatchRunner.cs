using AssetRipper.Primitives;
using AssetsTools.NET.Extra;
using MelonLoader.Installer.App.Views;
using MelonLoader.Installer.Core;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using UnityVersion = AssetRipper.Primitives.UnityVersion;

namespace MelonLoader.Installer.App.Utils;

public static class PatchRunner
{
    public static bool IsPatching => _isPatching;

    private static PatchLogger? _logger = null;
    private static PatchingConsolePage? _consolePage = null;

    private static string _basePath = "";
    private static string _tempPath = "";

    private static string _apkOutputPath = "";
    private static string _melonDataPath = "";
    private static string _unityDepsPath = "";

    private static UnityVersion _unityVersion = UnityVersion.MinVersion;

    private static bool _isPatching = false;

    public static async Task Begin(UnityApplicationFinder.Data data, string? localUnityDepsPath)
    {
        // TODO: wrap this all in a trycatch so anything can be thrown onto the screen
        _isPatching = true;

        Reset();

        await Shell.Current.GoToAsync(nameof(PatchingConsolePage));
        _consolePage = (PatchingConsolePage)Shell.Current.CurrentPage;
        _logger = new(_consolePage);

#if ANDROID
        _basePath = Platform.CurrentActivity!.GetExternalFilesDir(null)!.ToString();
#else
        _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MelonLoader.Installer.App");
#endif

        _tempPath = Path.Combine(_basePath, "patch_temp", data.PackageName);

        _melonDataPath = Path.Combine(_tempPath, "melondata.zip");
        _unityDepsPath = Path.Combine(_tempPath, "unity.zip");

        _apkOutputPath = Path.Combine(_tempPath, "output");

        Task task = Task.Run(async() => await InternalBegin(data, localUnityDepsPath));
        await task;

        _isPatching = false;
    }

    private static async Task InternalBegin(UnityApplicationFinder.Data data, string? localUnityDepsPath)
    {
        _logger?.Log($"Build Directory: [ {_basePath} ]");

        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath);

        await GetUnityVersion(data);

        await PrepareAssets(localUnityDepsPath);

        BackupAPKs(data);

        // TODO: backup obbs/data if adb

        CallPatchCore(data);

        // TODO: reinstall

        // TODO: restore obbs/data if adb
    }

    private static async Task GetUnityVersion(UnityApplicationFinder.Data data)
    {
        _logger?.Log("Parsing application Unity version");

        _unityVersion = await UnityVersionFinder.ParseUnityVersion(data, _tempPath);
        if (_unityVersion == UnityVersion.MinVersion)
            return; // TODO: handle failure

        _logger?.Log($"Found [ {_unityVersion} ]");
    }

    private static async Task PrepareAssets(string? localUnityDepsPath)
    {
        _logger?.Log("Preparing assets");

        Directory.CreateDirectory(_tempPath);
        Directory.CreateDirectory(_apkOutputPath);

        _logger?.Log("Created required directories");

        if (localUnityDepsPath != null && File.Exists(localUnityDepsPath))
        {
            File.Copy(localUnityDepsPath, _unityDepsPath);
            _logger?.Log("Local dependencies provided; copied");
        }

        _logger?.Log("Downloading MelonLoader data");

        await DownloadMelonData(_melonDataPath);
    }

    private static async Task<bool> DownloadMelonData(string destination)
    {
        _logger?.Log("Retrieving release info from GitHub");

        try
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "MelonLoaderInstaller/1.0");

            string releaseInfo = await client.GetStringAsync("https://api.github.com/repos/LemonLoader/MelonLoader/releases/latest");
            JObject baseJson = JObject.Parse(releaseInfo);
            JToken asset = baseJson["assets"]!
                .First(a => a["name"]!.ToString().StartsWith("installer_deps"));
            string assetUrl = asset["browser_download_url"]!.ToString();

            _logger?.Log($"Downloading [ {assetUrl} ]");
            byte[] data = await client.GetByteArrayAsync(assetUrl);
            await File.WriteAllBytesAsync(destination, data);
            _logger?.Log("Done");
        }
        catch (Exception ex)
        {
            _logger?.Log("Failed to get release info from GitHub, aborting install.\n" + ex.ToString());
            return false;
        }

        return true;
    }

    private static void BackupAPKs(UnityApplicationFinder.Data data)
    {
        if (data.Source == UnityApplicationFinder.Source.File)
            return;

        if (data.Status != UnityApplicationFinder.Status.Patched)
        {
            _logger?.Log("Backing up APKs");

            string backupDir = data.GetBackupDirectory()!;

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            foreach (string apk in data.APKPaths)
            {
                string backupPath = Path.Combine(backupDir, Path.GetFileName(apk));
                File.Copy(apk, backupPath, true);
            }
        }
        else
            _logger?.Log("App was previously patched, skipping back up");

    }

    private static bool CallPatchCore(UnityApplicationFinder.Data data)
    {
        _logger?.Log("Starting patching core");

        Patcher patcher = new(new()
        {
            TargetApkPath = data.APKPaths.First(),
            LibraryApkPath = data.APKPaths.FirstOrDefault(p => p.Contains("arm64")) ?? "",
            ExtraSplitApkPaths = data.APKPaths.Skip(1).Where(p => !p.Contains("arm64")).ToArray(),
            IsSplit = data.APKPaths.Length > 1,
            OutputApkDirectory = _apkOutputPath,
            TempDirectory = _tempPath,
            LemonDataPath = _melonDataPath,
            Il2CppEtcPath = "" /* TODO: remove this */,
            UnityDependenciesPath = _unityDepsPath,
            UnityVersion = _unityVersion,
            PackageName = data.PackageName
        }, _logger!);

        if (!patcher.Run())
        {
            // TODO: handle failure
            return false;
        }

        _logger?.Log("Application patched successfully, reinstalling.");
        return true;
    }

    private static void Reset()
    {
        _basePath = "";
        _tempPath = "";
        _melonDataPath = "";
        _apkOutputPath = "";
        _unityDepsPath = "";
        _unityVersion = UnityVersion.MinVersion;
    }

    public class PatchLogger : IPatchLogger
    {
        private PatchingConsolePage _consolePage;

        public PatchLogger(PatchingConsolePage consolePage)
        {
            _consolePage = consolePage;
        }

        public void Clear()
        {
            Application.Current!.Dispatcher.Dispatch(() => _consolePage.Log = "");
        }

        public void Log(string message)
        {
            Debug.WriteLine(message);
            Application.Current!.Dispatcher.Dispatch(() => _consolePage.Log += message + '\n');
        }
    }
}
