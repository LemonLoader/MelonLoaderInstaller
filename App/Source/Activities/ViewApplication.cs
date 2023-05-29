using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;
using System;
using static Android.Resource;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MelonLoaderInstaller.App.Activities
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller", MainLauncher = false)]
    public class ViewApplication : AppCompatActivity, View.IOnClickListener
    {
        private UnityApplicationData _applicationData;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_view_application);

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

            // TODO: setup LoggerHelper once i implement patching

            CheckWarnings(packageName);

            // TODO: FolderPermission for quest/android 12
            FolderPermission.CurrentContext = this;
            if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2 && Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                FolderPermission.OpenDirectory("/sdcard/Android/data/" + packageName + "/");
            }
        }

        public override void OnActionModeStarted(ActionMode mode)
        {
            MenuInflater.Inflate(Resource.Menu.patch_menu, mode.Menu);
        }

        private void CheckWarnings(string packageName)
        {
            if (PackageWarnings.AvailableWarnings.TryGetValue(packageName, out string warning))
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder
                        .SetTitle("Warning")
                        .SetMessage(warning)
                        .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();

                new WarningCountdown(3500, 1000, builder, alert).Start();
            }
        }

        public void OnClick(View v)
        {
            throw new NotImplementedException();
        }
    }
}