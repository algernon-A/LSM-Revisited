using UnityEngine;
using ColossalFramework.Packaging;
using LoadingScreenMod;


namespace LSM
{
    /// <summary>
    /// Static API class for use by other mods.
    /// </summary>
    public static class API
    {
        /// <summary>
        /// Returns true if LSM patches are applied and LSM sharing is active, false otherwise.
        /// </summary>
        public static bool IsActive => Instance<Sharing>.HasInstance;


        /// <summary>
        /// Attemps to retrieve a shared material with the given checksum from the specified package.
        /// </summary>
        /// <param name="package">Package containing the material</param>
        /// <param name="checksum">Material checksum</param>
        /// <returns>Shared material (null if unsuccessful)</returns>
        public static Material GetMaterial(Package package, string checksum) => Instance<Sharing>.instance?.GetMaterial(checksum, package);


        /// <summary>
        /// Attemps to retrieve a shared mesh with the given checksum from the specified package.
        /// </summary>
        /// <param name="package">Package containing the mesh</param>
        /// <param name="checksum">Mesh checksum</param>
        /// <returns>Shared mesh (null if unsuccessful)</returns>
        public static Mesh GetMesh(Package package, string checksum) => Instance<Sharing>.instance?.GetMesh(checksum, package);


        /// <summary>
        /// Attempts to retrieve the package containing the provided network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab</param>
        /// <returns>Containing package (null if unsuccessful)</returns>
        public static Package GetPackageOf(NetInfo netInfo) => CustomDeserializer.FindAsset(netInfo.name).package;


        /// <summary>
        /// Attempts to retrieve the package asset containing the provided network prefab.
        /// </summary>
        /// <param name="netInfo">Network prefab</param>
        /// <returns>Containing package asset (null if unsuccessful)</returns>
        public static Package.Asset GetAsset(NetInfo netInfo) => CustomDeserializer.FindAsset(netInfo.name);
    }
}
