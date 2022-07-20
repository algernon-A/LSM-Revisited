using ColossalFramework;
using ColossalFramework.Packaging;
using System;
using System.Collections.Generic;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	internal sealed class UsedAssets
	{
		// Instance reference
		internal static UsedAssets instance;


		// Master array of asset hashsets.
		private HashSet<string>[] allAssets;

		// Hashsets of packages.
		private HashSet<string> allPackages = new HashSet<string>();

		// Hashsets of assets.
		private HashSet<string> buildingAssets = new HashSet<string>();
		private HashSet<string> propAssets = new HashSet<string>();
		private HashSet<string> treeAssets = new HashSet<string>();
		private HashSet<string> vehicleAssets = new HashSet<string>();
		private HashSet<string> citizenAssets = new HashSet<string>();
		private HashSet<string> netAssets = new HashSet<string>();

		// Hashset of legacy name conversions.
		private HashSet<string> legacyNames = new HashSet<string>();


		/// <summary>
		/// Constructor.
		/// </summary>
		internal UsedAssets()
		{
			// Create all assets hashset array from individual asset type hashsets.
			// This needs to map to AssetLoader.typeMap.
			allAssets = new HashSet<string>[12]
			{
				buildingAssets, propAssets, treeAssets, vehicleAssets, vehicleAssets, buildingAssets, buildingAssets, propAssets, citizenAssets, netAssets,
				netAssets, buildingAssets
			};

			// Populate used assets hashets - buildings and nets require special treatment.
			GetUsedBuildings(allPackages, buildingAssets);
			GetUsedNets(allPackages, netAssets);

			// Generic types.
			GetUsedAssets<CitizenInfo>(allPackages, citizenAssets);
			GetUsedAssets<PropInfo>(allPackages, propAssets);
			GetUsedAssets<TreeInfo>(allPackages, treeAssets);
			GetUsedAssets<VehicleInfo>(allPackages, vehicleAssets);
		}


		/// <summary>
		/// Clears all data and disposes of the instance.
		/// </summary>
		internal void Dispose()
		{
			// Clear all arrays and hashsets.
			allPackages.Clear();
			buildingAssets.Clear();
			propAssets.Clear();
			treeAssets.Clear();
			vehicleAssets.Clear();
			citizenAssets.Clear();
			netAssets.Clear();

			// Clear all references.
			allPackages = null;
			buildingAssets = null;
			propAssets = null;
			treeAssets = null;
			vehicleAssets = null;
			citizenAssets = null;
			netAssets = null;
			allAssets = null;

			// Dispose of instance.
			instance = null;
		}


		/// <summary>
		/// Checks to see if the package is available.
		/// False positives are possible.
		/// </summary>
		/// <param name="packageName">Package name</param>
		/// <returns>True if the package is a custom asset or if we've got this in our list of all packages, false otherwise</returns>
		internal bool GotPackage(string packageName)
		{
			// Check to see if we've got this package in our list.
			if (!allPackages.Contains(packageName))
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
		/// <param name="assetRef">Asset</param>
		/// <param name="type">Asset type</param>
		/// <returns>True if the custom asset is in use, false otherwise</returns>
		internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type type) => allAssets[(int)type].Contains(assetRef.fullName);


		/// <summary>
		/// Checks to see if the given custom asset of either of two given types is used in the current save.
		/// </summary>
		/// <param name="assetRef">Asset</param>
		/// <param name="type1">First possilbe asset type</param>
		/// <param name="type2">Second possible asset type</param>
		/// <returns>True if the custom asset is in use, false otherwise</returns>
		internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type type1, CustomAssetMetaData.Type type2)
		{
			// Check if the asset is in our list for type 1.
			if (!allAssets[(int)type1].Contains(assetRef.fullName))
			{
				// Not in type 1 - return value is if asset is in list for type 2.
				return allAssets[(int)type2].Contains(assetRef.fullName);
			}

			// Asset was in list for type 1 - return true.
			return true;
		}

		
		/// <summary>
		/// Reports missing assets.
		/// </summary>
		internal void ReportMissingAssets()
		{
			ReportMissingAssets<BuildingInfo>(buildingAssets, CustomAssetMetaData.Type.Building);
			ReportMissingAssets<PropInfo>(propAssets, CustomAssetMetaData.Type.Prop);
			ReportMissingAssets<TreeInfo>(treeAssets, CustomAssetMetaData.Type.Tree);
			ReportMissingAssets<VehicleInfo>(vehicleAssets, CustomAssetMetaData.Type.Vehicle);
			ReportMissingAssets<CitizenInfo>(citizenAssets, CustomAssetMetaData.Type.Citizen);
			ReportMissingAssets<NetInfo>(netAssets, CustomAssetMetaData.Type.Road);
		}


		/// <summary>
		/// Checks to see if all assets have been loaded and are currently available, apart from the specified list to ignore.
		/// </summary>
		/// <param name="ignore">Asset names to ignore (known failures)</param>
		/// <returns>True if all non-ignored assets are available, false otherwise</returns>
		internal bool AllAssetsAvailable(HashSet<string> ignore)
		{
			// Check each deserializer status.
			return CustomDeserializer.AllAvailable<BuildingInfo>(buildingAssets, ignore) &&
				CustomDeserializer.AllAvailable<PropInfo>(propAssets, ignore) &&
				CustomDeserializer.AllAvailable<TreeInfo>(treeAssets, ignore) &&
				CustomDeserializer.AllAvailable<VehicleInfo>(vehicleAssets, ignore) &&
				CustomDeserializer.AllAvailable<CitizenInfo>(citizenAssets, ignore) &&
				CustomDeserializer.AllAvailable<NetInfo>(netAssets, ignore);
		}


		/// <summary>
		/// Reports missing custom assets of the given prefab type.
		/// </summary>
		/// <typeparam name="P">Prefab type</typeparam>
		/// <param name="customAssets">Hashset of custom asset names</param>
		/// <param name="type">Custom asset type</param>
		private void ReportMissingAssets<P>(HashSet<string> customAssets, CustomAssetMetaData.Type type) where P : PrefabInfo
		{
			try
			{
				// Check asset report generation setting.
				bool reportAssets = LoadingScreenMod.Settings.settings.reportAssets | LoadingScreenMod.Settings.settings.hideAssets;

				// Iterate through each custom asset.
				foreach (string customAsset in customAssets)
				{
					// See if this asset was loaded.
					if (CustomDeserializer.FindLoaded<P>(customAsset, tryName: false) == null)
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
		/// <typeparam name="P">Prefab type</typeparam>
		/// <param name="packages">Hashset of package names</param>
		/// <param name="assets">Hashset of asset names</param>
		private void GetUsedAssets<P>(HashSet<string> packages, HashSet<string> assets) where P : PrefabInfo
		{
			try
			{
				// Iterate through each prefab in the relevant collection.
				int prefabCount = PrefabCollection<P>.PrefabCount();
				for (int i = 0; i < prefabCount; ++i)
				{
					// Add package and asset to our list of used assets. 
					Add(PrefabCollection<P>.PrefabName((uint)i), packages, assets);
				}
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception looking up used assets of type ", typeof(P).Name);
			}
		}


		/// <summary>
		/// Determines the buildings that are used by the current save.
		/// </summary>
		/// <param name="packages">Hashset of package names</param>
		/// <param name="assets">Hashset of asset names</param>
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
		/// <param name="packages">Hashset of package names</param>
		/// <param name="assets">Hashset of asset names</param>
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
					if ((nodes[i].m_flags & NetNode.Flags.Created)!= 0)
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
		/// <param name="fullName">Full prefab name</param>
		/// <param name="packages">Package hashset for the relevant asset type</param
		/// <param name="assets">Asset hashset for the relevant asset type</param>
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
		/// <param name="fullName">Full prefab name</param>
		/// <param name="packages">Package hashset for the relevant asset type</param
		/// <param name="assets">Asset hashset for the relevant asset type</param>
		private void AddNet(string fullName, HashSet<string> packages, HashSet<string> assets)
		{
			// Have we already encountered this name?
			if (!string.IsNullOrEmpty(fullName) && !legacyNames.Contains(fullName))
			{
				// Not previously encountered - record this name in our list.
				legacyNames.Add(fullName);

				// Look up any replacement prefab name conversion.
				string finalName = BuildConfig.ResolveLegacyPrefab(fullName);

				// Add final name as per usual.
				Add(finalName, packages, assets);
			}
		}
	}
}
