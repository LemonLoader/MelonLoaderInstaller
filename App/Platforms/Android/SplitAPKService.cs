﻿using Android.App;
using Android.Content.PM;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Debug = System.Diagnostics.Debug;
using MelonLoader.Installer.App.Utils;

namespace MelonLoader.Installer.App.Platforms.Android;

[Service(Exported = true)]
internal class SplitAPKService : Service
{
    [return: GeneratedEnum]
    public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
    {
        PackageInstallStatus status = (PackageInstallStatus)intent.GetIntExtra(PackageInstaller.ExtraStatus, -999);

        switch (status)
        {
            case PackageInstallStatus.PendingUserAction:
                Debug.WriteLine("Requesting user confirmation for installation");
                Intent confirmationIntent = (Intent)intent.GetParcelableExtra(Intent.ExtraIntent)!;
                confirmationIntent.AddFlags(ActivityFlags.NewTask);
                try { StartActivity(confirmationIntent); }
                catch { }
                break;
            default:
                APKInstaller.Current.CheckPackageInstalled();
                break;
        }

        StopSelf();
        return StartCommandResult.NotSticky;
    }

    public override IBinder OnBind(Intent intent) => null;
}