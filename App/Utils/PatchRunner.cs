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
        _isPatching = true;

        try
        {
            Reset();

            await Shell.Current.GoToAsync(nameof(PatchingConsolePage));
            _consolePage = (PatchingConsolePage)Shell.Current.CurrentPage;
            _logger = new(_consolePage);

#if ANDROID
            _basePath = Platform.CurrentActivity!.GetExternalFilesDir(null)!.ToString();
#else
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MelonLoader.Installer.App");
#endif

            string packageName = data.Source == UnityApplicationFinder.Source.File ? "LocalFile" : data.PackageName;
            _tempPath = Path.Combine(_basePath, "patch_temp", packageName);

            _melonDataPath = Path.Combine(_tempPath, "melondata.zip");
            _unityDepsPath = Path.Combine(_tempPath, "unity.zip");

            _apkOutputPath = Path.Combine(_tempPath, "output");

            if (Directory.Exists(_tempPath))
                Directory.Delete(_tempPath, true);

            Task task = Task.Run(async () => await InternalBegin(data, localUnityDepsPath));
            await task;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _logger?.Log(ex.ToString());
            MarkFailure();
        }

        _isPatching = false;
        _consolePage!.BackButtonVisible = true;
    }

    private static async Task InternalBegin(UnityApplicationFinder.Data data, string? localUnityDepsPath)
    {
        _logger?.Log($"Build Directory: [ {_basePath} ]");

        if (Directory.Exists(_tempPath))
            Directory.Delete(_tempPath);

        await PrepareAssets(localUnityDepsPath);

        await GetUnityVersion(data);

        await BackupAPKs(data);

        await CopyAPKsFromDevice(data);

        await BackupAppData(data);

        CallPatchCore(data);

        // TODO: reinstall

        await RestoreAppData(data);
    }

    private static async Task GetUnityVersion(UnityApplicationFinder.Data data)
    {
        _logger?.Log("Parsing application Unity version");

        _unityVersion = await UnityVersionFinder.ParseUnityVersion(data, _tempPath);
        if (_unityVersion == UnityVersion.MinVersion)
        {
            throw new Exception("Failed to parse Unity version");
        }

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

    private static async Task BackupAPKs(UnityApplicationFinder.Data data)
    {
        if (data.Source == UnityApplicationFinder.Source.File)
            return;

        if (data.Status == UnityApplicationFinder.Status.Patched)
        {
            _logger?.Log("App was previously patched, skipping back up");
            return;
        }

        _logger?.Log("Backing up APKs");

        string backupDir = data.GetBackupDirectory()!;

        if (!Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);

        foreach (string apk in data.APKPaths)
        {
            _logger?.Log($"Backing up [ {apk} ]");
            string backupPath = Path.Combine(backupDir, Path.GetFileName(apk));
            if (data.Source == UnityApplicationFinder.Source.PackageManager)
                File.Copy(apk, backupPath, true);
            else
                await ADBManager.PullFileToPath(apk, backupPath);
        }

    }

    private static async Task CopyAPKsFromDevice(UnityApplicationFinder.Data data)
    {
        if (data.Source != UnityApplicationFinder.Source.ADB)
            return;

        _logger?.Log("Copying APKs from device");

        foreach (string apk in data.APKPaths)
        {
            _logger?.Log($"Copying [ {apk} ]");
            await ADBManager.PullFileToPath(apk, Path.Combine(_apkOutputPath, Path.GetFileName(apk)));
        }
    }

    private static async Task BackupAppData(UnityApplicationFinder.Data data)
    {
        // backing up files on-device on most recent android versions is a massive pain, if not impossible
        if (data.Source != UnityApplicationFinder.Source.ADB)
            return;

        _logger?.Log("Backing up app data");

        string src = $"/sdcard/Android/data/{data.PackageName}";
        string dest = $"/sdcard/Android/data/{data.PackageName}.lemon";

        await ADBManager.ShellMove(src, dest);

        _logger?.Log("Backing up app assets");

        src = $"/sdcard/Android/obb/{data.PackageName}";
        dest = $"/sdcard/Android/obb/{data.PackageName}.lemon";

        await ADBManager.ShellMove(src, dest);
    }

    private static bool CallPatchCore(UnityApplicationFinder.Data data)
    {
        _logger?.Log("Starting patching core");

        string targetApkPath;
        string libraryApkPath;
        string[] extraSplits;

        if (data.Source is UnityApplicationFinder.Source.PackageManager or UnityApplicationFinder.Source.File)
        {
            targetApkPath = data.APKPaths.First();
            libraryApkPath = data.APKPaths.FirstOrDefault(p => p.Contains("arm64")) ?? "";
            extraSplits = data.APKPaths.Skip(1).Where(p => !p.Contains("arm64")).ToArray();
        }
        else
        {
            string? arm64Split = data.APKPaths.FirstOrDefault(p => p.Contains("arm64"));

            targetApkPath = Path.Combine(_apkOutputPath, Path.GetFileName(data.APKPaths.First()));
            libraryApkPath = arm64Split == null ? "" : Path.Combine(_apkOutputPath, Path.GetFileName(arm64Split));
            extraSplits = data.APKPaths.Skip(1).Where(p => !p.Contains("arm64")).Select(p => Path.Combine(_apkOutputPath, Path.GetFileName(p))).ToArray();
        }

        Patcher patcher = new(new()
        {
            TargetApkPath = targetApkPath,
            LibraryApkPath = libraryApkPath,
            ExtraSplitApkPaths = extraSplits,
            IsSplit = data.APKPaths.Length > 1,
            OutputApkDirectory = _apkOutputPath,
            TempDirectory = _tempPath,
            MelonDataPath = _melonDataPath,
            UnityDependenciesPath = _unityDepsPath,
            UnityVersion = _unityVersion,
            PackageName = data.PackageName
        }, _logger!);

        if (!patcher.Run())
        {
            throw new Exception("Failed to patch.");
        }

        _logger?.Log("Application patched successfully, reinstalling.");
        return true;
    }

    private static async Task RestoreAppData(UnityApplicationFinder.Data data)
    {
        // backing up files on-device on most recent android versions is a massive pain, if not impossible
        if (data.Source != UnityApplicationFinder.Source.ADB)
            return;

        _logger?.Log("Restoring app data");

        string src = $"/sdcard/Android/data/{data.PackageName}.lemon";
        string dest = $"/sdcard/Android/data/{data.PackageName}";

        await ADBManager.ShellMove(src, dest);

        _logger?.Log("Restoring app assets");

        src = $"/sdcard/Android/obb/{data.PackageName}.lemon";
        dest = $"/sdcard/Android/obb/{data.PackageName}";

        await ADBManager.ShellMove(src, dest);
    }

    private static void MarkFailure()
    {
        Application.Current!.Dispatcher.Dispatch(async () =>
        {
            _logger?.Log("Patching failed.");

            await PopupHelper.Alert("Unable to patch the application, read the console for more info.", "Failure");

            _consolePage!.BackButtonVisible = true;

            _isPatching = false;
        });
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
