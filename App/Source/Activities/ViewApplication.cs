using Android.App;
using Android.OS;
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

            CheckWarnings(packageName);
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
                case Resource.Id.home:
                    Finish();
                    return true;
                case Resource.Id.action_patch_local_deps:
                    // TODO: patch with local deps
                    // requires patching to work obviously
                    return true;
            }

            return base.OnOptionsItemSelected(item);
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

        public async void OnClick(View v) => await StartPatching(Path.Combine(GetExternalFilesDir(null).ToString(), "temp", "unity.zip"));

        private async Task StartPatching(string unityDepsPath)
        {
            _patchLogger.Clear();

            string baseAppPath = GetExternalFilesDir(null).ToString();

            string packageTempPath = Path.Combine(baseAppPath, "temp", _applicationData.PackageName);
            string lemonDataPath = Path.Combine(baseAppPath, "temp", "dependencies.zip");
            string il2cppEtcPath = Path.Combine(baseAppPath, "temp", "il2cpp_etc.zip");
            string unityAssetsPath = unityDepsPath;

            // File selection stuff :P
            if (unityAssetsPath.StartsWith("content://"))
            {
                Stream inStream = ContentResolver.OpenInputStream(Uri.Parse(unityAssetsPath))
                    ?? throw new Exception("Unity assets path does not exist!");

                string outPath = Path.Combine(GetExternalFilesDir(null).ToString(), "temp", "unity.zip");
                Stream outStream = ContentResolver.OpenOutputStream(Uri.FromFile(new Java.IO.File(outPath)), "w");

                inStream.CopyTo(outStream);

                inStream.Dispose();
                outStream.Dispose();

                unityAssetsPath = outPath;
                Logger.Instance.Info($"Copied unity assets to [ {unityAssetsPath} ]");
            }

            // TODO: is the PublishedBase stuff needed?

            Button patchButton = FindViewById<Button>(Resource.Id.patchButton);

            SupportActionBar.SetDisplayHomeAsUpEnabled(false);
            patchButton.Enabled = false;
            patchButton.Text = "PATCHING";

            Task task = Task.Run(() =>
            {
                if (Directory.Exists(packageTempPath))
                    Directory.Delete(packageTempPath, true);

                _patchLogger.Log($"Build Directory: [ {baseAppPath} ]");
                _patchLogger.Log("Preparing Assets");

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
                        RunOnUiThread(() => { patchButton.Text = "FAILED"; });
                }

                CopyAsset("il2cpp_etc.zip", il2cppEtcPath);

                _patchLogger.Log("Starting patch");

                Directory.CreateDirectory(packageTempPath);

                string outputDir = Path.Combine(packageTempPath, "OutputAPKs");
                // TODO: for apks with extra split apks, we will want to pass them into the patcher so they can be signed with the same key as the others

                Patcher patcher = new Patcher(new PatchArguments()
                {
                    TargetApkPath = _applicationData.ApkLocation,
                    LibraryApkPath = _applicationData.SplitLibAPKLocation,
                    IsSplit = _applicationData.IsSplit,
                    OutputApkDirectory = outputDir,
                    TempDirectory = packageTempPath,
                    LemonDataPath = lemonDataPath,
                    Il2CppEtcPath = il2cppEtcPath,
                    UnityDependenciesPath = unityAssetsPath,
                    UnityVersion = _applicationData.EngineVersion
                }, _patchLogger);

                bool success = patcher.Run();

                if (success)
                {
                    _patchLogger.Log("Application patched successfully, reinstalling.");

                    RunOnUiThread(() => 
                    {
                        // TODO: install
                        // since I'm moving to a "pass in dir instead of base.apk path", I should just be able to check
                        // if file.count > 1: split, else: single
                    });
                }
                else
                    RunOnUiThread(() => { patchButton.Text = "FAILED"; });
            });

            await task;
        }

        private bool CopyAsset(string assetName, string destinationPath)
        {
            try
            {
                Stream inStream = Assets.Open(assetName);
                Stream outStream = File.OpenWrite(destinationPath);

                byte[] buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = inStream.Read(buffer)) != -1)
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