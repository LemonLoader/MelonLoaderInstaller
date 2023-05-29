using Android.App;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using Java.Lang;
using Java.Util.Concurrent;
using MelonLoaderInstaller.App.Utilities;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MelonLoaderInstaller.App
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Logger.SetupMainInstance("melonloader");

            PackageWarnings.Run(this);

            Logger.Instance.Info("Post.");
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}