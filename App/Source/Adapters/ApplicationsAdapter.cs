using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using AssetRipper.VersionUtilities;
using MelonLoaderInstaller.App.Models;
using System.Collections.Generic;

namespace MelonLoaderInstaller.App.Adapters
{
    public class ApplicationsAdapter : ArrayAdapter<UnityApplicationData>
    {
        public ApplicationsAdapter(Context context, List<UnityApplicationData> apps) : base(context, 0, apps)
        {
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            // Get the data item for this position
            UnityApplicationData application = GetItem(position);

            // Check if an existing view is being reused, otherwise inflate the view
            convertView = LayoutInflater.From(Context)?.Inflate(Resource.Layout.item_supported_application, parent, false);

            // Lookup view for data population
            TextView applicationName = convertView.FindViewById<TextView>(Resource.Id.applicationNameList);
            TextView unityVersion = convertView.FindViewById<TextView>(Resource.Id.unityVersionList);
            ImageView applicationIcon = convertView.FindViewById<ImageView>(Resource.Id.applicationIconList);
            TextView applicationPatched = convertView.FindViewById<TextView>(Resource.Id.isPatchedList);
            // Populate the data into the template view using the data object

            applicationName.Text = application.AppName;
            applicationIcon.SetImageDrawable(application.Icon);

            if (!application.IsSupported)
            {
                applicationPatched.Visibility = ViewStates.Visible;
                applicationPatched.Text = "unsupported";
                applicationPatched.SetTextColor(Color.Red);
            }
            else if (application.IsPatched)
            {
                applicationPatched.Visibility = ViewStates.Visible;
                applicationPatched.Text = "patched";
                applicationPatched.SetTextColor(Color.Green);

            }
            else
                applicationPatched.Visibility = ViewStates.Gone;

            unityVersion.Text = application.EngineVersion != UnityVersion.MinVersion ? application.EngineVersion.ToString() : "Unknown";
            unityVersion.Visibility = ViewStates.Visible;

            // Return the completed view to render on screen
            return convertView;
        }
    }
}