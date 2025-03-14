using Android.App;
using Android.Content.PM;
using Android.Runtime;
using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (grantResults.Length > 0 && grantResults.Any(a => a != Permission.Granted))
        {
            await PopupHelper.Toast("At least one permission was not granted.");
        }
    }
}
