using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using MelonLoaderInstaller.App.Adapters;
using MelonLoaderInstaller.App.Models;
using MelonLoaderInstaller.App.Utilities;

namespace MelonLoaderInstaller.App
{
    [Activity(Label = "@string/app_name", Theme = "@style/Theme.MelonLoaderInstaller", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, AdapterView.IOnItemClickListener
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Logger.SetupMainInstance("melonloader");

            PackageWarnings.Run(this);

            List<UnityApplicationData> availableApps = UnityApplicationFinder.Find(this);
            ApplicationsAdapter adapter = new ApplicationsAdapter(this, availableApps);

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
            throw new System.NotImplementedException();
        }
    }
}