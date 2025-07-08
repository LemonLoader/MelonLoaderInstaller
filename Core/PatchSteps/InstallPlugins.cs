using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonLoader.Installer.Core.PatchSteps
{
    internal class InstallPlugins : IPatchStep
    {
        public bool Run(Patcher patcher)
        {
            foreach(Plugin plugin in Plugin.LoadedPlugins)
            {
                patcher.Logger.Log($"Running custom patch: {plugin.Name}");
                if (!plugin.Run(patcher))
                {
                    patcher.Logger.Log($"Patch {plugin.Name} failed to run.");
                    return false;
                }
            }

            patcher.Logger.Log("All plugins ran successfully.");
            return true;
        }
    }
}
