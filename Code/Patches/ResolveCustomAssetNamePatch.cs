// <copyright file="ResolveCustomAssetNamePatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using ColossalFramework.Packaging;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch for custom asset name resolution.
    /// </summary>
    [HarmonyPatch(typeof(BuildConfig), "ResolveCustomAssetName")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class ResolveCustomAssetNamePatch
    {
        /// <summary>
        /// Pre-emptive Harmony prefix for BuildConfig.ResolveCustomAssetName to implement custom asset name resolution.
        /// </summary>
        /// <param name="__result">Original method result.</param>
        /// <param name="name">Asset name.</param>
        /// <returns>Always false (never execute original method).</returns>
        public static bool Prefix(ref string __result, string name)
        {
            // Check for names without periods and that haven't failed.
            if (name.IndexOf('.') < 0 && !name.StartsWith("lsm___") && !LevelLoader.HasAssetFailed(name))
            {
                Package.Asset[] array = CustomDeserializer.Assets;
                for (int i = 0; i < array.Length; i++)
                {
                    if (name == array[i].name)
                    {
                        __result = array[i].package.packageName + "." + name;

                        // Don't execute original method.
                        return false;
                    }
                }
            }

            // Otherwise, return original name.
            __result = name;

            // Don't execute original method.
            return false;
        }
    }
}