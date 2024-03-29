﻿using Android.App;
using Android.OS;
using AndroidX.AppCompat.App;
using System.Collections.Generic;
using System.Net;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;

namespace MelonLoaderInstaller.App.Utilities
{
    public static class PackageWarnings
    {
        public static Dictionary<string, string> AvailableWarnings { get; private set; }

        public static void Run(Activity context)
        {
            using WebClient client = new WebClient();
            string warnings = "";
            try { warnings = client.DownloadString("https://raw.githubusercontent.com/LemonLoader/MelonLoaderInstaller/master/package_warnings.json"); }
            catch (WebException)
            {
                AvailableWarnings = new Dictionary<string, string>();
                AlertDialog.Builder builder = new AlertDialog.Builder(context);
                builder
                        .SetTitle("Error")
                        .SetMessage("Unable to connect to GitHub! Please check your connection and try again")
                        .SetPositiveButton("Exit", (o, di) => context.FinishAndRemoveTask())
                        .SetIcon(Android.Resource.Drawable.IcDialogAlert);

                AlertDialog alert = builder.Create();
                alert.SetCancelable(false);
                alert.Show();
                return;
            }

            AvailableWarnings = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(warnings);
        }
    }

    public class WarningCountdown : CountDownTimer
    {
        private AlertDialog.Builder _builder;
        private AlertDialog _alert;

        public WarningCountdown(long millisInFuture, long countDownInterval, AlertDialog.Builder builder, AlertDialog alert) : base(millisInFuture, countDownInterval)
        {
            _builder = builder;
            _alert = alert;
        }

        public override void OnFinish()
        {
            _builder.SetPositiveButton("Understood", (a, b) => { });
            _builder.Create().Show();
            _alert.Dismiss();
        }

        public override void OnTick(long millisUntilFinished)
        {
        }
    }
}