using AndroidX.AppCompat.App;
using Java.Lang;
using Java.Util.Concurrent;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MelonLoaderInstaller.App.Utilities
{
    internal static class ADBBridge
    {
        public static AlertDialog AlertDialog { get; set; }
        private static bool _shouldDie;

        public static void AttemptConnect(string filesDir, string packageName, Action afterConnect)
        {
            _shouldDie = false;

            string textPath = Path.Combine(filesDir, "adbbridge.txt");
            File.WriteAllText(textPath, packageName);
            Executors.NewSingleThreadExecutor().Execute(new Runnable(async () =>
            {
                while (File.Exists(textPath))
                {
                    if (_shouldDie)
                        return;

                    await Task.Delay(5000);
                }

                Finalize(afterConnect);
            }));
        }

        public static void Finalize(Action afterConnect)
        {
            AlertDialog?.Dismiss();
            afterConnect?.Invoke();
        }

        public static void Kill()
        {
            _shouldDie = true;
        }
    }
}