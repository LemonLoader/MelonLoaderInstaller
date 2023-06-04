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
        private IPatchLogger _logger;

        public Patcher(PatchArguments arguments, IPatchLogger logger)
        {
            _args = arguments;
            _info = new PatchInfo(arguments);
            _logger = logger;
        }

        public bool Run()
        {
            bool success = true;

            try
            {
                _info.CreateDirectories();
            }
            catch (Exception ex)
            {
                _logger.Log("[ERROR] " + ex.ToString());
                success = false;
            }

            return success;
        }
    }
}
