#if ANDROID
using Android;
using Android.Content.PM;
#endif

namespace MelonLoader.Installer.App.Utils;

public static class AndroidPermissionHandler
{
    public static void Check()
    {
        // TODO: everything
#if ANDROID
        bool canRead = Platform.CurrentActivity!.CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted;
        bool canWrite = Platform.CurrentActivity!.CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted;

        if (!canRead || !canWrite)
        {
            Platform.CurrentActivity!.RequestPermissions(
            [
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage,
            ], 100);
        }

        if (!Platform.CurrentActivity!.PackageManager!.CanRequestPackageInstalls())
            RequestInstallUnknownSources();
#endif
    }

#if ANDROID
    public static void RequestInstallUnknownSources()
    {
        /*AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder
                .SetTitle("Install Permission")
                .SetMessage("Lemon needs permission to install apps from unknown sources to function!")
                .SetPositiveButton("Setup", (o, di) => StartActivity(new Intent(Android.Provider.Settings.ActionManageUnknownAppSources, Uri.Parse("package:" + PackageName))))
                .SetIcon(Android.Resource.Drawable.IcDialogAlert);

        AlertDialog alert = builder.Create();
        alert.SetCancelable(false);
        alert.Show();*/
    }

    public static void RequestManageAllFiles()
    {
        /*AlertDialog.Builder builder = new AlertDialog.Builder(this);
        builder
                .SetTitle("Storage Permission")
                .SetMessage("Lemon needs permission to manage all files to function!")
                .SetPositiveButton("Setup", (o, di) => StartActivity(new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, Uri.Parse("package:" + PackageName))))
                .SetIcon(Android.Resource.Drawable.IcDialogAlert);

        AlertDialog alert = builder.Create();
        alert.SetCancelable(false);
        alert.Show();*/
    }
#endif
}
