using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;
using MelonLoaderInstaller.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Android.Resource;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace MelonLoaderInstaller.App.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller.NoActionBar", MainLauncher = false)]
    public class ViewApplication : AppCompatActivity, View.IOnClickListener
    {
        private UnityApplicationData _applicationData;
        private PatchLogger _patchLogger;
        private APKInstaller _patchApkInstaller;
        private APKInstaller _restoreApkInstaller;
        private bool _patching;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_view_application);

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.toolbar1));
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            string packageName = Intent.GetStringExtra("target.packageName");

            try
            {
                _applicationData = UnityApplicationFinder.FromPackageName(this, packageName);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error("Failed to find target package\n" + ex.ToString());
                Finish();
                return;
            }

            ImageView appIcon = FindViewById<ImageView>(Resource.Id.applicationIcon);
            TextView appName = FindViewById<TextView>(Resource.Id.applicationName);
            Button patchButton = FindViewById<Button>(Resource.Id.patchButton);

            patchButton.SetOnClickListener(this);
            patchButton.Text = _applicationData.IsPatched ? "REPATCH" : "PATCH";

            appIcon.SetImageDrawable(_applicationData.Icon);
            appName.Text = _applicationData.AppName;

            _patchLogger = new PatchLogger(this);
            _patchApkInstaller = new APKInstaller(this, _applicationData.PackageName,
                        () =>
                        {
                            patchButton.Text = "PATCHED";
                            AlertDialog.Builder builder = new AlertDialog.Builder(this)
                                .SetTitle("Completed")
                                .SetMessage("The app was patched successfully.")
                                .SetPositiveButton("OK", (a, b) => { });
                            builder.Show();
                        },
                        () => patchButton.Text = "FAILED",
                        _patchLogger);

            _restoreApkInstaller = new APKInstaller(this, _applicationData.PackageName,
                        () =>
                        {
                            AlertDialog.Builder builder = new AlertDialog.Builder(this)
                                .SetTitle("Completed")
                                .SetMessage("The app was restored successfully.")
                                .SetPositiveButton("OK", (a, b) => { });
                            builder.Show();
                        },
                        () =>
                        {
                            AlertDialog.Builder builder = new AlertDialog.Builder(this)
                                .SetTitle("Failed")
                                .SetMessage("The app could not be stored.")
                                .SetPositiveButton("OK", (a, b) => { });
                            builder.Show();
                        },
                        _patchLogger);

            CheckWarnings(packageName);

            RunOnUiThread(() =>
            {
                _patchApkInstaller.Install("sdcard/Android/data/com.melonloader.installer/files/temp/com.setsnail.daddylonglegs/OutputAPKs");
            });
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.patch_menu, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Id.Home:
                    Finish();
                    return true;
                case Resource.Id.action_patch_local_deps:
                    Intent intent = new Intent()
                        .SetType("application/zip")
                        .SetAction(Intent.ActionGetContent);
                    StartActivityForResult(Intent.CreateChooser(intent, "Select a file"), 123);
                    return true;
                case Resource.Id.action_restore_apk:
                    RestoreAPKs();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            if (_patching)
                return;

            base.OnBackPressed();
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == 123 && resultCode == Result.Ok)
            {
                Uri selectedFile = data.Data;
                Logger.Instance.Verbose("User selected file " + selectedFile.ToString());
                await StartPatching(selectedFile.ToString());
                return;
            }
        }

        private void CheckWarnings(string packageName)
        {
            if (PackageWarnings.AvailableWarnings.TryGetValue(packageName, out string warning))
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("Warning")
                        .SetMessage(warning)
                        .SetIcon(Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();

                new WarningCountdown(3500, 1000, builder, alert).Start();
            }
        }

        public async void OnClick(View v) => await StartPatching("");

        private async Task StartPatching(string unityDepsPath)
        {
            _patching = true;

            _patchLogger.Clear();

            string baseAppPath = GetExternalFilesDir(null).ToString();

            string packageTempPath = Path.Combine(baseAppPath, "temp", _applicationData.PackageName);
            string lemonDataPath = Path.Combine(packageTempPath, "dependencies.zip");
            string il2cppEtcPath = Path.Combine(packageTempPath, "il2cpp_etc.zip");

            string wantedUnityOutPath = Path.Combine(packageTempPath, "unity.zip");
            if (string.IsNullOrEmpty(unityDepsPath))
                unityDepsPath = wantedUnityOutPath;

            Button patchButton = FindViewById<Button>(Resource.Id.patchButton);

            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            patchButton.Enabled = false;
            patchButton.Text = "PATCHING";

            Task task = Task.Run(() =>
            {
                _patchLogger.Log($"Build Directory: [ {baseAppPath} ]");
                _patchLogger.Log("Preparing Assets");

                string outputDir = Path.Combine(packageTempPath, "OutputAPKs");
                string tempDir = Path.Combine(baseAppPath, "temp");

                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                if (Directory.Exists(packageTempPath))
                    Directory.Delete(packageTempPath, true);

                Directory.CreateDirectory(packageTempPath);
                Directory.CreateDirectory(outputDir);

                unityDepsPath = TryCopyDocument(unityDepsPath, wantedUnityOutPath);

#if DEBUG
                bool localFile = true;
#else
                bool localFile = false;
#endif

                if (localFile && Assets.List("").Any(a => a.Contains("installer_deps")))
                {
                    _patchLogger.Log("Using embedded installer dependencies");
                    CopyAsset("installer_deps.zip", lemonDataPath);
                }
                else
                {
                    bool downloadResult = DependencyDownloader.Run(lemonDataPath, _patchLogger);
                    if (!downloadResult)
                        RunOnUiThread(SetFailed);
                }

                _patchLogger.Log("Writing il2cpp_etc to file");
                CopyAsset("il2cpp_etc.zip", il2cppEtcPath);

                // If it's patched, backing up has basically has no reason
                if (!_applicationData.IsPatched)
                {
                    _patchLogger.Log("Backing up APKs");

                    string baseBackupPath = Path.Combine(baseAppPath, "Backups");

                    if (!Directory.Exists(baseBackupPath))
                        Directory.CreateDirectory(baseBackupPath);

                    string backupPath = Path.Combine(baseBackupPath, _applicationData.PackageName);

                    if (!Directory.Exists(backupPath))
                        Directory.CreateDirectory(backupPath);

                    BackupAPK(_applicationData.ApkLocation, backupPath);

                    if (_applicationData.SplitLibApkLocation != null)
                        BackupAPK(_applicationData.SplitLibApkLocation, backupPath);

                    if (_applicationData.ExtraSplitApkLocations != null)
                        foreach (string split in _applicationData.ExtraSplitApkLocations)
                            BackupAPK(split, backupPath);
                }
                else
                    _patchLogger.Log("App was previously patched, skipping back up");

                _patchLogger.Log("Starting patch");

                Patcher patcher = new Patcher(new PatchArguments()
                {
                    TargetApkPath = _applicationData.ApkLocation,
                    LibraryApkPath = _applicationData.SplitLibApkLocation,
                    ExtraSplitApkPaths = _applicationData.ExtraSplitApkLocations,
                    IsSplit = _applicationData.IsSplit,
                    OutputApkDirectory = outputDir,
                    TempDirectory = packageTempPath,
                    LemonDataPath = lemonDataPath,
                    Il2CppEtcPath = il2cppEtcPath,
                    UnityDependenciesPath = unityDepsPath,
                    UnityVersion = _applicationData.EngineVersion,
                    PackageName = _applicationData.PackageName,
                }, _patchLogger);

                bool success = patcher.Run();

                if (!success)
                {
                    RunOnUiThread(SetFailed);
                    return;
                }

                _patchLogger.Log("Application patched successfully, reinstalling.");

                RunOnUiThread(() =>
                {
                    _patchApkInstaller.Install(outputDir);
                });
            });

            await task;
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            _patching = false;
        }

        private string TryCopyDocument(string from, string to)
        {
            if (from.StartsWith("content://"))
            {
                Stream inStream = ContentResolver.OpenInputStream(Uri.Parse(from))
                    ?? throw new Exception("Unity assets path does not exist!");

                Stream outStream = File.OpenWrite(to);

                inStream.CopyTo(outStream);

                inStream.Dispose();
                outStream.Dispose();

                Logger.Instance.Info($"Copied unity assets to [ {to} ]");
                return to;
            }
            else
                return from;
        }

        private bool CopyAsset(string assetName, string destinationPath)
        {
            try
            {
                Stream inStream = Assets.Open(assetName);
                Stream outStream = File.OpenWrite(destinationPath);

                byte[] buffer = new byte[4096];
                int bytesRead;
                while ((bytesRead = inStream.Read(buffer, 0, buffer.Length)) > 0)
                    outStream.Write(buffer, 0, bytesRead);

                inStream.Dispose();
                outStream.Dispose();

                return true;
            }
            catch (Exception ex)
            {
                _patchLogger.Log("Failed to copy asset file: " + assetName + " -> " + ex.ToString());
                return false;
            }
        }

        private void BackupAPK(string apkPath, string backupDir)
        {
            string backupPath = Path.Combine(backupDir, Path.GetFileName(apkPath));
            File.Copy(apkPath, backupPath, true);
        }
        
        private void RestoreAPKs()
        {
            string baseAppPath = GetExternalFilesDir(null).ToString();
            string baseBackupPath = Path.Combine(baseAppPath, "Backups");
            string backupPath = Path.Combine(baseBackupPath, _applicationData.PackageName);

            if (!Directory.Exists(backupPath) || Directory.GetFiles(backupPath, "*.apk").Length == 0)
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("Cannot restore")
                        .SetMessage("No backups are available for this package.")
                        .SetPositiveButton("OK", (a, b) => { })
                        .SetIcon(Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.Show();

                return;
            }

            _restoreApkInstaller.Install(backupPath);
        }

        private void SetFailed()
        {
            Button patchButton = FindViewById<Button>(Resource.Id.patchButton);
            patchButton.Text = "FAILED";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
        }

        public class PatchLogger : IPatchLogger
        {
            private Activity _context;
            private TextView _content;
            private ScrollView _scroller;
            private bool _dirty = false;

            public PatchLogger(Activity context)
            {
                _context = context;
                _content = context.FindViewById<TextView>(Resource.Id.loggerBody);
                _scroller = context.FindViewById<ScrollView>(Resource.Id.loggerScroll);
                _content.Text = string.Empty;
            }

            public void Clear()
            {
                _context.RunOnUiThread(() =>
                {
                    _content.Text = string.Empty;
                });
            }

            public void Log(string message)
            {
                Logger.Instance.Info(message);

                _context.RunOnUiThread(() =>
                {
                    if (_dirty)
                        _content.Append("\n");
                    else
                        _dirty = true;

                    _content.Append(message);
                    _scroller.FullScroll(FocusSearchDirection.Down);
                });
            }
        }
    }
}