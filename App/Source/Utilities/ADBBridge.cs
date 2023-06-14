using Android.OS;
using AndroidX.AppCompat.App;
using Java.Lang;
using Java.Util.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MelonLoaderInstaller.App.Utilities
{
    internal static class ADBBridge
    {
        public static AlertDialog AlertDialog { get; set; }
        private static FileObserver _observer;

        public static void AttemptConnect(string filesDir, string packageName, Action afterConnect)
        {
            string textPath = Path.Combine(filesDir, "adbbridge.txt");
            File.WriteAllText(textPath, packageName);

            _observer = new TextObserver(new Java.IO.File(textPath), () =>
            {
                Logger.Instance.Info("File deleted");
                Finalize(afterConnect);
            });
            _observer.StartWatching();
        }

        public static void Finalize(Action afterConnect)
        {
            AlertDialog?.Dismiss();
            afterConnect?.Invoke();
        }

        public static void Kill()
        {
            _observer.StopWatching();
            _observer.Dispose();
        }

        private class TextObserver : FileObserver
        {
            public Action OnDelete;

            public TextObserver(Java.IO.File file, Action onDelete) : base(file)
            {
                OnDelete = onDelete;
            }

            [Obsolete]
            public TextObserver(string path) : base(path)
            {             
            }

            public TextObserver(IList<Java.IO.File> files) : base(files)
            {
            }

            public TextObserver(IntPtr javaReference, Android.Runtime.JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public TextObserver(Java.IO.File file, [Android.Runtime.GeneratedEnum] FileObserverEvents mask) : base(file, mask)
            {
            }

            public TextObserver(string path, [Android.Runtime.GeneratedEnum] FileObserverEvents mask) : base(path, mask)
            {
            }

            public TextObserver(IList<Java.IO.File> files, [Android.Runtime.GeneratedEnum] FileObserverEvents mask) : base(files, mask)
            {
            }

            public override void OnEvent([Android.Runtime.GeneratedEnum] FileObserverEvents e, string path)
            {
                Logger.Instance.Info($"{path} with event {e}");
                if (e == FileObserverEvents.Delete || e == FileObserverEvents.DeleteSelf)
                {
                    OnDelete?.Invoke();
                }
            }
        }
    }
}