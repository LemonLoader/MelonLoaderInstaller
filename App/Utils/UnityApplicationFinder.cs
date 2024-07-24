using System.Reflection;

#if ANDROID
using Android.Content.PM;
using Android.Graphics.Drawables;
#endif

namespace MelonLoader.Installer.App.Utils
{
    public static class UnityApplicationFinder
    {
        private static Dictionary<string, Data> _cachedDatas = [];

        public static IEnumerable<Data> Find(CancellationToken token = default)
        {
#if ANDROID
            PackageManager pm = Platform.CurrentActivity!.PackageManager ?? throw new Exception("PackageManager is null, how does this happen?");
            IList<ApplicationInfo> allPackages = pm.GetInstalledApplications(PackageInfoFlags.MetaData);

            foreach (ApplicationInfo package in allPackages)
            {
                if (token.IsCancellationRequested)
                    yield break;

                if (package.NativeLibraryDir == null)
                    continue;

                string[]? libs = null;

                try
                {
                    libs = Directory.GetFiles(package.NativeLibraryDir);
                    bool isUnity = libs.Any(f => f.Contains("libunity.so")) && libs.Any(f => f.Contains("libil2cpp.so"));
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

                Status status = GetStatusFromInfo(package, libs);

                byte[]? iconData = null;
                Drawable? iconDrawable = package.LoadIcon(pm);
                if (iconDrawable != null)
                {
                    int width = iconDrawable.IntrinsicWidth > 0 ? iconDrawable.IntrinsicWidth : 128;
                    int height = iconDrawable.IntrinsicHeight > 0 ? iconDrawable.IntrinsicHeight : 128;

                    Android.Graphics.Bitmap bitmap = Android.Graphics.Bitmap.CreateBitmap(width, height, Android.Graphics.Bitmap.Config.Argb8888!);

                    if (iconDrawable is BitmapDrawable bitmapDrawable)
                    {
                        bitmapDrawable.SetAntiAlias(true);
                    }

                    iconDrawable.SetBounds(0, 0, width, height);

                    Android.Graphics.Canvas canvas = new(bitmap);
                    iconDrawable.Draw(canvas);

                    using MemoryStream ms = new();
                    bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png!, 100, ms);
                    iconData = ms.ToArray();

                    bitmap.Recycle();
                }

                List<string> apks = [ package.PublicSourceDir! ];
                if (package.SplitPublicSourceDirs != null)
                    apks.AddRange(package.SplitPublicSourceDirs);

                Data data = new(name, package.PackageName!, status, [.. apks], iconData);
                yield return data;
                _cachedDatas.Add(package.PackageName!, data);
            }
#else
            List<Data> datas = ADBManager.GetAppDatasFromListingTool();
            foreach (Data data in datas)
            {
                yield return data;
                _cachedDatas.Add(data.PackageName, data);
            }
#endif
        }

        public static Data? FromPackageName(string packageName)
        {
            if (_cachedDatas.TryGetValue(packageName, out Data? data) && data != null)
                return data;

            return null;
        }

#if ANDROID
        private static Status GetStatusFromInfo(ApplicationInfo info, string[]? libs = null)
        {
            libs ??= Directory.GetFiles(info.NativeLibraryDir!);

            Status status = Status.Unpatched;
            if (!info.NativeLibraryDir?.Contains("arm64") ?? true)
                status = Status.Unsupported;
            if (libs.Any(f => f.Contains("libBootstrap.so")) && libs.Any(f => f.Contains("libdobby.so")))
                status = Status.Patched;

            return status;
        }
#endif

        public class Data : BindableObject
        {
            public byte[] RawIconData { get; private set; }
            public string AppName { get; private set; }
            public string PackageName { get; private set; }
            public Status Status { get; private set; }
            public string[] APKPaths { get; private set; }

            public string StatusString => Status == Status.Unpatched ? "" : " • " + Status.ToString().ToUpper();

            private static byte[] PlaceholderIcon
            {
                get
                {
                    if (_placeholderIcon == null)
                    {
                        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream("MelonLoader.Installer.App.Resources.Images.placeholder_icon.png");
                        using MemoryStream memStr = new();

                        stream!.CopyTo(memStr);

                        _placeholderIcon = memStr.ToArray();
                    }

                    return _placeholderIcon;
                }
            }
            private static byte[]? _placeholderIcon;

            public Data(string appName, string packageName, Status status, string[] apkPaths, byte[]? icon = null)
            {
                AppName = appName;
                PackageName = packageName;
                Status = status;
                APKPaths = apkPaths;
                RawIconData = icon ?? PlaceholderIcon;
            }
        }

        public enum Status
        {
            Unpatched,
            Patched,
            Unsupported,
        }
    }
}