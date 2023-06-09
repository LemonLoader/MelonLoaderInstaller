using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace MelonLoaderInstaller.App.Utilities
{
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
                    Logger.Instance.Info("Requesting user confirmation for installation");
                    Intent confirmationIntent = (Intent)intent.GetParcelableExtra(Intent.ExtraIntent);
                    confirmationIntent.AddFlags(ActivityFlags.NewTask);
                    try { StartActivity(confirmationIntent); }
                    catch { }
                    break;
                case PackageInstallStatus.Success:
                    AlertDialog.Builder successDialog = new AlertDialog.Builder(this)
                        .SetTitle("Success")
                        .SetMessage("The app was successfully reinstalled")
                        .SetPositiveButton("Cool", new System.EventHandler<DialogClickEventArgs>((o,b) => { }));

                    successDialog.Show();
                    break;
                default:
                    Logger.Instance.Info(GetErrorString(status));
                    AlertDialog.Builder failDialog = new AlertDialog.Builder(this)
                        .SetTitle("Failed")
                        .SetMessage("The app was unable to be installed\n" + GetErrorString(status))
                        .SetPositiveButton("Cool", new System.EventHandler<DialogClickEventArgs>((o, b) => { }));

                    failDialog.Show();
                    break;
            }

            StopSelf();
            return StartCommandResult.NotSticky;
        }

        public string GetErrorString(PackageInstallStatus status)
        {
            Logger.Instance.Info(status.ToString());
            Logger.Instance.Info(((int)status).ToString());
            return status switch
            {
                PackageInstallStatus.FailureAborted => "Installation was cancelled by user",
                PackageInstallStatus.FailureBlocked => "Installation was blocked by device",
                PackageInstallStatus.FailureConflict => "Unable to install the app because it conflicts with an already installed app with same package name",
                PackageInstallStatus.FailureIncompatible => "Application is incompatible with this device",
                PackageInstallStatus.FailureInvalid => "Invalid APK files selected",
                PackageInstallStatus.FailureStorage => "Not enough storage space to install the app",
                _ => "Installation failed",
            };
        }

        public override IBinder OnBind(Intent intent) => null;
    }
}