using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Receivers;
using System.Reflection;

namespace MelonLoader.Installer.App.Utils;

internal static partial class ADBManager
{
    public static Action? OnPrimaryDeviceChanged { get; set; } = null;

    private static AdbClient? _adbClient;
    private static DeviceData? _deviceData;

    private static bool _initialized;
    private static bool _listingToolInstalled;

    private static string _baseDir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

    public static void Initialize()
    {
        // we are probably inside AppX where the resources dir isnt at
        if (!Directory.Exists(Path.Combine(_baseDir, "Resources")))
            _baseDir = Path.GetDirectoryName(_baseDir)!;

        if (!AdbServer.Instance.GetStatus().IsRunning)
        {
            AdbServer server = new();
            StartServerResult result = server.StartServer(Path.Combine(_baseDir, "Resources", "platform-tools", "adb.exe"), false);
            if (result != StartServerResult.Started)
                System.Diagnostics.Debug.WriteLine("Unable to start ADB server.");
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

    public static void InstallAppListingTool()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string aaptPath = Path.Combine(_baseDir, "Resources", "melonapplisting.dex");
        using SyncService service = new(_deviceData.Value);
        using FileStream stream = File.OpenRead(aaptPath);
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

    const string LISTING_TOOL_SPLIT = "-----------------------------------";
    public static List<UnityApplicationFinder.Data> GetAppDatasFromListingTool()
    {
        if (_deviceData == null || _adbClient == null)
            return [];

        if (!_listingToolInstalled)
            InstallAppListingTool();

        LineReceiver receiver = new();
        _adbClient?.ExecuteRemoteCommand("/system/bin/app_process -Djava.class.path=\"/data/local/tmp/melonapplisting.dex\" /system/bin --nice-name=NativeAppListing com.melonloader.nativeapplisting.Core", _deviceData.Value, receiver);

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

                i++; // go to RawIconData
                byte[] iconData = Convert.FromBase64String(receiver.Listings[i]);
                
                i++; // rest are apk paths
                List<string> apkPaths = [];
                while (i < receiver.Listings.Count && receiver.Listings[i] != LISTING_TOOL_SPLIT)
                {
                    apkPaths.Add(receiver.Listings[i]);
                    i++;
                }

                i--; // go back so this process can run again

                UnityApplicationFinder.Data data = new(appName, packageName, status, iconData);
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
