namespace MelonLoader.Installer.App.Utils;

public static class AndroidPermissionHandler
{
    public static bool HaveRequired()
    {
        return HasAccessToAllFiles() && CanInstallUnknownSources();
    }

    public static bool HasAccessToAllFiles()
    {
#if ANDROID30_0_OR_GREATER
#pragma warning disable CA1416 // Validate platform compatibility; I'm clearly already checking it
        return Android.OS.Environment.IsExternalStorageManager;
#pragma warning restore CA1416
#else
        return true;
#endif
    }

    public static bool CanInstallUnknownSources()
    {
#if ANDROID
        return Platform.CurrentActivity!.PackageManager!.CanRequestPackageInstalls();
#else
        return true;
#endif
    }

    public static void TryGetAccessToAllFiles()
    {
#if ANDROID30_0_OR_GREATER
#pragma warning disable CA1416 // Validate platform compatibility; I'm clearly already checking it
        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, Android.Net.Uri.Parse("package:" + Platform.CurrentActivity!.PackageName));
        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
        Platform.CurrentActivity!.StartActivity(intent);
#pragma warning restore CA1416
#endif
    }

    public static void TryGetInstallUnknownSources()
    {
#if ANDROID
        var intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageUnknownAppSources, Android.Net.Uri.Parse("package:" + Platform.CurrentActivity!.PackageName));
        intent.AddFlags(Android.Content.ActivityFlags.NewTask);
        Platform.CurrentActivity!.StartActivity(intent);
#endif
    }
}