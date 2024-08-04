#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
#endif

using MelonLoader.Installer.Core;

namespace MelonLoader.Installer.App.Utils;

public class APKInstaller
{
    public static APKInstaller Current { get; private set; }

    private UnityApplicationFinder.Data _data;

    private IPatchLogger _logger;
    
    private string? _apkDirectory;

    private Action? _next;
    private Action _onInstallFail;
    private Action _afterInstall;

#if ANDROID
    private string _pending;
    private int _installLoopCount;
    private int _uninstallLoopCount;
#endif

    public APKInstaller(UnityApplicationFinder.Data data, IPatchLogger logger, Action afterInstall, Action onInstallFail)
    {
        Current = this;

        _data = data;
        _logger = logger;
        _afterInstall = afterInstall;
        _onInstallFail = onInstallFail;

#if ANDROID
        var activity = (AndroidX.Activity.ComponentActivity)Platform.CurrentActivity!;

        _pending = "";
        _installLoopCount = 0;
        _uninstallLoopCount = 0;
#endif
    }

    public async Task Install(string apkDirectory)
    {
        _apkDirectory = apkDirectory;

        _next = async () => await InternalInstall();
        await UninstallPackage();
    }

    private async Task InternalInstall()
    {
        _logger.Log("Beginning install");

        string[] apks = Directory.GetFiles(_apkDirectory!, "*.apk");
        if (apks.Length > 1)
        {
            await InternalInstall_Split(apks);
            return;
        }

        await InternalInstall_Single(apks[0]);
    }

    private async Task InternalInstall_Single(string apk)
    {
#if ANDROID
        Android.Net.Uri fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(Platform.CurrentActivity!, Platform.CurrentActivity!.PackageName + ".provider", new Java.IO.File(apk))!;

        _pending = Intent.ActionInstallPackage;
        Intent install = new(_pending);
        install.SetDataAndType(fileUri, "application/vnd.android.package-archive");

        install.SetFlags(ActivityFlags.GrantReadUriPermission);
        install.PutExtra(Intent.ExtraReturnResult, true);

        try
        {
            MainActivity.ActivityResultLauncher.Launch(install);
        }
        catch (ActivityNotFoundException ex)
        {
            _logger.Log($"Error in opening file.\n{ex}");
        }

        await Task.Delay(50);
#else
        await ADBManager.InstallAPK(apk);
        _afterInstall.Invoke();
#endif
    }

    private async Task InternalInstall_Split(string[] apks)
    {
#if ANDROID
        PackageInstaller packageInstaller = Platform.CurrentActivity!.PackageManager!.PackageInstaller;
        try
        {
            PackageInstaller.SessionParams param = new(PackageInstallMode.FullInstall);
            param.SetInstallReason(PackageInstallReason.User);

            int sessionId = packageInstaller.CreateSession(param);
            PackageInstaller.Session session = packageInstaller.OpenSession(sessionId);

            for (int i = 0; i < apks.Length; i++)
            {
                string apk = apks[i];
                using FileStream apkStream = new(apk, FileMode.Open);
                using Stream outStream = session.OpenWrite($"{i + 1}.apk", 0, apkStream.Length);
                apkStream.CopyTo(outStream);
                session.Fsync(outStream);
            }

            Intent callbackIntent = new(Platform.CurrentActivity!, typeof(Platforms.Android.SplitAPKService));
            PendingIntent pending = PendingIntent.GetService(Platform.CurrentActivity!, 0, callbackIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
            session.Commit(pending.IntentSender);
        }
        catch (Exception e)
        {
            _logger.Log($"Failed to install split APKs\n{e}");
        }

        await Task.Delay(50);
#else
        await ADBManager.InstallMultipleAPKs(apks.First(a => a.EndsWith("base.apk")), apks.Where(a => !a.EndsWith("base.apk")).ToArray());
        _afterInstall.Invoke();
#endif
    }

    private async Task UninstallPackage()
    {
        _logger.Log("Beginning uninstall");

#if ANDROID
        _pending = Intent.ActionDelete;
        Intent intent = new(_pending);
        intent.SetData(Android.Net.Uri.Parse("package:" + _data.PackageName));
        MainActivity.ActivityResultLauncher.Launch(intent);
        await Task.Delay(50);
#else
        await ADBManager.UninstallPackage(_data.PackageName);
        _next!.Invoke();
#endif
    }

#if ANDROID
    private async Task CheckPackageUninstalled()
    {
        if (IsPackageInstalled())
        {
            if (_uninstallLoopCount >= 3)
            {
                _onInstallFail?.Invoke();
                return;
            }

            _logger.Log("Package was not uninstalled, re-attempting");
            _uninstallLoopCount++;
            await UninstallPackage();

            return;
        }

        _logger.Log("Uninstall successful");
        _next!.Invoke();
    }

    public async Task CheckPackageInstalled()
    {
        if (!IsPackageInstalled())
        {
            if (_installLoopCount >= 3)
            {
                _onInstallFail?.Invoke();
                return;
            }

            _logger.Log("Package was not installed, re-attempting");
            _installLoopCount++;
            await InternalInstall();

            return;
        }

        _afterInstall.Invoke();
    }

    private bool IsPackageInstalled()
    {
        try
        {
            Platform.CurrentActivity!.PackageManager!.GetPackageInfo(_data.PackageName, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }
#endif

#if ANDROID
    public class APKInstallerCallback : Java.Lang.Object, IActivityResultCallback
    {
        public async void OnActivityResult(Result resultCode, Intent data)
        {
            switch (Current._pending)
            {
                case Intent.ActionDelete:
                    await Current.CheckPackageUninstalled();
                    break;
                case Intent.ActionInstallPackage:
                    await Current.CheckPackageInstalled();
                    break;
            }
        }

        public void OnActivityResult(Java.Lang.Object? result)
        {
            if (result != null && result is ActivityResult activityResult)
                OnActivityResult((Result)activityResult.ResultCode, activityResult.Data!);
        }
    }
#endif
}