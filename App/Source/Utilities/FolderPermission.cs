using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Provider;
using AndroidX.Activity.Result;
using AndroidX.DocumentFile.Provider;
using Java.Lang;
using File = System.IO.File;

namespace MelonLoaderInstaller.App.Utilities
{
    // adapted from https://github.com/ComputerElite/QuestAppVersionSwitcher/blob/a168f5c0a77d35fb9d00096d1e75352032ced756/QuestAppVersionSwitcher/FolderPermission.cs
    public static class FolderPermission
    {
        public static Activity CurrentContext;

        public static ActivityResultLauncher l = null;
        public static void OpenDirectory(string dirInExtenalStorage)
        {
            //if (Build.VERSION.SdkInt <= BuildVersionCodes.SV2 && !Directory.Exists(dirInExtenalStorage))
            //    Directory.CreateDirectory(dirInExtenalStorage);

            Intent intent = new Intent(Intent.ActionOpenDocumentTree).PutExtra(DocumentsContract.ExtraInitialUri, Uri.Parse(RemapPathForApi300OrAbove(dirInExtenalStorage)));
            l.Launch(intent);
        }

        public static string RemapPathForApi300OrAbove(string path)
        {
            string text = path;
            if (text.StartsWith("/sdcard"))
                text = text["/sdcard".Length..];

            if (text.StartsWith(Environment.ExternalStorageDirectory.AbsolutePath))
                text = path[Environment.ExternalStorageDirectory.AbsolutePath.Length..];

            if (text.Length < 1)
                text = "/";

            string documentId = "primary:" + text[1..];
            return DocumentsContract.BuildDocumentUri("com.android.externalstorage.documents", documentId).ToString();
        }

        public static DocumentFile GetAccessToFile(string dir)
        {
            Logger.Instance.Info("Trying to get access to " + dir);

            string text = "/sdcard/Android/data";
            if (dir.Contains("/Android/obb/"))
                text = "/sdcard/Android/obb";

            string diff = dir.Replace(text, "");
            string[] dirs = diff.Split('/');
            DocumentFile docFile = DocumentFile.FromTreeUri(CurrentContext, Uri.Parse(RemapPathForApi300OrAbove(text).Replace("com.android.externalstorage.documents/document/", "com.android.externalstorage.documents/tree/")));
            foreach (string dirName in dirs)
            {
                if (string.IsNullOrWhiteSpace(dirName))
                    continue;

                if (docFile.FindFile(dirName) == null)
                    docFile.CreateDirectory(dirName);

                docFile = docFile.FindFile(dirName);
            }
            return docFile;
        }

        /*public static void Copy(string from, string to)
        {
            Stream file = GetOutputStream(to);
            StreamWriter sw = new StreamWriter(file);
            sw.Write(File.ReadAllBytes(from));
            sw.Dispose();
        }

        public static void CreateDirectory(string dir)
        {
            DocumentFile parent = GetAccessToFile(Directory.GetParent(dir).FullName);

            Logger.Instance.Info(parent.CanWrite().ToString());

            parent.CreateDirectory(Path.GetFileName(dir));
        }

        public static Stream GetOutputStream(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            directory.FindFile(name)?.Delete();
            return CurrentContext.ContentResolver.OpenOutputStream(directory.CreateFile("application/octet-stream", name).Uri);
        }

        public static void Delete(string path)
        {
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            directory.FindFile(name)?.Delete();
        }

        public static void CreateDirectoryIfNotExisting(string path)
        {
            Logger.Instance.Info("Creating directory " + path + " if it doesn't exist");
            DocumentFile directory = GetAccessToFile(Directory.GetParent(path).FullName);
            string name = Path.GetFileName(path);
            if (directory.FindFile(name) == null) directory.CreateDirectory(name);
        }*/
    }

    public class FolderPermissionCallback : Object, IActivityResultCallback
    {
        public void OnActivityResult(Result resultCode, Intent data)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (data.Data != null)
                {
                    FolderPermission.CurrentContext.ContentResolver.TakePersistableUriPermission(
                        data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
        }
        public void OnActivityResult(Object result)
        {
            if (result is ActivityResult activityResult && Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                if (activityResult.Data.Data != null)
                {
                    FolderPermission.CurrentContext.ContentResolver.TakePersistableUriPermission(
                        activityResult.Data.Data,
                        ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                }
            }
        }
    }
}