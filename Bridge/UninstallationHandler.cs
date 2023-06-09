using Microsoft.Extensions.Logging;
using SharpAdbClient;
using System.Diagnostics;
using System.Net;
using Websocket.Client;

namespace LemonADBBridge
{
    internal static class UninstallationHandler
    {
        private static WebsocketClient wsClient;
        private static AdbClient adbClient;
        private static DeviceData deviceData;

        private static string? packageToUninstall;

        public static async Task Run(AdbClient client, DeviceData data, MainForm mainForm)
        {
            deviceData = data;
            adbClient = client;

            mainForm.statusText.Text = "WAITING FOR CONNECTION...";

            while (true)
            {
                StringReceiver rec = new();
                adbClient.ExecuteRemoteCommand("cat /sdcard/Android/data/com.melonloader.installer/files/temp/adbbridge.txt", deviceData, rec);
                string result = rec.ToString();

                if (!result.Contains("No such file or directory"))
                {
                    packageToUninstall = result.TrimEnd();
                    break;
                }

                await Task.Delay(5000);
            }

            mainForm.statusText.Text = "CONNECTED";

            var receiver = new ConsoleOutputReceiver(new LoggerFactory().CreateLogger<ConsoleOutputReceiver>());

            if (mainForm.copyLocal.Checked)
            {
                Process proc = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = StaticStuff.ADBPath,
                        Arguments = $"pull /sdcard/Android/data/{packageToUninstall} \"{Directory.GetCurrentDirectory()}\"",
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/obb/{packageToUninstall} /sdcard/Android/obb/{packageToUninstall}_BACKUP", deviceData, receiver);

                adbClient.ExecuteRemoteCommand("pm uninstall " + packageToUninstall, deviceData, receiver);

                proc.StartInfo.Arguments = $"push \"{Directory.GetCurrentDirectory()}\\{packageToUninstall}\" /sdcard/Android/data/";
                proc.Start();
                proc.WaitForExit();
                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/obb/{packageToUninstall}_BACKUP /sdcard/Android/obb/{packageToUninstall}", deviceData, receiver);
                try
                {
                    File.Delete(Path.Combine(Directory.GetCurrentDirectory(), packageToUninstall));
                }
                catch { }
            }
            else
            {
                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/data/{packageToUninstall} /sdcard/Android/data/{packageToUninstall}_BACKUP", deviceData, receiver);
                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/obb/{packageToUninstall} /sdcard/Android/obb/{packageToUninstall}_BACKUP", deviceData, receiver);

                // Barely handles permission conflicts when reinstalling MelonLoader
                adbClient.ExecuteRemoteCommand($"rm -rf /sdcard/Android/data/{packageToUninstall}_BACKUP/cache", deviceData, receiver);
                adbClient.ExecuteRemoteCommand($"rm -rf /sdcard/Android/data/{packageToUninstall}_BACKUP/files/melonloader", deviceData, receiver);
                adbClient.ExecuteRemoteCommand($"rm -rf /sdcard/Android/data/{packageToUninstall}_BACKUP/files/il2cpp", deviceData, receiver);
                adbClient.ExecuteRemoteCommand($"rm /sdcard/Android/data/{packageToUninstall}_BACKUP/files/funchook.log", deviceData, receiver);

                adbClient.ExecuteRemoteCommand("pm uninstall " + packageToUninstall, deviceData, receiver);

                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/data/{packageToUninstall}_BACKUP /sdcard/Android/data/{packageToUninstall}", deviceData, receiver);
                adbClient.ExecuteRemoteCommand($"mv /sdcard/Android/obb/{packageToUninstall}_BACKUP /sdcard/Android/obb/{packageToUninstall}", deviceData, receiver);
            }

            adbClient.ExecuteRemoteCommand($"rm /sdcard/Android/data/com.melonloader.installer/files/temp/adbbridge.txt", deviceData, receiver);

            mainForm.statusText.Text = "COMPLETE+DISCONNECTED";

            Dispose();
        }

        public static void Dispose()
        {
            try
            {
                adbClient?.KillAdb();
            }
            catch { }
            wsClient?.Dispose();
        }
    }
}
