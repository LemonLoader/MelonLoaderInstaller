using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MelonLoader.Installer.Core
{
    public abstract class Plugin
    {
        internal static readonly List<Plugin> LoadedPlugins = new List<Plugin>();

        public static void ClearPlugins()
        {
            LoadedPlugins.Clear();
        }

        public static bool LoadPlugin(string pluginPath, out bool added)
        {
            added = false;
            try
            {
                Assembly pluginAssembly = Assembly.LoadFile(pluginPath);
                if (pluginAssembly == null)
                {
                    return false;
                }

                Type[] types = pluginAssembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsAbstract || !typeof(Plugin).IsAssignableFrom(type))
                    {
                        continue;
                    }
                    Plugin pluginInstance = (Plugin)Activator.CreateInstance(type);
                    if (pluginInstance != null)
                    {
                        if (LoadedPlugins.Any(p => p.Name == pluginInstance.Name))
                        {
                            LoadedPlugins.RemoveAll(p => p.Name == pluginInstance.Name);
                        }
                        else
                        {
                            added = true;
                            LoadedPlugins.Add(pluginInstance);
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Name of the patch plugin, used for logging and identification
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// Called when the InstallPlugins patch step is ran
        /// </summary>
        /// <param name="patcher"></param>
        /// <returns></returns>
        public abstract bool Run(Patcher patcher);
    }
}
