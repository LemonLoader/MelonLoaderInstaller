using AdvancedSharpAdbClient.Exceptions;
using MelonLoader.Installer.App.Views;
using MelonLoader.Installer.Core;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
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

#if ANDROID
            _basePath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory!.ToString(), "MelonLoader.Installer.App");
#else
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MelonLoader.Installer.App");
#endif

            _logger = new(_consolePage, _basePath);
            _logger?.Log($"Created Log File [ {_logger.LogPath} ]");

            string packageName = data.Source == UnityApplicationFinder.Source.File ? "LocalFile" : data.PackageName;
            _tempPath = Path.Combine(_basePath, "patch_temp", packageName);

            _melonDataPath = Path.Combine(_tempPath, "melondata.zip");
            _unityDepsPath = Path.Combine(_tempPath, "unity.bin");

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

        await ReinstallApp(data);

        await RestoreAppData(data);

        MarkSuccess();
    }

    public static async Task BeginRestore(UnityApplicationFinder.Data data)
    {
        _isPatching = true;

        try
        {
            Reset();

            await Shell.Current.GoToAsync(nameof(PatchingConsolePage));
            _consolePage = (PatchingConsolePage)Shell.Current.CurrentPage;
            _logger = new(_consolePage, _basePath);

            _consolePage.Title = "Restoring...";

            Task task = Task.Run(async () => await InternalBeginRestore(data));
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

    private static async Task InternalBeginRestore(UnityApplicationFinder.Data data)
    {
        await BackupAppData(data);

        string? backupDir = data.GetBackupDirectory();
        if (backupDir == null)
            return;

        APKInstaller installer = new(data,
            _logger!,
            async () =>
            {
                await PopupHelper.Alert("Unable to restore the application, read the console for more info.", "Failure");
                _consolePage!.BackButtonVisible = true;
            });

        await installer.Install(backupDir);

        await RestoreAppData(data);

        await PopupHelper.Alert("Successfully restored the application.", "Success");
        _consolePage!.BackButtonVisible = true;
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

        if (!await DownloadMelonData(_melonDataPath))
            throw new Exception("Failed to download MelonLoader data");
    }

    private static async Task<bool> DownloadMelonData(string destination)
    {
#if DEBUG
        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "melon_data/melon_data.zip")))
        {
            _logger?.Log("Using local melon data");

            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "melon_data/melon_data.zip"), destination);
            return true;
        }

        if (File.Exists(@"/sdcard/Download/melon_data.zip"))
        {
            _logger?.Log("Using local melon data");

            File.Copy(@"/sdcard/Download/melon_data.zip", destination);
            return true;
        }
#endif

        _logger?.Log("Retrieving release info from GitHub");

        try
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "MelonLoaderInstaller/1.0");

            string releaseInfo = await client.GetStringAsync("https://api.github.com/repos/LemonLoader/MelonLoader/releases/latest");
            JObject baseJson = JObject.Parse(releaseInfo);
            JToken asset = baseJson["assets"]!
                .First(a => a["name"]!.ToString().StartsWith("melon_data"));
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

        return true;
    }

    private static async Task BackupAppData(UnityApplicationFinder.Data data)
    {
        // backing up files on-device on most recent android versions is a massive pain, if not impossible
        if (data.Source != UnityApplicationFinder.Source.ADB)
            return;

        _logger?.Log("Backing up app data, this can take awhile");

        string src = $"/sdcard/Android/data/{data.PackageName}";
        string dest = Path.Combine(_tempPath, "data_backup"); // the package name isnt here due to how adb handles pulling

        try
        {
            await ADBManager.PullDirectoryToPath(src, dest, true, _logger);
        }
        catch (AdbException ex)
        {
            _logger?.Log($"Failed to back up data directory.\n{ex}");
            bool res = await PopupHelper.TwoAnswerQuestion("Backing up game data failed, you may lose any save data or additional game data if the patching continues. Do you want to continue anyway?", "Unable to Back Up Data", "Yes", "No");
            if (!res)
                throw;
        }

        _logger?.Log("Backing up app assets, this can take awhile");

        src = $"/sdcard/Android/obb/{data.PackageName}";
        dest = $"/sdcard/Android/{data.PackageName}.obb.lemon";

        await ADBManager.TryMoveUsingScript(src, dest);
    }

    private static async Task RestoreAppData(UnityApplicationFinder.Data data)
    {
        // backing up files on-device on most recent android versions is a massive pain, if not impossible
        if (data.Source != UnityApplicationFinder.Source.ADB)
            return;

        // I tried so many things and all of them caused permission issues.
        // At this point, I give up and will let users restore it themselves.

        /*_logger?.Log("Restoring app data, this can take awhile");

        string src = Path.Combine(_tempPath, "data_backup", $"{data.PackageName}");
        string dest = $"/sdcard/Android/data/"; // the package name isnt here due to how adb handles pushing

        await ADBManager.PushDirectoryToDevice(src, dest, _logger);
        await ADBManager.AttemptPermissionReset($"/sdcard/Android/data/{data.PackageName}");*/

        _logger?.Log("Restoring app assets (OBBs), this can take awhile");

        string src = $"/sdcard/Android/{data.PackageName}.obb.lemon";
        string dest = $"/sdcard/Android/obb/{data.PackageName}";

        await ADBManager.TryMoveUsingScript(src, dest);

        _logger?.Log("Asset restore complete");

        // ask about game data

        string dataBackupPath = Path.Combine(_tempPath, "data_backup", $"{data.PackageName}");
        if (Directory.Exists(dataBackupPath) && Directory.GetFileSystemEntries(dataBackupPath).Length > 0)
        {
            await Application.Current!.Dispatcher.DispatchAsync(async () =>
            {
                bool res = await PopupHelper.TwoAnswerQuestion("Lemon is unable to restore app data, do you want to open the folder containing the back up now? It will not be deleted after patching is complete.", "Unable to Restore", "Yes", "No");
                if (res)
                {
                    Process proc = new()
                    {
                        StartInfo =
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{dataBackupPath}\""
                    }
                    };

                    proc.Start();
                }
            });
        }
    }

    private static async Task ReinstallApp(UnityApplicationFinder.Data data)
    {
        if (data.Source == UnityApplicationFinder.Source.File)
        {
#if WINDOWS
            _logger?.Log("APK patched successfully, moving to final path");

            string originalPath = data.APKPaths.First();
            string newPatchedPath = Path.Combine(Path.GetDirectoryName(originalPath)!, Path.GetFileNameWithoutExtension(originalPath) + "_patched.apk");
            string patchedPath = Directory.GetFiles(_apkOutputPath).First();

            File.Move(patchedPath, newPatchedPath, true);

            _logger?.Log($"Patched APK saved [ {patchedPath} ]");


            Process proc = new()
            {
                StartInfo =
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{newPatchedPath}\""
                }
            };

            proc.Start();
#else
            _logger?.Log("APK patched successfully.");
            _logger?.Log($"Patched APK(s) are available at [ {_apkOutputPath} ]");
#endif

            return;
        }

        _logger?.Log("Application patched successfully, reinstalling");

        APKInstaller installer = new(data, _logger!, MarkFailure);
        await installer.Install(_apkOutputPath);
    }

    private static void MarkSuccess()
    {
        Application.Current!.Dispatcher.Dispatch(async () =>
        {
            _logger?.Log("Patching complete");

            await PopupHelper.Alert("Successfully patched the application.", "Success");

            _consolePage!.BackButtonVisible = true;

            _isPatching = false;
        });
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
        public string LogPath => _logPath;

        private PatchingConsolePage _consolePage;
        private string _logPath;

        private FileStream _fileStream;
        private StreamWriter _writer;

        public PatchLogger(PatchingConsolePage consolePage, string basePath)
        {
            string baseLogPath = Path.Combine(basePath, "Logs");
            if (!Directory.Exists(baseLogPath))
                Directory.CreateDirectory(baseLogPath);

            _consolePage = consolePage;
            _logPath = Path.Combine(baseLogPath, $"{DateTime.Now:%y-%M-%d_%H-%m-%s}.log");

            _fileStream = File.Open(_logPath, new FileStreamOptions() { Access = FileAccess.ReadWrite, BufferSize = 0, Mode = FileMode.Create, Share = FileShare.ReadWrite });
            _writer = new StreamWriter(_fileStream, Encoding.UTF8, 1)
            {
                AutoFlush = true
            };
        }

        public void Clear()
        {
            Application.Current!.Dispatcher.Dispatch(() => _consolePage.Log = "");
        }

        public void Log(string message)
        {
            Debug.WriteLine(message);
            _writer.WriteLine(message);
            Application.Current!.Dispatcher.Dispatch(() => _consolePage.Log += message + '\n');
        }
    }
}
