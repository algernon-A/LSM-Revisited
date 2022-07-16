using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ICities;
using ColossalFramework.Plugins;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Class that manages interactions with other mods, including compatibility and functionality checks.
    /// </summary>
    internal static class ModUtils
    {
        // Mod assembly path cache.
        private static string assemblyPath = null;


        /// <summary>
        /// Returns the current mod version as a string, leaving off any trailing zero versions for build and revision.
        /// </summary>
        internal static string CurrentVersion
        {
            get
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (currentVersion.Revision != 0)
                {
                    return currentVersion.ToString(4);
                }
                else if (currentVersion.Build != 0)
                {
                    return currentVersion.ToString(3);
                }
                else
                {
                    return currentVersion.ToString(2);
                }
            }
        }


        /// <summary>
        /// Returns the filepath of the current mod assembly.
        /// </summary>
        /// <returns>Mod assembly filepath</returns>
        internal static string AssemblyPath
        {
            get
            {
                // Return cached path if it exists.
                if (assemblyPath != null)
                {
                    return assemblyPath;
                }

                // No path cached - get list of currently active plugins.
                IEnumerable<PluginManager.PluginInfo> plugins = PluginManager.instance.GetPluginsInfo();

                // Iterate through list.
                foreach (PluginManager.PluginInfo plugin in plugins)
                {
                    try
                    {
                        // Get all (if any) mod instances from this plugin.
                        IUserMod[] mods = plugin.GetInstances<IUserMod>();

                        // Check to see if the primary instance is this mod.
                        if (mods.FirstOrDefault() is LSMRMod)
                        {
                            // Found it! Return path.
                            return plugin.modPath;
                        }
                    }
                    catch
                    {
                        // Don't care.
                    }
                }

                // If we got here, then we didn't find the assembly.
                Logging.Error("assembly path not found");
                throw new FileNotFoundException(LSMRMod.ModName + ": assembly path not found!");
            }
        }
    }
}
