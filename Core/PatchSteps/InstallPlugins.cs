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
                // ends up being empty on windows
                if (!string.IsNullOrEmpty(patcher.Info.PackageName))
                {
                    if (!plugin.CompatiblePackages.Contains(patcher.Info.PackageName))
                    {
                        patcher.Logger.Log($"{plugin.Name} is incompatible with the app you are trying to patch!.");
                        continue;
                    }
                }

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
