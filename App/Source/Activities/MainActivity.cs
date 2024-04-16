using System.Collections.Generic;
using System.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using IO.Rayshift.Translatefgo;
using MelonLoaderInstaller.App.Adapters;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;
using Rikka.Shizuku;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MelonLoaderInstaller.App.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, AdapterView.IOnItemClickListener
    {
        public static bool ShizukuBound => NextGenFS.Binder != null;
        public static NextGenFSServiceConnection NextGenFS = new NextGenFSServiceConnection();
        public static ShizukuPermissionResultListener ShizukuListener = new ShizukuPermissionResultListener();

        private List<UnityApplicationData> _availableApps;
        private Toast _unsupportedToast;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Logger.SetupMainInstance("melonloader");

            PackageWarnings.Run(this);

            _availableApps = UnityApplicationFinder.Find(this);
            ApplicationsAdapter adapter = new ApplicationsAdapter(this, _availableApps);

            ListView listView = FindViewById<ListView>(Resource.Id.application_list);
            listView.Adapter = adapter;
            listView.OnItemClickListener = this;

            FolderPermission.CurrentContext = this;
            FolderPermission.l = RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), new FolderPermissionCallback());

            TryRequestPermissions();

            var shizukuActive = Shizuku.PingBinder();

            if (shizukuActive)
            {
                Shizuku.AddRequestPermissionResultListener(ShizukuListener);
                ShizukuProvider.EnableMultiProcessSupport(true);
                Logger.Instance.Info("Shizuku");

                GetShizukuPermission();
            }
            else
            {
                Logger.Instance.Info("No Shizuku");
                // TODO: I feel like this mess of AlertDialogs on first start is a messy gross mess and should be cleaned or something
                RequestFolderPermissions();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (grantResults.Length > 0 && grantResults.Any(a => a != Permission.Granted))
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("Permissions Issue")
                        .SetMessage("Lemon needs to be granted storage permissions to function!")
                        .SetPositiveButton("Setup", (o, di) => TryRequestPermissions())
                        .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();
            }
        }

        public void TryRequestPermissions()
        {
            bool canRead = CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted;
            bool canWrite = CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Granted;

            if (!canRead || !canWrite)
            {
                RequestPermissions(new string[]
                {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage,
                }, 100);
            }

            if (!PackageManager.CanRequestPackageInstalls())
                RequestInstallUnknownSources();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.R && !Environment.IsExternalStorageManager)
                RequestManageAllFiles();
        }

        public void RequestInstallUnknownSources()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder
                    .SetTitle("Install Permission")
                    .SetMessage("Lemon needs permission to install apps from unknown sources to function!")
                    .SetPositiveButton("Setup", (o, di) => StartActivity(new Intent(Android.Provider.Settings.ActionManageUnknownAppSources, Uri.Parse("package:" + PackageName))))
                    .SetIcon(Android.Resource.Drawable.IcDialogAlert);

            AlertDialog alert = builder.Create();
            alert.SetCancelable(false);
            alert.Show();
        }

        public void RequestManageAllFiles()
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder
                    .SetTitle("Storage Permission")
                    .SetMessage("Lemon needs permission to manage all files to function!")
                    .SetPositiveButton("Setup", (o, di) => StartActivity(new Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission, Uri.Parse("package:" + PackageName))))
                    .SetIcon(Android.Resource.Drawable.IcDialogAlert);

            AlertDialog alert = builder.Create();
            alert.SetCancelable(false);
            alert.Show();
        }

        public void RequestFolderPermissions()
        {
            // Does not help these versions
            if (Build.VERSION.SdkInt > BuildVersionCodes.SV2 || Build.VERSION.SdkInt < BuildVersionCodes.R)
                return;

            if (!FolderPermission.GotAccessTo("/sdcard/Android/data"))
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("Data Folder Permission")
                        .SetMessage("Lemon needs access to the Android data folder to backup game data.")
                        .SetPositiveButton("Setup", (o, di) => FolderPermission.OpenDirectory("/sdcard/Android/data"))
                        .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();
                return;
            }

            if (!FolderPermission.GotAccessTo("/sdcard/Android/obb"))
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("OBB Folder Permission")
                        .SetMessage("Lemon needs access to the Android obb folder to backup game data.")
                        .SetPositiveButton("Setup", (o, di) => FolderPermission.OpenDirectory("/sdcard/Android/obb"))
                        .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();
                return;
            }
        }

        public void OnItemClick(AdapterView parent, View view, int position, long id)
        {
            UnityApplicationData app = _availableApps[position];

            if (!app.IsSupported)
            {
                _unsupportedToast ??= Toast.MakeText(this, "Unsupported application", ToastLength.Short);
                _unsupportedToast.Show();
                return;
            }

            Intent intent = new Intent();
            intent.SetClass(this, typeof(ViewApplication));
            intent.PutExtra("target.packageName", app.PackageName);
            StartActivity(intent);
        }

        internal static void BindShizuku()
        {
            if (NextGenFS.Binder == null)
            {
                Context context2 = Application.Context;
                var nextClass = Java.Lang.Class.FromType(typeof(NGFSService)).Name;
                var package = context2.PackageName!;

                Logger.Instance.Info($"Classname: {nextClass}");
                Logger.Instance.Info($"Package name: {package}");

                var pckManager = context2.PackageManager;

                if (pckManager == null) throw new System.Exception("Null package manager. This should never happen.");
                var verCode = pckManager.GetPackageInfo(package, 0)?.LongVersionCode;

                if (verCode == null) throw new System.Exception("Null verCode. This should never happen.");

                var shizukuArgs = new Shizuku.UserServiceArgs(
                    ComponentName.CreateRelative(package,
                        nextClass)).ProcessNameSuffix("user_service").Debuggable(true).Version((int)verCode);

                Logger.Instance.Info("Trying to bind NextGenFS.");

                NextGenFS = new NextGenFSServiceConnection();

                Shizuku.BindUserService(shizukuArgs, NextGenFS);
            }
        }

        public static bool GetShizukuPermission(bool bind = false)
        {
            if (Shizuku.IsPreV11)
            {
                // Pre-v11 is unsupported
                Toast.MakeText(Application.Context, "Your Shizuku version is too old. Please upgrade.", ToastLength.Long)?.Show();
                return false;
            }

            if (Shizuku.CheckSelfPermission() == 0)
            {
                // Granted
                if (bind)
                    BindShizuku();

                return true;
            }
            else if (Shizuku.ShouldShowRequestPermissionRationale())
            {
                // Users choose "Deny and don't ask again"
                return false;
            }
            else
            {
                // Request the permission
                Shizuku.RequestPermission(1000);
                return false;
            }
        }
    }

    public class ShizukuPermissionResultListener : Java.Lang.Object, Shizuku.IOnRequestPermissionResultListener
    {
        public void OnRequestPermissionResult(int requestCode, int grantResult)
        {
            if (grantResult == (int)Permission.Granted)
            {
                MainActivity.BindShizuku();
            }
            else
            {
                Toast.MakeText(Application.Context, "Shizuku permission not granted.", ToastLength.Long)?.Show();
            }
        }
    }
}