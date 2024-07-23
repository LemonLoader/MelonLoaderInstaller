using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Receivers;

namespace MelonLoader.Installer.App.Utils;

internal static class ADBManager
{
    public static Action? OnPrimaryDeviceChanged { get; set; } = null;

    private static AdbClient? _adbClient;
    private static DeviceData? _deviceData;

    private static bool _initialized;

    public static void Initialize()
    {
        if (!AdbServer.Instance.GetStatus().IsRunning)
        {
            AdbServer server = new();
            StartServerResult result = server.StartServer(Path.Combine(Directory.GetCurrentDirectory(), "Resources", "platform-tools", "adb.exe"), false);
            if (result != StartServerResult.Started)
                System.Diagnostics.Debug.WriteLine("Unable to start ADB server.");
        }

        _adbClient = new();
        _initialized = true;
    }

    public static void SetPrimaryDevice(DeviceData data)
    {
        _deviceData = data;
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
}
