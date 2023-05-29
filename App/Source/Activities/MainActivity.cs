using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using MelonLoaderInstaller.App.Adapters;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;

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

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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