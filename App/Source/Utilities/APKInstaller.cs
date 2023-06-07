using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.DocumentFile.Provider;
using System;
using System.IO;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Uri = Android.Net.Uri;

namespace MelonLoaderInstaller.App.Utilities
{
    public class APKInstaller
    {
        private Activity _context;
        private string _packageName;
        private string _lastInstallPath;

        private int _installLoopCount;

        private string _pending;
        private Action _next;

        private Action _onInstallFail;
        private Action _afterInstall;

        private DataInfo _dataInfo;
        private ActivityResultLauncher _activityResultLauncher;

        public APKInstaller(AppCompatActivity context, string packageName, Action afterInstall, Action onInstallFail)
        {
            _context = context;
            _packageName = packageName;
            _afterInstall = afterInstall;
            _onInstallFail = onInstallFail;
            _installLoopCount = 0;
            _activityResultLauncher = context.RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), new APKInstallerCallback(this));
        }

        public void Install(string apkDirectory)
        {
            _next = () => InternalInstall(apkDirectory);
            UninstallPackage();
        }

        private void InternalInstall(string apkDirectory)
        {
            _lastInstallPath = apkDirectory;

            string[] apks = Directory.GetFiles(apkDirectory, "*.apk");
            if (apks.Length > 1)
            {
                // TODO: split install
                Fail();
                return;
            }

            _context.RunOnUiThread(() =>
            {
                Uri fileUri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", new Java.IO.File(apks[0]));

                _pending = Intent.ActionView;
                Intent install = new Intent(_pending);
                install.SetDataAndType(fileUri, "application/vnd.android.package-archive");

                install.SetFlags(ActivityFlags.NewTask);
                install.SetFlags(ActivityFlags.GrantReadUriPermission);

                try
                {
                    _activityResultLauncher.Launch(install);
                }
                catch (ActivityNotFoundException ex)
                {
                    Logger.Instance.Error($"Error in opening file.\n{ex}");
                }
            });
        }

        private void UninstallPackage(Action nxt = null)
        {
            if (nxt != null)
                _next = nxt;

            AlertDialog.Builder builder = new AlertDialog.Builder(_context);
            builder
                .SetTitle("ADB Bridge")
                .SetMessage("Do you want to use the Lemon ADB Bridge® to save game data and OBBs, if they exist?")
                .SetPositiveButton("Yes", (o, i) => HandleBridge())
                .SetNegativeButton("No", (o, i) => _context.RunOnUiThread(HandleStandard))
                .SetIcon(Android.Resource.Drawable.IcDialogAlert);

            AlertDialog alert = builder.Create();
            alert.SetCancelable(false);
            alert.Show();
        }

        private void HandleStandard()
        {
            string selfBaseDir = _context.GetExternalFilesDir(null).ToString();

            _dataInfo = new DataInfo()
            {
                ShouldMoveBack = false,
                DataPath = $"/sdcard/Android/data/{_packageName}/",
                ObbPath = $"/sdcard/Android/obb/{_packageName}/",
                NewDataPath = Path.Combine(selfBaseDir, "data_backup"),
                NewObbPath = Path.Combine(selfBaseDir, "obb_backup"),
            };

            try
            {
                // Before /Android protections
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    Directory.Move(_dataInfo.DataPath, _dataInfo.NewDataPath);
                    Directory.Move(_dataInfo.ObbPath, _dataInfo.NewObbPath);
                }
                // The DocumentFile era
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    _dataInfo.DataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.DataPath);
                    _dataInfo.NewDataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewDataPath);

                    _dataInfo.ObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.ObbPath);
                    _dataInfo.NewObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewObbPath);

                    DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.DataDF.Uri, _dataInfo.DataDF.ParentFile.Uri, _dataInfo.NewDataDF.Uri);
                    DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.ObbDF.Uri, _dataInfo.ObbDF.ParentFile.Uri, _dataInfo.NewObbDF.Uri);
                }
                else
                    throw new Exception("In-app backups are unsupported past API 32.");

                _dataInfo.ShouldMoveBack = true;

                _pending = Intent.ActionDelete;
                Intent intent = new Intent(_pending);
                intent.SetData(Uri.Parse("package:" + _packageName));
                _activityResultLauncher.Launch(intent);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.ToString());

                AlertDialog.Builder builder = new AlertDialog.Builder(_context);
                builder
                    .SetTitle("Failed to save Data!")
                    .SetMessage("Failed to save any data stored in Android/data or Android/obb. This could break some games. Do you want to continue?")
                    .SetPositiveButton("Yes", (o, i) => {
                        _pending = Intent.ActionDelete;
                        Intent intent = new Intent(_pending);
                        intent.SetData(Uri.Parse("package:" + _packageName));
                        _activityResultLauncher.Launch(intent);
                    })
                    .SetNegativeButton("No", (o, i) => Fail())
                    .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();
            }
        }

        private void HandleBridge()
        {
            // TODO: adbbridge
        }

        private void TryFileMoveBack()
        {
            if (!_dataInfo.ShouldMoveBack)
                return;

            try
            {
                // Before /Android protections
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    Directory.Move(_dataInfo.DataPath, _dataInfo.NewDataPath);
                    Directory.Move(_dataInfo.ObbPath, _dataInfo.NewObbPath);
                }
                // The DocumentFile era
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    _dataInfo.DataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.DataPath);
                    _dataInfo.NewDataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewDataPath);

                    _dataInfo.ObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.ObbPath);
                    _dataInfo.NewObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewObbPath);

                    DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.DataDF.Uri, _dataInfo.DataDF.ParentFile.Uri, _dataInfo.NewDataDF.Uri);
                    DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.ObbDF.Uri, _dataInfo.ObbDF.ParentFile.Uri, _dataInfo.NewObbDF.Uri);
                }
                else
                    throw new Exception("In-app backups are unsupported past API 32.");

                _dataInfo.ShouldMoveBack = true;

                _pending = Intent.ActionDelete;
                Intent intent = new Intent(_pending);
                intent.SetData(Android.Net.Uri.Parse("package:" + _packageName));
                _activityResultLauncher.Launch(intent);
            }
            catch (Exception ex)
            {
                
            }
        }

        private void PostInstallAttempt()
        {
            if (!IsPackageInstalled())
            {
                if (_installLoopCount >= 3)
                {
                    Fail();
                    return;
                }

                Logger.Instance.Info("Package was not installed, re-attempting installation");
                _installLoopCount++;
                InternalInstall(_lastInstallPath);

                return;
            }

            _afterInstall?.Invoke();
        }

        private bool IsPackageInstalled()
        {
            try
            {
                _context.PackageManager.GetPackageInfo(_packageName, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void Fail() => _onInstallFail?.Invoke();

        // This is not very clean but /shrug
        private class DataInfo
        {
            public bool ShouldMoveBack;

            public string DataPath;
            public DocumentFile DataDF;

            public string ObbPath;
            public DocumentFile ObbDF;

            public string NewDataPath;
            public DocumentFile NewDataDF;

            public string NewObbPath;
            public DocumentFile NewObbDF;
        }

        public class APKInstallerCallback : Java.Lang.Object, IActivityResultCallback
        {
            private APKInstaller _parent;

            public APKInstallerCallback(APKInstaller parent)
            {
                _parent = parent;
            }

            public void OnActivityResult(Result resultCode, Intent data)
            {
                switch (_parent._pending)
                {
                    case Intent.ActionDelete:
                        _parent.TryFileMoveBack();
                        _parent._next?.Invoke();
                        break;
                    case Intent.ActionView:
                        _parent.PostInstallAttempt();
                        break;
                }

                _parent._pending = string.Empty;
            }

            public void OnActivityResult(Java.Lang.Object result)
            {
                if (result is ActivityResult activityResult)
                    OnActivityResult((Result)activityResult.ResultCode, activityResult.Data);
            }
        }
    }
}