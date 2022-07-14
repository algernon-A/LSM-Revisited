using System;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Packaging;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class UsedAssets : Instance<UsedAssets>
	{
		private HashSet<string> allPackages = new HashSet<string>();

		private HashSet<string>[] allAssets;

		private HashSet<string> buildingAssets = new HashSet<string>();

		private HashSet<string> propAssets = new HashSet<string>();

		private HashSet<string> treeAssets = new HashSet<string>();

		private HashSet<string> vehicleAssets = new HashSet<string>();

		private HashSet<string> citizenAssets = new HashSet<string>();

		private HashSet<string> netAssets = new HashSet<string>();

		private UsedAssets()
		{
			allAssets = new HashSet<string>[12]
			{
				buildingAssets, propAssets, treeAssets, vehicleAssets, vehicleAssets, buildingAssets, buildingAssets, propAssets, citizenAssets, netAssets,
				netAssets, buildingAssets
			};
			LookupUsed();
		}

		private void LookupUsed()
		{
			LookupSimulationBuildings(allPackages, buildingAssets);
			LookupSimulationNets(allPackages, netAssets);
			LookupSimulationAssets<CitizenInfo>(allPackages, citizenAssets);
			LookupSimulationAssets<PropInfo>(allPackages, propAssets);
			LookupSimulationAssets<TreeInfo>(allPackages, treeAssets);
			LookupSimulationAssets<VehicleInfo>(allPackages, vehicleAssets);
		}

		internal void Dispose()
		{
			allPackages.Clear();
			buildingAssets.Clear();
			propAssets.Clear();
			treeAssets.Clear();
			vehicleAssets.Clear();
			citizenAssets.Clear();
			netAssets.Clear();
			allPackages = null;
			buildingAssets = null;
			propAssets = null;
			treeAssets = null;
			vehicleAssets = null;
			citizenAssets = null;
			netAssets = null;
			allAssets = null;
			Instance<UsedAssets>.instance = null;
		}

		internal bool GotPackage(string packageName)
		{
			if (!allPackages.Contains(packageName))
			{
				return packageName.IndexOf('.') >= 0;
			}
			return true;
		}

		internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type type)
		{
			return allAssets[(int)type].Contains(assetRef.fullName);
		}

		internal bool IsUsed(Package.Asset assetRef, CustomAssetMetaData.Type t1, CustomAssetMetaData.Type t2)
		{
			if (!allAssets[(int)t1].Contains(assetRef.fullName))
			{
				return allAssets[(int)t2].Contains(assetRef.fullName);
			}
			return true;
		}

		internal void ReportMissingAssets()
		{
			ReportMissingAssets<BuildingInfo>(buildingAssets, CustomAssetMetaData.Type.Building);
			ReportMissingAssets<PropInfo>(propAssets, CustomAssetMetaData.Type.Prop);
			ReportMissingAssets<TreeInfo>(treeAssets, CustomAssetMetaData.Type.Tree);
			ReportMissingAssets<VehicleInfo>(vehicleAssets, CustomAssetMetaData.Type.Vehicle);
			ReportMissingAssets<CitizenInfo>(citizenAssets, CustomAssetMetaData.Type.Citizen);
			ReportMissingAssets<NetInfo>(netAssets, CustomAssetMetaData.Type.Road);
		}

		private static void ReportMissingAssets<P>(HashSet<string> customAssets, CustomAssetMetaData.Type type) where P : PrefabInfo
		{
			try
			{
				bool flag = Settings.settings.reportAssets | Settings.settings.hideAssets;
				foreach (string customAsset in customAssets)
				{
					if ((UnityEngine.Object)CustomDeserializer.FindLoaded<P>(customAsset, tryName: false) == (UnityEngine.Object)null)
					{
						LoadingScreenModRevisited.AssetLoader.Instance.AssetMissing(customAsset);
						if (flag)
						{
							Instance<Reports>.instance.AddMissing(customAsset, type);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		internal bool AllAssetsAvailable(HashSet<string> ignore)
		{
			if (CustomDeserializer.AllAvailable<BuildingInfo>(buildingAssets, ignore) && CustomDeserializer.AllAvailable<PropInfo>(propAssets, ignore) && CustomDeserializer.AllAvailable<TreeInfo>(treeAssets, ignore) && CustomDeserializer.AllAvailable<VehicleInfo>(vehicleAssets, ignore) && CustomDeserializer.AllAvailable<CitizenInfo>(citizenAssets, ignore))
			{
				return CustomDeserializer.AllAvailable<NetInfo>(netAssets, ignore);
			}
			return false;
		}

		private void LookupSimulationAssets<P>(HashSet<string> packages, HashSet<string> assets) where P : PrefabInfo
		{
			try
			{
				int num = PrefabCollection<P>.PrefabCount();
				for (int i = 0; i < num; i++)
				{
					Add(PrefabCollection<P>.PrefabName((uint)i), packages, assets);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void LookupSimulationBuildings(HashSet<string> packages, HashSet<string> assets)
		{
			try
			{
				Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
				int num = buffer.Length;
				for (int i = 1; i < num; i++)
				{
					if (buffer[i].m_flags != 0)
					{
						Add(PrefabCollection<BuildingInfo>.PrefabName(buffer[i].m_infoIndex), packages, assets);
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void LookupSimulationNets(HashSet<string> packages, HashSet<string> assets)
		{
			try
			{
				NetNode[] buffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
				int num = buffer.Length;
				for (int i = 1; i < num; i++)
				{
					if (buffer[i].m_flags != 0)
					{
						Add(PrefabCollection<NetInfo>.PrefabName(buffer[i].m_infoIndex), packages, assets);
					}
				}
				NetSegment[] buffer2 = Singleton<NetManager>.instance.m_segments.m_buffer;
				num = buffer2.Length;
				for (int j = 1; j < num; j++)
				{
					if (buffer2[j].m_flags != 0)
					{
						Add(PrefabCollection<NetInfo>.PrefabName(buffer2[j].m_infoIndex), packages, assets);
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void Add(string fullName, HashSet<string> packages, HashSet<string> assets)
		{
			if (!string.IsNullOrEmpty(fullName))
			{
				int num = fullName.IndexOf('.');
				if (num >= 0 && num < fullName.Length - 1)
				{
					packages.Add(fullName.Substring(0, num));
					assets.Add(fullName);
				}
			}
		}
	}
}
