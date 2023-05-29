using Android.Util;
using Java.Lang;

namespace MelonLoaderInstaller.App.Utilities
{
    /// <summary>
    /// Very basic wrapper over Android.Util.Log so I don't have to keep repeating tags
    /// </summary>
    public class Logger
    {
        #region Static Stuff

        public static Logger Instance { get; private set; }

        public static void SetupMainInstance(string tag)
        {
            Instance = new Logger(tag);
        }

        #endregion

        private string _tag;

        public Logger(string tag)
        {
            _tag = tag;
        }

        public void Info(string msg) => Log.Info(_tag, msg);
        public void Info(string format, params object[] args) => Log.Info(_tag, format, args);
        public void Info(Throwable tr, string msg) => Log.Info(_tag, tr, msg);
        public void Info(Throwable tr, string format, params object[] args) => Log.Info(_tag, tr, format, args);

        public void Warn(string msg) => Log.Warn(_tag, msg);
        public void Warn(string format, params object[] args) => Log.Warn(_tag, format, args);
        public void Warn(Throwable tr, string msg) => Log.Warn(_tag, tr, msg);
        public void Warn(Throwable tr, string format, params object[] args) => Log.Warn(_tag, tr, format, args);

        public void Error(string msg) => Log.Error(_tag, msg);
        public void Error(string format, params object[] args) => Log.Error(_tag, format, args);
        public void Error(Throwable tr, string msg) => Log.Error(_tag, tr, msg);
        public void Error(Throwable tr, string format, params object[] args) => Log.Error(_tag, tr, format, args);

        public void Verbose(string msg) => Log.Verbose(_tag, msg);
        public void Verbose(string format, params object[] args) => Log.Verbose(_tag, format, args);
        public void Verbose(Throwable tr, string msg) => Log.Verbose(_tag, tr, msg);
        public void Verbose(Throwable tr, string format, params object[] args) => Log.Verbose(_tag, tr, format, args);
    }
}