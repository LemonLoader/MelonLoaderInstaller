using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Receivers;
using System.Text.RegularExpressions;
using System.Reflection;
using AdvancedSharpAdbClient.Exceptions;

namespace MelonLoader.Installer.App.Utils;

internal static partial class ADBManager
{
    public static Action? OnPrimaryDeviceChanged { get; set; } = null;

    private static AdbClient? _adbClient;
    private static DeviceData? _deviceData;

    private static bool _initialized;
    private static bool _aaptInstalled;

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
        _aaptInstalled = false;
        OnPrimaryDeviceChanged?.Invoke();
    }

    public static void InstallAapt2()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string aaptPath = Path.Combine(_baseDir, "Resources", "lemon-aapt2-arm64");
        using SyncService service = new(_deviceData.Value);
        using FileStream stream = File.OpenRead(aaptPath);
        service.Push(stream, "/data/local/tmp/lemon-aapt2-arm64", UnixFileStatus.AccessPermissions, DateTimeOffset.Now, null);
        _aaptInstalled = true;
    }

    public static void UninstallAapt2()
    {
        if (_deviceData == null || _adbClient == null)
            return;

        string path = "/data/local/tmp/lemon-aapt2-arm64";
        _adbClient.ExecuteRemoteCommand($"rm {path}", _deviceData.Value);
        _aaptInstalled = false;
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

    public static string[] GetPackages()
    {
        if (_deviceData == null || _adbClient == null)
            return [];

        LineReceiver receiver = new(true);
        _adbClient.ExecuteRemoteCommand("cmd package list packages -e", _deviceData.Value, receiver);
        return [.. receiver.Listings];
    }

    public static string[] GetPackageAPKPaths(string package)
    {
        if (_deviceData == null || _adbClient == null)
            return [];

        LineReceiver receiver = new(true);
        _adbClient.ExecuteRemoteCommand("pm path " + package, _deviceData.Value, receiver);
        return [.. receiver.Listings];
    }

    public static string GetApplicationLabel(string apkPath)
    {
        if (_deviceData == null || _adbClient == null)
            return "";

        if (!_aaptInstalled)
            InstallAapt2();

        ConsoleOutputReceiver receiver = new();
        _adbClient?.ExecuteRemoteCommand($"/data/local/tmp/aapt2-arm64-v8a dump badging {apkPath} | grep application-label", _deviceData.Value, receiver);
        string output = receiver.ToString();
        string name = MatchApplicationLabel().Match(output).Groups[1].Value;

        return name;
    }

    public static string[] GetNativeLibraries(string package, out string nativeLibPath)
    {
        if (_deviceData == null || _adbClient == null)
        {
            nativeLibPath = "";
            return [];
        }

        ConsoleOutputReceiver receiver = new();
        _adbClient?.ExecuteRemoteCommand($"dumpsys package {package} | grep legacyNativeLibraryDir=", _deviceData.Value, receiver);
        string output = receiver.ToString();

        nativeLibPath = MatchNativeLibraryDir().Match(output).Groups[1].Value + "/arm64";

        LineReceiver lineReceiver = new();
        try
        {
            _adbClient?.ExecuteRemoteCommand($"ls {nativeLibPath}", _deviceData.Value, receiver);
        }
        catch (ShellCommandUnresponsiveException)
        {
            return [];
        }

        if (lineReceiver.Listings.Count <= 0 || lineReceiver.Listings[0].Contains("No such file or directory"))
            return [];

        return lineReceiver.Listings.ToArray();
    }

    private class LineReceiver(bool trimPackage = false) : MultiLineReceiver
    {
        public List<string> Listings = [];

        private bool _trimPackage = trimPackage;

        protected override void ProcessNewLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (_trimPackage && line.StartsWith("package:"))
                    Listings.Add(line[8..]);
                else
                    Listings.Add(line);
            }
        }
    }

    [GeneratedRegex(@"^application-label:'([^']*)'")]
    private static partial Regex MatchApplicationLabel();
    [GeneratedRegex(@"legacyNativeLibraryDir=([^\s]*)")]
    private static partial Regex MatchNativeLibraryDir();
}
