using System.Collections.Generic;
using System.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result.Contract;
using AndroidX.AppCompat.App;
using AndroidX.Core.App;
using MelonLoaderInstaller.App.Adapters;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MelonLoaderInstaller.App.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, AdapterView.IOnItemClickListener
    {
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

            // TODO: I feel like this mess of AlertDialogs on first start is a messy gross mess and should be cleaned or something
            TryRequestPermissions();
            RequestFolderPermissions();
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
            if (!Environment.IsExternalStorageManager)
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
    }
}