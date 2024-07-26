using System.Net;

namespace MelonLoader.Installer.App.Utils;

public static class PackageWarningManager
{
    public static Dictionary<string, string> AvailableWarnings { get; private set; } = [];

    public static async Task Retrieve()
    {
        using HttpClient client = new();
        string rawWarnings = "";
        try { rawWarnings = await client.GetStringAsync("https://raw.githubusercontent.com/LemonLoader/MelonLoaderInstaller/master/package_warnings.json"); }
        catch (WebException)
        {
            await PopupHelper.Alert("Unable to connect to GitHub! Please check your connection and try again.", "Connection Error", "Exit");
            Environment.Exit(0);
            return;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await PopupHelper.Alert($"Connection failed ({ex.Message}), please try again later.", "Connection Error", "Exit");
            Environment.Exit(0);
            return;
        }

        AvailableWarnings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(rawWarnings)!;
    }
}
