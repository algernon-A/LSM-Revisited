using ColossalFramework.Plugins;
using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace LoadingScreenModRevisited
{
    /// <summary>
    /// Class that manages interactions with assemblies, including compatibility and functionality checks.
    /// </summary>
    internal static class AssemblyUtils
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
                        if (mods.FirstOrDefault() is Mod)
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
                return null;
            }
        }
    }
}
