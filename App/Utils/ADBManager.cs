using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Receivers;
using System.Reflection;
using AdvancedSharpAdbClient.Exceptions;
using AdvancedSharpAdbClient.DeviceCommands;
using MelonLoader.Installer.Core;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace MelonLoader.Installer.App.Utils;

internal static partial class ADBManager
{
    public static Action? OnPrimaryDeviceChanged { get; set; } = null;

    private static AdbClient? _adbClient;
    private static DeviceData? _deviceData;

    private static bool _initialized;
    private static bool _listingToolInstalled;
    private static bool _copyingToolInstalled;

    private static string _baseDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
    private static string? _adbExePath;

    public static void Initialize()
    {
        // we are probably inside AppX where the resources dir isnt at
        if (!Directory.Exists(Path.Combine(_baseDir, "Resources")))
            _baseDir = Path.GetDirectoryName(_baseDir)!;

        _adbExePath = Path.Combine(_baseDir, "Resources", "platform-tools", "adb.exe");

        if (!AdbServer.Instance.GetStatus().IsRunning)
        {
            AdbServer server = new();
            StartServerResult result = server.StartServer(_adbExePath, false);
            if (result != StartServerResult.Started)
                Debug.WriteLine("Unable to start ADB server.");
        }

        _adbClient = new();
        _initialized = true;
    }

    public static void SetPrimaryDevice(DeviceData data)
    {
        _deviceData = data;
        _listingToolInstalled = false;
        OnPrimaryDeviceChanged?.Invoke();
    }

    public static IEnumerable<DeviceData> GetDevices()
    {
        if (!_initialized)
            Initialize();

        return _adbClient?.GetDevices() ?? Enumerable.Empty<DeviceData>();
    }

    public static bool IsArm64(DeviceData deviceData)
    {
        ConsoleOutputReceiver receiver = new();
        _adbClient?.ExecuteRemoteCommand("getprop ro.product.cpu.abilist64", deviceData, receiver);
        string output = receiver.ToString();
        if (output.Contains("arm64-v8a"))
            return true;

        return false;
    }

    public static async Task InstallAPK(string apkPath)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        using FileStream stream = File.OpenRead(apkPath);
        await _adbClient.InstallAsync(_deviceData.Value, stream);
    }

    public static async Task InstallMultipleAPKs(string baseApkPath, string[] splitApks)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        using FileStream baseApkStream = File.OpenRead(baseApkPath);

        var splitStreams = splitApks.Select(File.OpenRead);

        await _adbClient.InstallMultipleAsync(_deviceData.Value, baseApkStream, splitStreams);

        foreach (var stream in splitStreams)
            await stream.DisposeAsync();
    }

    public static async Task UninstallPackage(string packageName)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        await _adbClient.UninstallAsync(_deviceData.Value, packageName);
    }

    public static void InstallAppListingTool()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string toolPath = Path.Combine(_baseDir, "Resources", "melonapplisting.dex");
        using SyncService service = new(_deviceData.Value);
        using FileStream stream = File.OpenRead(toolPath);
        service.Push(stream, "/data/local/tmp/melonapplisting.dex", UnixFileStatus.AccessPermissions, DateTimeOffset.Now, null);
        _listingToolInstalled = true;
    }

    public static void UninstallAppListingTool()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string path = "/data/local/tmp/melonapplisting.dex";
        _adbClient.ExecuteRemoteCommand($"rm {path}", _deviceData.Value);
        _listingToolInstalled = false;
    }

    public static void InstallCopyScript()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string toolPath = Path.Combine(_baseDir, "Resources", "lemon_copy_util.sh");
        using SyncService service = new(_deviceData.Value);
        using FileStream stream = File.OpenRead(toolPath);
        service.Push(stream, "/data/local/tmp/lemon_copy_util.sh", UnixFileStatus.AccessPermissions, DateTimeOffset.Now, null);
        _adbClient.ExecuteRemoteCommand("chmod +x /data/local/tmp/lemon_copy_util.sh", _deviceData.Value);
        _copyingToolInstalled = true;
    }

    public static async Task TryMoveUsingScript(string source, string dest)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        if (!_copyingToolInstalled)
            InstallCopyScript();

        LineReceiver rc = new();
        await _adbClient.ExecuteRemoteCommandAsync($"/system/bin/sh /data/local/tmp/lemon_copy_util.sh \"{source}\" \"{dest}\"", _deviceData.Value, rc);
    }

    public static async Task PullFileToPath(string devicePath, string destinationPath)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        try
        {
            using SyncService service = new(_deviceData.Value);
            using FileStream stream = File.OpenWrite(destinationPath);
            await service.PullAsync(devicePath, stream, null);
        }
        catch (AdbException)
        {
            // likely a file not found
        }
    }

    public static async Task PullDirectoryToPath(string deviceFolderPath, string destinationFolderPath, bool stripIl2cpp = false, IPatchLogger? logger = null)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        if (!Directory.Exists(destinationFolderPath))
            Directory.CreateDirectory(destinationFolderPath);

        logger?.Log($"Working in {deviceFolderPath}");

        await CallADBDirect($"pull \"{deviceFolderPath}\" \"{destinationFolderPath}\"");

        if (stripIl2cpp)
        {
            // assumes we're working on an Android data folder
            string path = Path.Combine(destinationFolderPath, Path.GetFileName(deviceFolderPath).TrimEnd('/'));

            string il2Folder = Path.Combine(path, "files", "il2cpp");
            if (Directory.Exists(il2Folder))
                Directory.Delete(il2Folder, true);

            // unity creates a "il2cpp_tmp" folder when the il2cpp folder can't be accessed (which happens with this pull/push cycle for some reason)
            il2Folder = Path.Combine(path, "files", "il2cpp_tmp");
            if (Directory.Exists(il2Folder))
                Directory.Delete(il2Folder, true);
        }
    }

    public static async Task PushDirectoryToDevice(string sourceFolderPath, string deviceFolderPath, IPatchLogger? logger = null)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        if (!Directory.Exists(sourceFolderPath))
            return;

        logger?.Log($"Pushing {sourceFolderPath}");

        for (int i = 0; i < 5; i++)
        {
            try
            {
                await CallADBDirect($"push --sync \"{sourceFolderPath}\" \"{deviceFolderPath}\"");
                break;
            }
            catch (AdbException ex)
            {
                // keep retrying if its a mkdir issue, it resolves itself
                if (ex.Message.Contains("secure_mkdirs failed"))
                {
                    logger?.Log("mkdir error; retrying");
                    i--;
                }
                else
                    logger?.Log($"Failed to push {sourceFolderPath}\n{ex}");
            }
        }
    }

    public static async Task AttemptPermissionReset(string path)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        await _adbClient.ExecuteShellCommandAsync(_deviceData.Value, $"find \"{path}\" -type d -exec chmod 760 {{}} \\; -o -type f -exec chmod 660 {{}} \\;");
    }

    private static async Task CallADBDirect(string command)
    {
        if (_deviceData == null || _adbClient == null)
            return;

        Process proc = new()
        {
            StartInfo = new()
            {
                FileName = _adbExePath,
                Arguments = $"-s {_deviceData!.Value.Serial} {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        proc.Start();
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
            throw new AdbException(await proc.StandardOutput.ReadToEndAsync());
    }

    const string LISTING_TOOL_SPLIT = "-----------------------------------";
    const string LISTING_TOOL_DNS = "DEVICE_NOT_SUPPORTED";
    public static List<UnityApplicationFinder.Data> GetAppDatasFromListingTool()
    {
        if (_deviceData == null || _adbClient == null)
            return [];

        if (!_listingToolInstalled)
            InstallAppListingTool();

        LineReceiver receiver = new();
        _adbClient?.ExecuteRemoteCommand("/system/bin/app_process -Djava.class.path=\"/data/local/tmp/melonapplisting.dex\" /system/bin --nice-name=NativeAppListing com.melonloader.nativeapplisting.Core", _deviceData.Value, receiver);

        if (receiver.Listings.Count <= 0 || receiver.Listings[0] == LISTING_TOOL_DNS || receiver.Listings[0] != LISTING_TOOL_SPLIT)
            return [];

        List<UnityApplicationFinder.Data> datas = [];
        for (int i = 0; i < receiver.Listings.Count; i++)
        {
            if (receiver.Listings[i] == LISTING_TOOL_SPLIT)
            {
                i++; // go to AppName
                string appName = receiver.Listings[i].ToString();

                i++; // go to PackageName
                string packageName = receiver.Listings[i].ToString();

                i++; // go to Status
                UnityApplicationFinder.Status status = Enum.Parse<UnityApplicationFinder.Status>(receiver.Listings[i], true);

                if (packageName!.IsBad())
                    status = UnityApplicationFinder.Status.Unsupported;

                i++; // go to RawIconData
                byte[] iconData = Convert.FromBase64String(receiver.Listings[i]);
                
                i++; // rest are apk paths
                List<string> apkPaths = [];
                while (i < receiver.Listings.Count && receiver.Listings[i] != LISTING_TOOL_SPLIT)
                {
                    apkPaths.Add(receiver.Listings[i]);
                    i++;
                }

                apkPaths.Reverse(); // apparently the java code causes a flipped list of apks; i presume i just misunderstood the api

                i--; // go back so this process can run again

                UnityApplicationFinder.Data data = new(appName, packageName, status, UnityApplicationFinder.Source.ADB, [.. apkPaths], iconData);
                datas.Add(data);
            }
        }

        UninstallAppListingTool();

        return datas;
    }

    private class LineReceiver : MultiLineReceiver
    {
        public List<string> Listings = [];

        protected override void ProcessNewLines(IEnumerable<string> lines)
        {
            Listings.AddRange(lines);
        }
    }
}
