﻿using System.Reflection;

#if ANDROID
using Android.Content.PM;
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

                yield return new Data(name, package.PackageName!, status, iconData);
            }
#else
                List<Data> unityApps = [
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unsupported),
                    new("Test App", "com.veryreal.app", Status.Patched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unsupported),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Patched),
                    new("Test App", "com.veryreal.app", Status.Patched),
                    new("Test App", "com.veryreal.app", Status.Unsupported),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unsupported),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Patched),
                    new("Test App", "com.veryreal.app", Status.Patched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unpatched),
                    new("Test App", "com.veryreal.app", Status.Unsupported),
                    new("Test App", "com.veryreal.app", Status.Unpatched)];

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
            Status status = GetStatusFromInfo(packageInfo);
            return new Data(name, packageInfo.PackageName!, status);
#else
            return new Data("", "", Status.Unpatched);
#endif
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

            public string StatusString => Status == Status.Unpatched ? "" : Status.ToString().ToUpper();

            public Color StatusColor
            {
                get
                {
                    return Status switch
                    {
                        Status.Patched => new(0, 255, 0, 255),
                        Status.Unsupported => new(255, 0, 0, 255),
                        _ => new(255, 255, 255, 255),
                    };
                }
            }

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

            public Data(string appName, string packageName, Status status, byte[]? icon = null)
            {
                AppName = appName;
                PackageName = packageName;
                Status = status;
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