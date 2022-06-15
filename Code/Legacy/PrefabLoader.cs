using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using UnityEngine;

namespace LoadingScreenMod
{
	public sealed class PrefabLoader : DetourUtility<PrefabLoader>
	{
		private delegate void UpdatePrefabs(Array prefabs);

		private delegate void UpdateCollection(string name, Array keptPrefabs, string[] replacedNames);

		private readonly FieldInfo hasQueuedActionsField = typeof(LoadingManager).GetField("m_hasQueuedActions", BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly FieldInfo[] nameField = new FieldInfo[3];

		private readonly FieldInfo[] prefabsField = new FieldInfo[3];

		private readonly FieldInfo[] replacesField = new FieldInfo[3];

		private readonly FieldInfo netPrefabsField;

		private readonly HashSet<string>[] skippedPrefabs = new HashSet<string>[3];

		private HashSet<string> simulationPrefabs;

		private HashSet<string> keptProps = new HashSet<string>();

		private Matcher skipMatcher = Settings.settings.SkipMatcher;

		private Matcher exceptMatcher = Settings.settings.ExceptMatcher;

		private bool saveDeserialized;

		private const string ROUTINE = "<InitializePrefabs>c__Iterator0";

		internal HashSet<string> SkippedProps => skippedPrefabs[2];

		private PrefabLoader()
		{
			try
			{
				int num = 0;
				Type[] array = new Type[3]
				{
					typeof(BuildingCollection),
					typeof(VehicleCollection),
					typeof(PropCollection)
				};
				for (int i = 0; i < array.Length; i++)
				{
					Type nestedType = array[i].GetNestedType("<InitializePrefabs>c__Iterator0", BindingFlags.NonPublic);
					nameField[num] = nestedType.GetField("name", BindingFlags.Instance | BindingFlags.NonPublic);
					prefabsField[num] = nestedType.GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
					replacesField[num] = nestedType.GetField("replaces", BindingFlags.Instance | BindingFlags.NonPublic);
					skippedPrefabs[num++] = new HashSet<string>();
				}
				netPrefabsField = typeof(NetCollection).GetNestedType("<InitializePrefabs>c__Iterator0", BindingFlags.NonPublic).GetField("prefabs", BindingFlags.Instance | BindingFlags.NonPublic);
				init(typeof(LoadingManager), "QueueLoadingAction");
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		internal void SetSkippedPrefabs(HashSet<string>[] prefabs)
		{
			prefabs.CopyTo(skippedPrefabs, 0);
		}

		internal override void Dispose()
		{
			base.Dispose();
			skipMatcher = (exceptMatcher = null);
			simulationPrefabs?.Clear();
			simulationPrefabs = null;
			Instance<LevelLoader>.instance.SetSkippedPrefabs(skippedPrefabs);
			Array.Clear(skippedPrefabs, 0, skippedPrefabs.Length);
		}

		//[HarmonyPatch(typeof(LoadingManager), nameof(LoadingManager.QueueLoadingAction))]
		//[HarmonyPrefix]
		public static bool QueueLoadingAction(LoadingManager __instance, IEnumerator action)
		{
				Type declaringType = action.GetType().DeclaringType;
			int num = -1;
			if (declaringType == typeof(BuildingCollection))
			{
				num = 0;
			}
			else if (declaringType == typeof(VehicleCollection))
			{
				num = 1;
			}
			else if (declaringType == typeof(PropCollection))
			{
				num = 2;
			}
			if (num >= 0 && !Instance<PrefabLoader>.instance.saveDeserialized)
			{
				while (!Instance<LevelLoader>.instance.IsSaveDeserialized())
				{
					Thread.Sleep(60);
				}
				Instance<PrefabLoader>.instance.saveDeserialized = true;
			}
			while (!Monitor.TryEnter(Instance<LevelLoader>.instance.loadingLock, SimulationManager.SYNCHRONIZE_TIMEOUT))
			{
			}
			try
			{
				switch (num)
				{
				case 0:
					Instance<PrefabLoader>.instance.Skip<BuildingInfo>(action, UpdateBuildingPrefabs, UpdateBuildingCollection, num);
					break;
				case 1:
					Instance<PrefabLoader>.instance.Skip<VehicleInfo>(action, UpdateVehiclePrefabs, UpdateVehicleCollection, num);
					break;
				case 2:
					Instance<PrefabLoader>.instance.Skip<PropInfo>(action, UpdatePropPrefabs, UpdatePropCollection, num);
					break;
				default:
					if (Instance<PrefabLoader>.instance.skipMatcher.Has[2] && declaringType == typeof(NetCollection))
					{
						Instance<PrefabLoader>.instance.RemoveSkippedFromNets(action);
					}
					break;
				}
				Instance<LevelLoader>.instance.mainThreadQueue.Enqueue(action);
				if (Instance<LevelLoader>.instance.mainThreadQueue.Count < 2)
				{
					Instance<PrefabLoader>.instance.hasQueuedActionsField.SetValue(__instance, true);
				}
			}
			finally
			{
				Monitor.Exit(Instance<LevelLoader>.instance.loadingLock);
			}

			return false;
		}

		private void Skip<P>(IEnumerator action, UpdatePrefabs UpdateAll, UpdateCollection UpdateKept, int index) where P : PrefabInfo
		{
			try
			{
				P[] array = prefabsField[index].GetValue(action) as P[];
				if (array == null)
				{
					prefabsField[index].SetValue(action, new P[0]);
					return;
				}
				UpdateAll(array);
				if (!skipMatcher.Has[index])
				{
					return;
				}
				if (index == 0)
				{
					LookupSimulationPrefabs();
				}
				string[] array2 = replacesField[index].GetValue(action) as string[];
				if (array2 == null)
				{
					array2 = new string[0];
				}
				List<P> list = null;
				List<string> list2 = null;
				for (int i = 0; i < array.Length; i++)
				{
					P val = array[i];
					string text = ((i >= array2.Length) ? string.Empty : array2[i]?.Trim());
					if (Skip(val, text, index))
					{
						AddToSkipped(val, text, index);
						Instance<LevelLoader>.instance.skipCounts[index]++;
						if (list == null)
						{
							list = array.ToList(i);
							if (i < array2.Length)
							{
								list2 = array2.ToList(i);
							}
						}
					}
					else if (list != null)
					{
						list.Add(val);
						list2?.Add(text);
					}
				}
				if (list != null)
				{
					P[] array3 = list.ToArray();
					string[] array4 = null;
					prefabsField[index].SetValue(action, array3);
					if (list2 != null)
					{
						array4 = list2.ToArray();
						replacesField[index].SetValue(action, array4);
					}
					UpdateKept(nameField[index].GetValue(action) as string, array3, array4);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void AddToSkipped(PrefabInfo info, string replace, int index)
		{
			HashSet<string> hashSet = skippedPrefabs[index];
			hashSet.Add(info.name);
			if (string.IsNullOrEmpty(replace))
			{
				return;
			}
			if (replace.IndexOf(',') != -1)
			{
				string[] array = replace.Split(',');
				for (int i = 0; i < array.Length; i++)
				{
					hashSet.Add(array[i].Trim());
				}
			}
			else
			{
				hashSet.Add(replace);
			}
		}

		private static void UpdateBuildingPrefabs(Array prefabs)
		{
			if (!Instance<PrefabLoader>.instance.skipMatcher.Has[2])
			{
				return;
			}
			BuildingInfo[] array = prefabs as BuildingInfo[];
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					Instance<PrefabLoader>.instance.RemoveSkippedFromBuilding(array[i]);
				}
			}
		}

		private static void UpdateVehiclePrefabs(Array prefabs)
		{
			if (!Instance<PrefabLoader>.instance.skipMatcher.Has[1])
			{
				return;
			}
			VehicleInfo[] array = prefabs as VehicleInfo[];
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					Instance<PrefabLoader>.instance.RemoveSkippedFromVehicle(array[i]);
				}
			}
		}

		private static void UpdatePropPrefabs(Array prefabs)
		{
			if (!Instance<PrefabLoader>.instance.skipMatcher.Has[2])
			{
				return;
			}
			PropInfo[] array = prefabs as PropInfo[];
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					Instance<PrefabLoader>.instance.RemoveSkippedFromProp(array[i]);
				}
			}
		}

		private static void UpdateBuildingCollection(string name, Array keptPrefabs, string[] replacedNames)
		{
			BuildingCollection buildingCollection = GameObject.Find(name)?.GetComponent<BuildingCollection>();
			if (buildingCollection != null)
			{
				buildingCollection.m_prefabs = keptPrefabs as BuildingInfo[];
				if (replacedNames != null)
				{
					buildingCollection.m_replacedNames = replacedNames;
				}
			}
		}

		private static void UpdateVehicleCollection(string name, Array keptPrefabs, string[] replacedNames)
		{
			VehicleCollection vehicleCollection = GameObject.Find(name)?.GetComponent<VehicleCollection>();
			if (vehicleCollection != null)
			{
				vehicleCollection.m_prefabs = keptPrefabs as VehicleInfo[];
				if (replacedNames != null)
				{
					vehicleCollection.m_replacedNames = replacedNames;
				}
			}
		}

		private static void UpdatePropCollection(string name, Array keptPrefabs, string[] replacedNames)
		{
			PropCollection propCollection = GameObject.Find(name)?.GetComponent<PropCollection>();
			if (propCollection != null)
			{
				propCollection.m_prefabs = keptPrefabs as PropInfo[];
				if (replacedNames != null)
				{
					propCollection.m_replacedNames = replacedNames;
				}
			}
		}

		private bool Skip(PrefabInfo info, string replace, int index)
		{
			if (skipMatcher.Matches(info, index))
			{
				string name = info.name;
				if (index == 0 && IsSimulationPrefab(name, replace))
				{
					Util.DebugPrint(name + " -> not skipped because used in city");
					return false;
				}
				if (exceptMatcher.Matches(info, index))
				{
					Util.DebugPrint(name + " -> not skipped because excepted");
					return false;
				}
				Util.DebugPrint(name + " -> skipped");
				return true;
			}
			return false;
		}

		private bool Skip(PrefabInfo info, int index)
		{
			if (skipMatcher.Matches(info, index))
			{
				return !exceptMatcher.Matches(info, index);
			}
			return false;
		}

		private bool Skip(PropInfo info)
		{
			string name = info.name;
			if (keptProps.Contains(name))
			{
				return false;
			}
			if (skippedPrefabs[2].Contains(name))
			{
				return true;
			}
			bool num = Skip(info, 2);
			(num ? skippedPrefabs[2] : keptProps).Add(name);
			return num;
		}

		internal void LookupSimulationPrefabs()
		{
			if (simulationPrefabs != null)
			{
				return;
			}
			simulationPrefabs = new HashSet<string>();
			try
			{
				Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
				int num = buffer.Length;
				for (int i = 1; i < num; i++)
				{
					if (buffer[i].m_flags != 0)
					{
						string text = PrefabCollection<BuildingInfo>.PrefabName(buffer[i].m_infoIndex);
						if (!string.IsNullOrEmpty(text) && text.IndexOf('.') < 0)
						{
							simulationPrefabs.Add(text);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		internal bool AllPrefabsAvailable()
		{
			return CustomDeserializer.AllAvailable<BuildingInfo>(simulationPrefabs, new HashSet<string>());
		}

		private bool IsSimulationPrefab(string name, string replace)
		{
			if (simulationPrefabs.Contains(name))
			{
				return true;
			}
			if (string.IsNullOrEmpty(replace))
			{
				return false;
			}
			if (replace.IndexOf(',') != -1)
			{
				string[] array = replace.Split(',');
				for (int i = 0; i < array.Length; i++)
				{
					if (simulationPrefabs.Contains(array[i].Trim()))
					{
						return true;
					}
				}
				return false;
			}
			return simulationPrefabs.Contains(replace);
		}

		private void RemoveSkippedFromBuilding(BuildingInfo info)
		{
			BuildingInfo.Prop[] props = info.m_props;
			if (props == null || props.Length == 0)
			{
				return;
			}
			try
			{
				List<BuildingInfo.Prop> list = new List<BuildingInfo.Prop>(props.Length);
				bool flag = false;
				foreach (BuildingInfo.Prop prop in props)
				{
					if (prop != null)
					{
						if (prop.m_prop == null)
						{
							list.Add(prop);
						}
						else if (Skip(prop.m_prop))
						{
							prop.m_prop = (prop.m_finalProp = null);
							flag = true;
						}
						else
						{
							list.Add(prop);
						}
					}
				}
				if (flag)
				{
					info.m_props = list.ToArray();
					if (info.m_props.Length == 0)
					{
						CommonBuildingAI commonBuildingAI = info.m_buildingAI as CommonBuildingAI;
						if ((object)commonBuildingAI != null)
						{
							commonBuildingAI.m_ignoreNoPropsWarning = true;
						}
						else
						{
							CommonBuildingAI commonBuildingAI2 = info.GetComponent<BuildingAI>() as CommonBuildingAI;
							if ((object)commonBuildingAI2 != null)
							{
								commonBuildingAI2.m_ignoreNoPropsWarning = true;
							}
						}
					}
				}
				list.Clear();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RemoveSkippedFromVehicle(VehicleInfo info)
		{
			VehicleInfo.VehicleTrailer[] trailers = info.m_trailers;
			if (trailers == null || trailers.Length == 0)
			{
				return;
			}
			try
			{
				List<VehicleInfo.VehicleTrailer> list = new List<VehicleInfo.VehicleTrailer>(trailers.Length);
				string text = string.Empty;
				bool flag = false;
				bool flag2 = false;
				for (int i = 0; i < trailers.Length; i++)
				{
					VehicleInfo info2 = trailers[i].m_info;
					if (!(info2 == null))
					{
						string name = info2.name;
						if (text != name)
						{
							flag2 = Skip(info2, 1);
							text = name;
						}
						if (flag2)
						{
							trailers[i].m_info = null;
							flag = true;
						}
						else
						{
							list.Add(trailers[i]);
						}
					}
				}
				if (flag)
				{
					info.m_trailers = ((list.Count > 0) ? list.ToArray() : null);
				}
				list.Clear();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RemoveSkippedFromProp(PropInfo info)
		{
			PropInfo.Variation[] variations = info.m_variations;
			if (variations == null || variations.Length == 0)
			{
				return;
			}
			try
			{
				List<PropInfo.Variation> list = new List<PropInfo.Variation>(variations.Length);
				bool flag = false;
				for (int i = 0; i < variations.Length; i++)
				{
					PropInfo prop = variations[i].m_prop;
					if (!(prop == null))
					{
						if (Skip(prop))
						{
							variations[i].m_prop = (variations[i].m_finalProp = null);
							flag = true;
						}
						else
						{
							list.Add(variations[i]);
						}
					}
				}
				if (flag)
				{
					info.m_variations = ((list.Count > 0) ? list.ToArray() : null);
				}
				list.Clear();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private void RemoveSkippedFromNets(IEnumerator action)
		{
			try
			{
				NetInfo[] array = netPrefabsField.GetValue(action) as NetInfo[];
				if (array == null)
				{
					netPrefabsField.SetValue(action, new NetInfo[0]);
					return;
				}
				List<NetLaneProps.Prop> list = new List<NetLaneProps.Prop>(16);
				NetInfo[] array2 = array;
				foreach (NetInfo netInfo in array2)
				{
					if (netInfo.m_lanes == null)
					{
						continue;
					}
					for (int j = 0; j < netInfo.m_lanes.Length; j++)
					{
						NetLaneProps laneProps = netInfo.m_lanes[j].m_laneProps;
						if (laneProps == null || laneProps.m_props == null)
						{
							continue;
						}
						bool flag = false;
						for (int k = 0; k < laneProps.m_props.Length; k++)
						{
							NetLaneProps.Prop prop = laneProps.m_props[k];
							if (prop != null)
							{
								if (prop.m_prop == null)
								{
									list.Add(prop);
								}
								else if (Skip(prop.m_prop))
								{
									prop.m_prop = (prop.m_finalProp = null);
									flag = true;
								}
								else
								{
									list.Add(prop);
								}
							}
						}
						if (flag)
						{
							laneProps.m_props = list.ToArray();
						}
						list.Clear();
					}
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		internal static IEnumerator RemoveSkippedFromSimulation()
		{
			if (Instance<PrefabLoader>.instance != null)
			{
				Instance<PrefabLoader>.instance.RemoveSkippedFromSimulation<BuildingInfo>(0);
				yield return null;
				Instance<PrefabLoader>.instance.RemoveSkippedFromSimulation<VehicleInfo>(1);
				yield return null;
				Instance<PrefabLoader>.instance.RemoveSkippedFromSimulation<PropInfo>(2);
				yield return null;
			}
		}

		private void RemoveSkippedFromSimulation<P>(int index) where P : PrefabInfo
		{
			HashSet<string> hashSet = skippedPrefabs[index];
			if (hashSet == null || hashSet.Count == 0)
			{
				return;
			}
			object @static = Util.GetStatic(typeof(PrefabCollection<P>), "m_prefabLock");
			while (!Monitor.TryEnter(@static, SimulationManager.SYNCHRONIZE_TIMEOUT))
			{
			}
			try
			{
				FastList<PrefabCollection<P>.PrefabData> obj = (FastList<PrefabCollection<P>.PrefabData>)Util.GetStatic(typeof(PrefabCollection<P>), "m_simulationPrefabs");
				int size = obj.m_size;
				PrefabCollection<P>.PrefabData[] buffer = obj.m_buffer;
				for (int i = 0; i < size; i++)
				{
					if (buffer[i].m_name != null && hashSet.Contains(buffer[i].m_name))
					{
						buffer[i].m_name = "lsm___" + (i + (index << 12));
						buffer[i].m_refcount = 0;
					}
				}
			}
			finally
			{
				Monitor.Exit(@static);
			}
		}

		internal static void RemoveSkippedFromStyle(DistrictStyle style)
		{
			PrefabLoader prefabLoader = Instance<PrefabLoader>.instance;
			HashSet<string> hashSet = ((prefabLoader != null) ? prefabLoader.skippedPrefabs[0] : null);
			if (hashSet == null || hashSet.Count == 0)
			{
				return;
			}
			try
			{
				BuildingInfo[] buildingInfos = style.GetBuildingInfos();
				((HashSet<BuildingInfo>)Util.Get(style, "m_Infos")).Clear();
				((HashSet<int>)Util.Get(style, "m_AffectedServices")).Clear();
				BuildingInfo[] array = buildingInfos;
				foreach (BuildingInfo buildingInfo in array)
				{
					if (buildingInfo != null && !hashSet.Contains(buildingInfo.name))
					{
						style.Add(buildingInfo);
					}
				}
				Array.Clear(buildingInfos, 0, buildingInfos.Length);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		internal static void UnloadSkipped()
		{
			if (Instance<PrefabLoader>.instance != null)
			{
				Instance<PrefabLoader>.instance.keptProps.Clear();
				Instance<PrefabLoader>.instance.keptProps = null;
				Instance<PrefabLoader>.instance.simulationPrefabs?.Clear();
				Instance<PrefabLoader>.instance.simulationPrefabs = null;
				int[] skipCounts = Instance<LevelLoader>.instance.skipCounts;
				if (skipCounts[0] > 0)
				{
					Util.DebugPrint("Skipped", skipCounts[0], "building prefabs");
				}
				if (skipCounts[1] > 0)
				{
					Util.DebugPrint("Skipped", skipCounts[1], "vehicle prefabs");
				}
				if (skipCounts[2] > 0)
				{
					Util.DebugPrint("Skipped", skipCounts[2], "prop prefabs");
				}
				try
				{
					Resources.UnloadUnusedAssets();
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}
		}
	}
}
