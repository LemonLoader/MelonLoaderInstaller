using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AndroidX.DocumentFile.Provider;
using System;
using System.IO;
using System.Threading;
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
        private APKInstallerCallback _installerCallback;
        private ActivityResultLauncher _activityResultLauncher;

        private Activities.ViewApplication.PatchLogger _logger;

        public APKInstaller(AppCompatActivity context, string packageName, Action afterInstall, Action onInstallFail, Activities.ViewApplication.PatchLogger logger)
        {
            _context = context;
            _packageName = packageName;
            _afterInstall = afterInstall;
            _onInstallFail = onInstallFail;
            _logger = logger;
            _installLoopCount = 0;
            _installerCallback = new APKInstallerCallback(this);
            _activityResultLauncher = context.RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), _installerCallback);
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
                InternalInstall_Split(apks);
                return;
            }

            InternalInstall_Single(apks[0]);
        }

        private void InternalInstall_Single(string apk)
        {
            _context.RunOnUiThread(() =>
            {
                Uri fileUri = FileProvider.GetUriForFile(_context, _context.PackageName + ".provider", new Java.IO.File(apk));

                _pending = Intent.ActionInstallPackage;
                Intent install = new Intent(_pending);
                install.SetDataAndType(fileUri, "application/vnd.android.package-archive");

                install.SetFlags(ActivityFlags.GrantReadUriPermission);
                install.PutExtra(Intent.ExtraReturnResult, true);

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

        private void InternalInstall_Split(string[] apks)
        {
            PackageInstaller packageInstaller = _context.PackageManager.PackageInstaller;
            try
            {
                PackageInstaller.SessionParams param = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
                param.SetInstallReason(PackageInstallReason.User);

                int sessionId = packageInstaller.CreateSession(param);
                PackageInstaller.Session session = packageInstaller.OpenSession(sessionId);

                for (int i = 0; i < apks.Length; i++)
                {
                    string apk = apks[i];
                    using FileStream apkStream = new FileStream(apk, FileMode.Open);
                    using Stream outStream = session.OpenWrite($"{i + 1}.apk", 0, apkStream.Length);
                    apkStream.CopyTo(outStream);
                    session.Fsync(outStream);
                }

                Intent callbackIntent = new Intent(_context, typeof(SplitAPKService));
                PendingIntent pending = PendingIntent.GetService(_context, 0, callbackIntent, PendingIntentFlags.Mutable);
                session.Commit(pending.IntentSender);
            }
            catch (Exception e)
            {
                Logger.Instance.Error($"Failed to install split APKs\n{e}");
            }
        }

        private void UninstallPackage(Action nxt = null)
        {
            if (nxt != null)
                _next = nxt;

            _context.RunOnUiThread(HandleStandard);
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
                NewObbPath = Path.Combine($"/sdcard/Android/obb/{_context.PackageName}/"),
            };

            if (!Directory.Exists(_dataInfo.NewDataPath))
                Directory.CreateDirectory(_dataInfo.NewDataPath);

            try
            {
                // Before /Android protections
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    Directory.Move(_dataInfo.DataPath, Path.Combine(_dataInfo.NewDataPath, GetDirectoryName(_dataInfo.DataPath)));
                    Directory.Move(_dataInfo.ObbPath, Path.Combine(_dataInfo.NewObbPath, GetDirectoryName(_dataInfo.ObbPath)));
                }
                // The DocumentFile era
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    _dataInfo.DataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.DataPath);
                    _dataInfo.NewDataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewDataPath);

                    _dataInfo.ObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.ObbPath);
                    _dataInfo.NewObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.NewObbPath);

                    if (!_dataInfo.NewObbDF.Exists())
                        _dataInfo.NewObbDF.ParentFile.CreateDirectory(_context.PackageName);

                    DocumentFile newDataPackage = _dataInfo.NewDataDF.FindFile(_packageName);
                    if (newDataPackage?.Exists() ?? false)
                        newDataPackage.Delete();
                    DocumentFile newObbPackage = _dataInfo.NewObbDF.FindFile(_packageName);
                    if (newObbPackage?.Exists() ?? false)
                        newObbPackage.Delete();

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

        private void TryFileMoveBack()
        {
            if (_dataInfo == null || !_dataInfo.ShouldMoveBack)
                return;

            try
            {
                // Before /Android protections
                if (Build.VERSION.SdkInt <= BuildVersionCodes.Q)
                {
                    Directory.Move(Path.Combine(_dataInfo.NewDataPath, GetDirectoryName(_dataInfo.DataPath)), _dataInfo.DataPath);
                    Directory.Move(Path.Combine(_dataInfo.NewObbPath, GetDirectoryName(_dataInfo.ObbPath)), _dataInfo.ObbPath);
                    _next?.Invoke();
                }
                // The DocumentFile era
                else if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2)
                {
                    int pkgIdx = _dataInfo.DataPath.IndexOf(_packageName);
                    _dataInfo.DataDF = FolderPermission.GetAccessToFile(_context, _dataInfo.DataPath);
                    _dataInfo.NewDataDF = FolderPermission.GetAccessToFile(_context, Path.Combine(_dataInfo.NewDataPath, GetDirectoryName(_dataInfo.DataPath)));

                    pkgIdx = _dataInfo.ObbPath.IndexOf(_packageName);
                    _dataInfo.ObbDF = FolderPermission.GetAccessToFile(_context, _dataInfo.ObbPath[..pkgIdx]);
                    _dataInfo.NewObbDF = FolderPermission.GetAccessToFile(_context, Path.Combine(_dataInfo.NewObbPath, GetDirectoryName(_dataInfo.ObbPath)));

                    // We can't use MoveDocument since it causes permission issues, instead we move stuff using streams
                    //DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.NewDataDF.Uri, _dataInfo.NewDataDF.ParentFile.Uri, _dataInfo.DataDF.Uri);

                    ProgressDialog restoreDialog = ProgressDialog.Show(_context, "Please wait", "Restoring data, this can take a few");
                    restoreDialog.SetCancelable(false);

                    new Thread(() =>
                    {
                        RestoreFolder(_dataInfo.DataDF, _dataInfo.NewDataDF);
                        if (_dataInfo.NewDataDF.Exists())
                            _dataInfo.NewDataDF.Delete();
                        restoreDialog.Dismiss();
                        _next?.Invoke();
                    }).Start();
                    DocumentsContract.MoveDocument(_context.ContentResolver, _dataInfo.NewObbDF.Uri, _dataInfo.NewObbDF.ParentFile.Uri, _dataInfo.ObbDF.Uri);
                }
                else
                    throw new Exception("In-app backups are unsupported past API 32.");
            }
            catch (Exception ex)
            {
                Logger.Instance.Info($"Restore failed\n{ex}");
                Toast.MakeText(_context, "Failed to restore data, check for data folders in Android/[obb][data]/" + _context.PackageName + "/!", ToastLength.Long).Show();
                _next?.Invoke();
            }
        }

        private void RestoreFolder(DocumentFile destinationFolder, DocumentFile sourceFolder)
        {
            if (!destinationFolder.Exists())
            {
                destinationFolder.ParentFile.CreateDirectory(destinationFolder.Name);
            }

            DocumentFile[] files = sourceFolder.ListFiles();
            foreach (DocumentFile file in files)
            {
                if (file.IsDirectory)
                {
                    // Recursively restore subfolders
                    DocumentFile destinationSubfolder = destinationFolder.CreateDirectory(file.Name);
                    RestoreFolder(destinationSubfolder, file);
                }
                else
                {
                    // Restore individual file
                    _logger.Log("Restoring " + file.Name);
                    DocumentFile destinationFile = destinationFolder.CreateFile(file.Type, file.Name);
                    using Stream inputStream = _context.ContentResolver.OpenInputStream(file.Uri);
                    using Stream outputStream = _context.ContentResolver.OpenOutputStream(destinationFile.Uri);
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                }

                file.Delete();
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

        private string GetDirectoryName(string path) => Path.GetFileName(Path.GetDirectoryName(path));

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
                        break;
                    case Intent.ActionInstallPackage:
                        _parent.PostInstallAttempt();
                        break;
                }
            }

            public void OnActivityResult(Java.Lang.Object result)
            {
                if (result is ActivityResult activityResult)
                    OnActivityResult((Result)activityResult.ResultCode, activityResult.Data);
            }
        }
    }
}