using System;

namespace MelonLoaderInstaller.Core
{
    /// <summary>
    /// Main class that handles starting patches
    /// </summary>
    public class Patcher
    {
        private PatchArguments _args;
        private PatchInfo _info;

        public static void Run(PatchArguments arguments)
        {
            Patcher patcher = new Patcher
            {
                _args = arguments,
                _info = new PatchInfo(arguments)
            };

            patcher.InternalRun();
        }

        private void InternalRun()
        {

        }
    }
}
