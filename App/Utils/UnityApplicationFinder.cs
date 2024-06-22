#if ANDROID
using Android.Content.PM;
#endif
using UraniumUI;

namespace MelonLoader.Installer.App.Utils
{
    public static class UnityApplicationFinder
    {
#if ANDROID
        public static List<Data> Find()
        {
            PackageManager pm = Platform.CurrentActivity!.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            IList<ApplicationInfo> allPackages = pm.GetInstalledApplications(PackageInfoFlags.MetaData);

            List<Data> unityApps = [];

            foreach (ApplicationInfo package in allPackages)
            {
                if (package.NativeLibraryDir == null)
                    continue;

                try
                {
                    bool isUnity = Directory.GetFiles(package.NativeLibraryDir).Any(f => f.Contains("libunity.so") && f.Contains("libil2cpp.so"));
                    bool hasPlayer = pm.GetLaunchIntentForPackage(package.PackageName!)?.Component?.ClassName?.Contains("UnityPlayer") ?? false;
                    if (!isUnity && !hasPlayer)
                        continue;
                }
                catch (IOException)
                {
                    // This probably means that it's a special app that we can't access
                    continue;
                }

                unityApps.Add(new Data(package.PackageName!));
            }

            return unityApps;
        }

        public static Data FromPackageName(string packageName)
        {
            PackageManager pm = Platform.CurrentActivity!.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            ApplicationInfo packageInfo = pm.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
            return new Data(packageInfo.Name!);
        }
#else
        // TODO: windows support

        public static List<Data> Find()
        {
            List<Data> unityApps = [new("a"), new("b"), new("c"), new("d")];

            return unityApps;
        }

        public static Data FromPackageName(string packageName)
        {
            return new Data("");
        }
#endif

        public class Data : UraniumBindableObject
        {
            public string AppName { get; private set; }

            public Data(string appName)
            {
                AppName = appName;
            }
        }
    }
}