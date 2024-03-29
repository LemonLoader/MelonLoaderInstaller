﻿using Android.App;
using Android.Content.PM;
using MelonLoaderInstaller.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MelonLoaderInstaller.App.Utilities
{
    public static class UnityApplicationFinder
    {
        public static List<UnityApplicationData> Find(Activity context)
        {
            PackageManager pm = context.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            IList<ApplicationInfo> allPackages = pm.GetInstalledApplications(PackageInfoFlags.MetaData);

            List<UnityApplicationData> unityApps = new List<UnityApplicationData>();

            foreach (ApplicationInfo package in allPackages)
            {
                if (package.NativeLibraryDir == null)
                    continue;

                try
                {
                    bool isUnity = Directory.GetFiles(package.NativeLibraryDir).Any(f => f.Contains("libunity.so"));
                    bool hasPlayer = pm.GetLaunchIntentForPackage(package.PackageName)?.Component?.ClassName?.Contains("UnityPlayer") ?? false;
                    if (!isUnity && !hasPlayer)
                        continue;
                }
                catch (IOException)
                {
                    // This probably means that it's a special app that we can't access
                    continue;
                }

                unityApps.Add(new UnityApplicationData(pm, package));
            }

            return unityApps;
        }

        public static UnityApplicationData FromPackageName(Activity context, string packageName)
        {
            PackageManager pm = context.PackageManager;
            ApplicationInfo packageInfo = pm.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
            return new UnityApplicationData(pm, packageInfo);
        }
    }
}