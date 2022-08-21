// <copyright file="UsedAssets.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using AlgernonCommons;
    using ColossalFramework;
    using ColossalFramework.Packaging;
    using LoadingScreenMod;

    /// <summary>
    /// Tracks assets used in this save.
    /// </summary>
    internal sealed class UsedAssets
    {
        // Instance reference.
        private static UsedAssets s_instance;

        // Hashset of legacy name conversions.
        private readonly HashSet<string> _legacyNames = new HashSet<string>();

        // Master array of asset hashsets.
        private HashSet<string>[] _allAssets;

        // Hashsets of packages.
        private HashSet<string> _allPackages = new HashSet<string>();

        // Hashsets of assets.
        private HashSet<string> _buildingAssets = new HashSet<string>();
        private HashSet<string> _propAssets = new HashSet<string>();
        private HashSet<string> _treeAssets = new HashSet<string>();
        private HashSet<string> _vehicleAssets = new HashSet<string>();
        private HashSet<string> _citizenAssets = new HashSet<string>();
        private HashSet<string> _netAssets = new HashSet<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UsedAssets"/> class.
        /// Constructor.
        /// </summary>
        private UsedAssets()
        {
            // Create all assets hashset array from individual asset type hashsets.
            // This needs to map to AssetLoader.typeMap.
            _allAssets = new HashSet<string>[12]
            {
                _buildingAssets, _propAssets, _treeAssets, _vehicleAssets, _vehicleAssets, _buildingAssets, _buildingAssets, _propAssets, _citizenAssets, _netAssets,
                _netAssets, _buildingAssets,
            };

            // Populate used assets hashets - buildings and nets require special treatment.
            GetUsedBuildings(_allPackages, _buildingAssets);
            GetUsedNets(_allPackages, _netAssets);

            // Generic types.
            GetUsedAssets<CitizenInfo>(_allPackages, _citizenAssets);
            GetUsedAssets<PropInfo>(_allPackages, _propAssets);
            GetUsedAssets<TreeInfo>(_allPackages, _treeAssets);
            GetUsedAssets<VehicleInfo>(_allPackages, _vehicleAssets);
        }

        /// <summary>
        /// Gets the active instance.
        /// </summary>
        public static UsedAssets Instance => s_instance;

        /// <summary>
        /// Creates the instance.
        /// </summary>
        internal static void Create()
        {
            s_instance = new UsedAssets();
        }

        /// <summary>
        /// Clears all data and disposes of the instance.
        /// </summary>
        internal void Dispose()
        {
            // Clear all arrays and hashsets.
            _allPackages.Clear();
            _buildingAssets.Clear();
            _propAssets.Clear();
            _treeAssets.Clear();
            _vehicleAssets.Clear();
            _citizenAssets.Clear();
            _netAssets.Clear();

            // Clear all references.
            _allPackages = null;
            _buildingAssets = null;
            _propAssets = null;
            _treeAssets = null;
            _vehicleAssets = null;
            _citizenAssets = null;
            _netAssets = null;
            _allAssets = null;

            // Dispose of instance.
            s_instance = null;
        }

        /// <summary>
        /// Checks to see if the package is available.
        /// False positives are possible.
        /// </summary>
        /// <param name="packageName">Package name.</param>
        /// <returns>True if the package is a custom asset or if we've got this in our list of all packages, false otherwise.</returns>
        internal bool GotPackage(string packageName)
        {
            // Check to see if we've got this package in our list.
            if (!_allPackages.Contains(packageName))
            {
                // Return true if this is a custom package, otherwise false.
                return packageName.IndexOf('.') >= 0;
            }

            // If we got here, the package is in our list of used assets; return true.
            return true;
        }

        /// <summary>
        /// Checks to see if the given custom asset of the given type is used in the current save.
        /// </summary>
        /// <param name="assetRef">Asset.</param>
        /// <param name="type">Asset type.</param>
        /// <returns>True if the custom asset is in use, false otherwise.</returns>
        internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type type) => _allAssets[(int)type].Contains(assetRef.fullName);

        /// <summary>
        /// Checks to see if the given custom asset of either of two given types is used in the current save.
        /// </summary>
        /// <param name="assetRef">Asset.</param>
        /// <param name="type1">First possilbe asset type.</param>
        /// <param name="type2">Second possible asset type.</param>
        /// <returns>True if the custom asset is in use, false otherwise.</returns>
        internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type type1, CustomAssetMetaData.Type type2)
        {
            // Check if the asset is in our list for type 1.
            if (!_allAssets[(int)type1].Contains(assetRef.fullName))
            {
                // Not in type 1 - return value is if asset is in list for type 2.
                return _allAssets[(int)type2].Contains(assetRef.fullName);
            }

            // Asset was in list for type 1 - return true.
            return true;
        }

        /// <summary>
        /// Reports missing assets.
        /// </summary>
        internal void ReportMissingAssets()
        {
            ReportMissingAssets<BuildingInfo>(_buildingAssets, CustomAssetMetaData.Type.Building);
            ReportMissingAssets<PropInfo>(_propAssets, CustomAssetMetaData.Type.Prop);
            ReportMissingAssets<TreeInfo>(_treeAssets, CustomAssetMetaData.Type.Tree);
            ReportMissingAssets<VehicleInfo>(_vehicleAssets, CustomAssetMetaData.Type.Vehicle);
            ReportMissingAssets<CitizenInfo>(_citizenAssets, CustomAssetMetaData.Type.Citizen);
            ReportMissingAssets<NetInfo>(_netAssets, CustomAssetMetaData.Type.Road);
        }

        /// <summary>
        /// Checks to see if all assets have been loaded and are currently available, apart from the specified list to ignore.
        /// </summary>
        /// <param name="ignore">Asset names to ignore (known failures).</param>
        /// <returns>True if all non-ignored assets are available, false otherwise.</returns>
        internal bool AllAssetsAvailable(HashSet<string> ignore)
        {
            // Check each deserializer status.
            return CustomDeserializer.AllAvailable<BuildingInfo>(_buildingAssets, ignore) &&
                CustomDeserializer.AllAvailable<PropInfo>(_propAssets, ignore) &&
                CustomDeserializer.AllAvailable<TreeInfo>(_treeAssets, ignore) &&
                CustomDeserializer.AllAvailable<VehicleInfo>(_vehicleAssets, ignore) &&
                CustomDeserializer.AllAvailable<CitizenInfo>(_citizenAssets, ignore) &&
                CustomDeserializer.AllAvailable<NetInfo>(_netAssets, ignore);
        }

        /// <summary>
        /// Reports missing custom assets of the given prefab type.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="customAssets">Hashset of custom asset names.</param>
        /// <param name="type">Custom asset type.</param>
        private void ReportMissingAssets<TPrefab>(HashSet<string> customAssets, CustomAssetMetaData.Type type)
            where TPrefab : PrefabInfo
        {
            try
            {
                // Check asset report generation setting.
                bool reportAssets = LSMRSettings.ReportAssets | LSMRSettings.HideAssets;

                // Iterate through each custom asset.
                foreach (string customAsset in customAssets)
                {
                    // See if this asset was loaded.
                    if (CustomDeserializer.FindLoaded<TPrefab>(customAsset, tryName: false) == null)
                    {
                        // Asset not found.
                        AssetLoader.Instance.AssetMissing(customAsset);

                        // Report missing asset if we're doing so.
                        if (reportAssets)
                        {
                            Instance<Reports>.instance.AddMissing(customAsset, type);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception reporting missing assets");
            }
        }

        /// <summary>
        /// Determines the simulation assets of the given type that are used by the current save.
        /// Should not be used for buildings or nets (use alternative methods for those).
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        /// <param name="packages">Hashset of package names.</param>
        /// <param name="assets">Hashset of asset names.</param>
        private void GetUsedAssets<TPrefab>(HashSet<string> packages, HashSet<string> assets)
            where TPrefab : PrefabInfo
        {
            try
            {
                // Iterate through each prefab in the relevant collection.
                int prefabCount = PrefabCollection<TPrefab>.PrefabCount();
                for (int i = 0; i < prefabCount; ++i)
                {
                    // Add package and asset to our list of used assets.
                    Add(PrefabCollection<TPrefab>.PrefabName((uint)i), packages, assets);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception looking up used assets of type ", typeof(TPrefab).Name);
            }
        }

        /// <summary>
        /// Determines the buildings that are used by the current save.
        /// </summary>
        /// <param name="packages">Hashset of package names.</param>
        /// <param name="assets">Hashset of asset names.</param>
        private void GetUsedBuildings(HashSet<string> packages, HashSet<string> assets)
        {
            try
            {
                // Local reference.
                Building[] buildings = Singleton<BuildingManager>.instance.m_buildings.m_buffer;

                // Iterate through each building in the save.
                for (int i = 1; i < buildings.Length; ++i)
                {
                    // If building exists (has flags), add the prefab to our list of used assets.
                    if (buildings[i].m_flags != 0)
                    {
                        Add(PrefabCollection<BuildingInfo>.PrefabName(buildings[i].m_infoIndex), packages, assets);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception looking up used buildings");
            }
        }

        /// <summary>
        /// Determines the networks that are used by the current save.
        /// </summary>
        /// <param name="packages">Hashset of package names.</param>
        /// <param name="assets">Hashset of asset names.</param>
        private void GetUsedNets(HashSet<string> packages, HashSet<string> assets)
        {
            try
            {
                // Local references.
                NetNode[] nodes = Singleton<NetManager>.instance.m_nodes.m_buffer;
                NetSegment[] segments = Singleton<NetManager>.instance.m_segments.m_buffer;

                // Iterate through each node in save.
                for (int i = 1; i < nodes.Length; ++i)
                {
                    // If node exists (has flags), add the prefab to our list of used assets.
                    if ((nodes[i].m_flags & NetNode.Flags.Created) != 0)
                    {
                        AddNet(PrefabCollection<NetInfo>.PrefabName(nodes[i].m_infoIndex), packages, assets);
                    }
                }

                // Iterate through each segment in save.
                for (int i = 1; i < segments.Length; ++i)
                {
                    // If segment exists (has flags), add the prefab to our list of used assets.
                    if ((segments[i].m_flags & NetSegment.Flags.Created) != 0)
                    {
                        AddNet(PrefabCollection<NetInfo>.PrefabName(segments[i].m_infoIndex), packages, assets);
                    }
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception looking up used networks");
            }
        }

        /// <summary>
        /// Adds the given prefab to the lists of packages and assets that are in use (after first checking name for validity).
        /// Will only add custom assets (those with a name containing a period that isn't the final character).
        /// </summary>
        /// <param name="fullName">Full prefab name.</param>
        /// <param name="packages">Package hashset for the relevant asset type.</param>
        /// <param name="assets">Asset hashset for the relevant asset type.</param>
        private void Add(string fullName, HashSet<string> packages, HashSet<string> assets)
        {
            // Safety check.
            if (!string.IsNullOrEmpty(fullName))
            {
                // Only dealing with custom assets, those with a period in their name (that isn't the final character).
                int periodPos = fullName.IndexOf('.');
                if (periodPos >= 0 & periodPos < fullName.Length - 1)
                {
                    // Valid name - add the package name to the used packages hashset, and the full name to the used assets hashset.
                    packages.Add(fullName.Substring(0, periodPos));
                    assets.Add(fullName);
                }
            }
        }

        /// <summary>
        /// Adds the given network prefab to the lists of packages and assets that are in use,
        /// after first checking name for validity and checking for any legacy name mapping.
        /// Will only add custom assets (those with a name containing a period that isn't the final character).
        /// </summary>
        /// <param name="fullName">Full prefab name.</param>
        /// <param name="packages">Package hashset for the relevant asset type.</param>
        /// <param name="assets">Asset hashset for the relevant asset type.</param>
        private void AddNet(string fullName, HashSet<string> packages, HashSet<string> assets)
        {
            // Have we already encountered this name?
            if (!string.IsNullOrEmpty(fullName) && !_legacyNames.Contains(fullName))
            {
                // Not previously encountered - record this name in our list.
                _legacyNames.Add(fullName);

                // Look up any replacement prefab name conversion.
                string finalName = BuildConfig.ResolveLegacyPrefab(fullName);

                // Add final name as per usual.
                Add(finalName, packages, assets);
            }
        }
    }
}
