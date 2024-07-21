using System.Reflection;
#if ANDROID
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
#endif

namespace MelonLoader.Installer.App.Utils
{
    public static class UnityApplicationFinder
    {
        public static IEnumerable<Data> Find()
        {
#if ANDROID
            PackageManager pm = Platform.CurrentActivity!.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            IList<ApplicationInfo> allPackages = pm.GetInstalledApplications(PackageInfoFlags.MetaData);

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

                string name = pm.GetApplicationLabel(package);

                byte[]? iconData = null;
                Drawable? iconDrawable = package.LoadIcon(pm);
                if (iconDrawable != null)
                {
                    int width = iconDrawable.IntrinsicWidth > 0 ? iconDrawable.IntrinsicWidth : 128;
                    int height = iconDrawable.IntrinsicHeight > 0 ? iconDrawable.IntrinsicHeight : 128;

                    Bitmap bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888!);

                    if (iconDrawable is BitmapDrawable bitmapDrawable)
                    {
                        bitmapDrawable.SetAntiAlias(true);
                    }

                    iconDrawable.SetBounds(0, 0, width, height);

                    Canvas canvas = new(bitmap);
                    iconDrawable.Draw(canvas);

                    using MemoryStream ms = new();
                    bitmap.Compress(Bitmap.CompressFormat.Png!, 100, ms);
                    iconData = ms.ToArray();

                    bitmap.Recycle();
                }

                yield return new Data(name, package.PackageName!, iconData);
            }
#else
                List<Data> unityApps = [
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app"),
                    new("Test App", "com.veryreal.app")];

                foreach (var app in unityApps)
                    yield return app;
#endif
        }

        public static Data FromPackageName(string packageName)
        {
#if ANDROID
            PackageManager pm = Platform.CurrentActivity!.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            ApplicationInfo packageInfo = pm.GetApplicationInfo(packageName, PackageInfoFlags.MetaData);
            string name = pm.GetApplicationLabel(packageInfo);
            return new Data(name, packageInfo.PackageName!);
#else
            return new Data("", "");
#endif
        }

        public class Data : BindableObject
        {
            public byte[] RawIconData { get; private set; }
            public string AppName { get; private set; }
            public string PackageName { get; private set; }

            private static byte[] PlaceholderIcon
            {
                get
                {
                    if (_placeholderIcon == null)
                    {
                        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("MelonLoader.Installer.App.Resources.Images.test_app_icon.png");
                        using MemoryStream memStr = new();

                        stream!.CopyTo(memStr);

                        _placeholderIcon = memStr.ToArray();
                    }

                    return _placeholderIcon;
                }
            }
            private static byte[]? _placeholderIcon;

            public Data(string appName, string packageName, byte[]? icon = null)
            {
                AppName = appName;
                PackageName = packageName;
                RawIconData = icon ?? PlaceholderIcon;
            }
        }
    }
}