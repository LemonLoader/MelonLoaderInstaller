using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient;

namespace MelonLoader.Installer.App.Utils;

internal static class ADBManager
{
    private static AdbClient? _adbClient;
    private static DeviceData? _deviceData;

    private static bool _initialized;

    public static void Initialize()
    {
        if (!AdbServer.Instance.GetStatus().IsRunning)
        {
            AdbServer server = new();
            StartServerResult result = server.StartServer(@"adb.exe", false); // TODO: add adb exe to build
            if (result != StartServerResult.Started)
                System.Diagnostics.Debug.WriteLine("Unable to start ADB server.");
        }

        _adbClient = new();
        _initialized = true;
    }

    public static void SetPrimaryDevice(DeviceData data)
    {
        _deviceData = data;
    }

    public static IEnumerable<DeviceData> GetDevices()
    {
        if (!_initialized)
            Initialize();

        return _adbClient?.GetDevices() ?? Enumerable.Empty<DeviceData>();
    }
}
