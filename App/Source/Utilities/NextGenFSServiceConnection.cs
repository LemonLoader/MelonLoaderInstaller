using System;
using System.Linq;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Systems;
using Android.Util;
using IO.Rayshift.Translatefgo;
using Java.Interop;

namespace MelonLoaderInstaller.App.Utilities
{
    public class NextGenFSServiceConnection : Java.Lang.Object, IServiceConnection, INGFSService
    {
        static readonly JniPeerMembers _members = new XAPeerMembers("io/rayshift/translatefgo$Default", typeof(NGFSServiceDefault));
        public INGFSService Binder { get; private set; }

        private static object FileTransferLock = new object();

        private static readonly string BinderError =
            "Shizuku binder dead. Make sure Shizuku is running, and restart the app.";

        public NextGenFSServiceConnection()
        {
            Binder = null;
        }

        private static int CHUNK_SIZE = 1024 * 128;

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            if (service != null && service.PingBinder())
            {
                Binder = NGFSServiceStub.AsInterface(service);
                Log.Info("melonloader", "NGFS bound; pid=" + Os.Getpid() + ", uid=" + Os.Getuid());
            }
        }
        public void OnServiceDisconnected(ComponentName name)
        {
            Log.Warn("melonloader", "NGFS unbound");
            Binder = null;
        }

        public void Destroy()
        {
            Binder?.Destroy();
        }

        public void Exit()
        {
            Binder?.Exit();
        }

        public bool CopyFile(string source, string destination, NGFSError error)
        {
            var result = Binder?.CopyFile(source, destination, error);
            if (result != null) return (bool)result;

            error.IsSuccess = false;
            error.Error = BinderError;
            return false;
        }

        public bool MoveDirectory(string source, string destination, NGFSError error)
        {
            var result = Binder?.MoveDirectory(source, destination, error);
            if (result != null) return (bool)result;

            error.IsSuccess = false;
            error.Error = BinderError;
            return false;
        }

        public int GetExistingFileSize(string filename, NGFSError error)
        {
            var result = Binder?.GetExistingFileSize(filename, error);
            if (result != null) return (int)result;

            error.IsSuccess = false;
            error.Error = BinderError;
            return -1;
        }

        public bool GetFileExists(string filename, NGFSError error)
        {
            var result = Binder?.GetFileExists(filename, error);
            if (result != null) return (bool)result;

            error.IsSuccess = false;
            error.Error = BinderError;
            return false;
        }

        public long GetFileModTime(string filename, NGFSError error)
        {
            var result = Binder?.GetFileModTime(filename, error);

            if (result != null) return (long)result;

            error.IsSuccess = false;
            error.Error = BinderError;

            return -1;
        }

        public string[] ListDirectoryContents(string filename, NGFSError error)
        {
            var result = Binder?.ListDirectoryContents(filename, error);

            if (result != null) return result;

            error.IsSuccess = false;
            error.Error = BinderError;

            return null;
        }

        public byte[] ReadExistingFile(string filename, int offset, int length, NGFSError error)
        {
            lock (FileTransferLock)
            {
                var result = Binder?.ReadExistingFile(filename, offset, length, error);

                if (result != null) return result;

                error.IsSuccess = false;
                error.Error = BinderError;

                return null;
            }
        }

        public byte[] ReadExistingFile(string filename, NGFSError error)
        {
            var ctx = Android.App.Application.Context;
            var cache = ctx.GetExternalCacheDirs()?.FirstOrDefault();

            if (cache == null)
            {
                throw new System.Exception("External cache directory is null.");
            }

            var guid = Guid.NewGuid();
            var path = System.IO.Path.Combine(cache.Path, guid + ".bin");

            bool res = CopyFile(filename, path, error);

            if (!res || !error.IsSuccess) return null;

            var bytes = System.IO.File.ReadAllBytes(path);

            System.IO.File.Delete(path);

            return bytes;
        }

        public bool RemoveFileIfExists(string filename, NGFSError error)
        {
            var result = Binder?.RemoveFileIfExists(filename, error);
            if (result != null) return (bool)result;

            error.IsSuccess = false;
            error.Error = BinderError;

            return false;
        }

        public bool WriteFileContents(string filename, byte[] contents, int offset, int length, NGFSError error)
        {
            lock (FileTransferLock)
            {
                var result = Binder?.WriteFileContents(filename, contents, offset, length, error);
                if (result != null) return (bool)result;

                error.IsSuccess = false;
                error.Error = BinderError;

                return false;
            }
        }

        public bool WriteFileContents(string filename, byte[] contents, NGFSError error)
        {
            var ctx = Android.App.Application.Context;
            var cache = ctx.GetExternalCacheDirs()?.FirstOrDefault();

            if (cache == null)
            {
                throw new System.Exception("External cache directory is null.");
            }

            var guid = Guid.NewGuid();
            var path = System.IO.Path.Combine(cache.Path, guid + ".bin");

            System.IO.File.WriteAllBytes(path, contents);

            bool res = CopyFile(path, filename, error);
            System.IO.File.Delete(path);

            if (!res || !error.IsSuccess) return false;

            return true;
        }

        [Register("asBinder", "()Landroid/os/IBinder;", "GetAsBinderHandler")]
        public virtual unsafe global::Android.OS.IBinder AsBinder()
        {
            try
            {
                JniObjectReference val = _members.InstanceMethods.InvokeVirtualObjectMethod("asBinder.()Landroid/os/IBinder;", this, null);
                return Java.Lang.Object.GetObject<IBinder>(val.Handle, JniHandleOwnership.TransferLocalRef);
            }
            finally
            {
            }
        }
    }
}