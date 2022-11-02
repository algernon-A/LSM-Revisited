// <copyright file="ReadByteArrayPatch.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using ColossalFramework.Packaging;
    using HarmonyLib;

    /// <summary>
    /// Harmony patch for faster byte array loading.
    /// </summary>
    [HarmonyPatch(typeof(PackageReader), nameof(PackageReader.ReadByteArray))]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class ReadByteArrayPatch
    {
        /// <summary>
        /// Pre-emptive Harmony prefix for PackageReader.ReadByteArray to implement faster byte array loading.
        /// </summary>
        /// <param name="__result">Original method result.</param>
        /// <param name="__instance">PackageReader instance.</param>
        /// <returns>Always false (never execute original method).</returns>
        public static bool Prefix(ref byte[] __result, PackageReader __instance)
        {
            // ReadBytes is faster than reading one byte at a time.
            __result = __instance.ReadBytes(__instance.ReadInt32());

            // Don't execute original method.
            return false;
        }
    }
}