// <copyright file="API.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LSM
{
    using ColossalFramework.Packaging;
    using LoadingScreenMod;
    using LoadingScreenModRevisited;
    using UnityEngine;

    /// <summary>
    /// Static API class for use by other mods.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Gets a value indicating whether LSM patches are applied and LSM sharing is active.
        /// </summary>
        public static bool IsActive => Instance<Sharing>.HasInstance;

        /// <summary>
        /// Attempts to retrieve a shared material with the given checksum from the specified package.
        /// Will first attempt main model materials, then LOD materials.
        /// </summary>
        /// <param name="package">Package containing the material.</param>
        /// <param name="checksum">Material checksum.</param>
        /// <returns>Shared material (null if unsuccessful).</returns>
        public static Material GetMaterial(Package package, string checksum) => Instance<Sharing>.instance?.GetMaterial(checksum, package);

        /// <summary>
        /// Attempts to retrieve a shared material with the given checksum from the specified package.
        /// </summary>
        /// <param name="package">Package containing the material.</param>
        /// <param name="checksum">Material checksum.</param>
        /// <param name="isMain">True requesting the main material, false if requesting a LOD material.</param>
        /// <returns>Shared material (null if unsuccessful).</returns>
        public static Material GetMaterial(Package package, string checksum, bool isMain) => Instance<Sharing>.instance?.GetMaterial(checksum, package, isMain);

        /// <summary>
        /// Attempts to retrieve a shared mesh with the given checksum from the specified package.
        /// Will first attempt main model meshes, then LOD meshes.
        /// </summary>
        /// <param name="package">Package containing the mesh.</param>
        /// <param name="checksum">Mesh checksum.</param>
        /// <returns>Shared mesh (null if unsuccessful).</returns>
        public static Mesh GetMesh(Package package, string checksum) => Instance<Sharing>.instance?.GetMesh(checksum, package);

        /// <summary>
        /// Attempts to retrieve a shared mesh with the given checksum from the specified package.
        /// </summary>
        /// <param name="package">Package containing the mesh.</param>
        /// <param name="checksum">Mesh checksum.</param>
        /// <param name="isMain">True requesting the main maesh, false if requesting a LOD mesh.</param>
        /// <returns>Shared mesh (null if unsuccessful).</returns>
        public static Mesh GetMesh(Package package, string checksum, bool isMain) => Instance<Sharing>.instance?.GetMesh(checksum, package, isMain);

        /// <summary>
        /// Attempts to retrieve the package containing the provided network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Containing package (null if unsuccessful).</returns>
        public static Package GetPackageOf(NetInfo netInfo) => CustomDeserializer.FindAsset(netInfo.name).package;

        /// <summary>
        /// Attempts to retrieve the package asset containing the provided network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab.</param>
        /// <returns>Containing package asset (null if unsuccessful).</returns>
        public static Package.Asset GetAsset(NetInfo netInfo) => CustomDeserializer.FindAsset(netInfo.name);
    }
}
