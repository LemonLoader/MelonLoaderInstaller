using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public static ActivityResultLauncher ActivityResultLauncher => _activityResultLauncher;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor; by the time these are used, they will have to be assigned or something has gone very, very wrong
    private static ActivityResultLauncher _activityResultLauncher;
    private static APKInstaller.APKInstallerCallback _installerCallback;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
    public MainActivity()
    {
        _installerCallback = new();
        _activityResultLauncher = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), _installerCallback);
    }

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
