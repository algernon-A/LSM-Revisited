using System;
using System.Collections;
using System.Threading;
using ColossalFramework;
using UnityEngine;

namespace LoadingScreenMod
{
	internal class Safenets : DetourUtility<Safenets>
	{
		private delegate void ARef<T>(uint i, ref T item);

		private Safenets()
		{
			init(typeof(LoadingProfiler), "ContinueLoading");
		}

		internal static IEnumerator Setup()
		{
			Instance<Safenets>.Create().Deploy();
			yield break;
		}

		//[HarmonyPatch(typeof(LoadingProfiler), nameof(LoadingProfiler.ContinueLoading))]
		//[HarmonyPrefix]
		public static bool ContinueLoading(LoadingProfiler __instance)
		{
			ProfilerSource.GetEvents(__instance).Add(new LoadingProfiler.Event(LoadingProfiler.Type.ContinueLoading, null, 0L));
			if (Thread.CurrentThread == Singleton<SimulationManager>.instance.m_simulationThread)
			{
				try
				{
					Util.DebugPrint("Starting recovery at", Profiling.Millis);
					Instance<Safenets>.instance.Dispose();
					PrefabCollection<NetInfo>.BindPrefabs();
					PrefabCollection<BuildingInfo>.BindPrefabs();
					PrefabCollection<PropInfo>.BindPrefabs();
					PrefabCollection<TreeInfo>.BindPrefabs();
					PrefabCollection<TransportInfo>.BindPrefabs();
					PrefabCollection<VehicleInfo>.BindPrefabs();
					PrefabCollection<CitizenInfo>.BindPrefabs();
					RemoveBadVehicles();
					FastList<ushort> badNodes = GetBadNodes();
					FastList<ushort> badSegments = GetBadSegments();
					RemoveBadNodes(badNodes);
					RemoveBadSegments(badSegments);
					Singleton<SimulationManager>.instance.SimulationPaused = true;
					Util.DebugPrint("Recovery finished at", Profiling.Millis);
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
			}

			return false;
		}

		private static FastList<ushort> GetBadNodes()
		{
			NetInfo info = PrefabCollection<NetInfo>.FindLoaded("Large Road");
			FastList<ushort> fastList = new FastList<ushort>();
			NetManager netManager = Singleton<NetManager>.instance;
			NetNode[] buffer = netManager.m_nodes.m_buffer;
			uint size = netManager.m_nodes.m_size;
			for (uint num = 0u; num < size; num++)
			{
				if (buffer[num].m_flags != 0)
				{
					NetInfo info2 = buffer[num].Info;
					if (info2 == null || info2.m_netAI == null)
					{
						fastList.Add((ushort)num);
						buffer[num].Info = info;
					}
				}
			}
			Util.DebugPrint("Found", fastList.m_size, "bad net nodes");
			return fastList;
		}

		private static FastList<ushort> GetBadSegments()
		{
			NetInfo info = PrefabCollection<NetInfo>.FindLoaded("Large Road");
			FastList<ushort> fastList = new FastList<ushort>();
			NetManager netManager = Singleton<NetManager>.instance;
			NetSegment[] buffer = netManager.m_segments.m_buffer;
			uint size = netManager.m_segments.m_size;
			for (uint num = 0u; num < size; num++)
			{
				if (buffer[num].m_flags != 0)
				{
					NetInfo info2 = buffer[num].Info;
					if (info2 == null || info2.m_netAI == null)
					{
						fastList.Add((ushort)num);
						buffer[num].Info = info;
					}
				}
			}
			Util.DebugPrint("Found", fastList.m_size, "bad net segments");
			return fastList;
		}

		private static void RemoveBadNodes(FastList<ushort> badNodes)
		{
			NetManager netManager = Singleton<NetManager>.instance;
			NetNode[] buffer = netManager.m_nodes.m_buffer;
			int num = 0;
			foreach (ushort badNode in badNodes)
			{
				if (buffer[badNode].m_flags != 0)
				{
					try
					{
						netManager.ReleaseNode(badNode);
						num++;
					}
					catch (Exception exception)
					{
						Util.DebugPrint("Cannot remove net node", badNode);
						Debug.LogException(exception);
					}
				}
			}
			Util.DebugPrint("Removed", num, "bad net nodes");
		}

		private static void RemoveBadSegments(FastList<ushort> badSegments)
		{
			NetManager netManager = Singleton<NetManager>.instance;
			NetSegment[] buffer = netManager.m_segments.m_buffer;
			int num = 0;
			foreach (ushort badSegment in badSegments)
			{
				if (buffer[badSegment].m_flags != 0)
				{
					try
					{
						netManager.ReleaseSegment(badSegment, keepNodes: false);
						num++;
					}
					catch (Exception exception)
					{
						Util.DebugPrint("Cannot remove net segment", badSegment);
						Debug.LogException(exception);
					}
				}
			}
			Util.DebugPrint("Removed", num, "bad net segments");
		}

		private static void RemoveBadVehicles()
		{
			VehicleManager vehicleManager = Singleton<VehicleManager>.instance;
			Vehicle[] buffer = vehicleManager.m_vehicles.m_buffer;
			uint size = vehicleManager.m_vehicles.m_size;
			uint num = 0u;
			for (uint num2 = 0u; num2 < size; num2++)
			{
				if (buffer[num2].m_flags == (Vehicle.Flags)0)
				{
					continue;
				}
				VehicleInfo info = buffer[num2].Info;
				if (info == null || info.m_vehicleAI == null)
				{
					try
					{
						vehicleManager.ReleaseVehicle((ushort)num2);
						num++;
					}
					catch (Exception exception)
					{
						Util.DebugPrint("Cannot remove vehicle", num2);
						Debug.LogException(exception);
					}
				}
			}
			Util.DebugPrint("Removed", num, "bad vehicles");
			VehicleParked[] buffer2 = vehicleManager.m_parkedVehicles.m_buffer;
			size = vehicleManager.m_parkedVehicles.m_size;
			num = 0u;
			for (uint num3 = 0u; num3 < size; num3++)
			{
				if (buffer2[num3].m_flags == 0)
				{
					continue;
				}
				VehicleInfo info2 = buffer2[num3].Info;
				if (info2 == null || info2.m_vehicleAI == null)
				{
					try
					{
						vehicleManager.ReleaseParkedVehicle((ushort)num3);
						num++;
					}
					catch (Exception exception2)
					{
						Util.DebugPrint("Cannot remove parked vehicle", num3);
						Debug.LogException(exception2);
					}
				}
			}
			Util.DebugPrint("Removed", num, "bad parked vehicles");
		}

		internal static IEnumerator Removals()
		{
			AsyncAction task = Singleton<SimulationManager>.instance.AddAction(RemoveNow);
			while (!task.completedOrFailed)
			{
				yield return null;
			}
		}

		private static void RemoveNow()
		{
			Util.DebugPrint("Removing starts at", Profiling.Millis);
			if (Settings.settings.removeVehicles)
			{
				RemoveVehicles();
			}
			if (Settings.settings.removeCitizenInstances)
			{
				RemoveCitizenInstances();
			}
			Util.DebugPrint("Removing finished at", Profiling.Millis);
		}

		private static void RemoveVehicles()
		{
			try
			{
				int num = ForVehicles(delegate (uint i, ref Vehicle d)
				{
					Singleton<VehicleManager>.instance.ReleaseVehicle((ushort)i);
				});
				Util.DebugPrint("Removed", num, "vehicles");
				num = ForParkedVehicles(delegate (uint i, ref VehicleParked d)
				{
					Singleton<VehicleManager>.instance.ReleaseParkedVehicle((ushort)i);
				});
				Util.DebugPrint("Removed", num, "parked vehicles");
				ushort[] vehicleGrid = Singleton<VehicleManager>.instance.m_vehicleGrid;
				ushort[] vehicleGrid2 = Singleton<VehicleManager>.instance.m_vehicleGrid2;
				ushort[] parkedGrid = Singleton<VehicleManager>.instance.m_parkedGrid;
				for (int j = 0; j < vehicleGrid.Length; j++)
				{
					vehicleGrid[j] = 0;
				}
				for (int k = 0; k < vehicleGrid2.Length; k++)
				{
					vehicleGrid2[k] = 0;
				}
				for (int l = 0; l < parkedGrid.Length; l++)
				{
					parkedGrid[l] = 0;
				}
				ForCitizens(delegate (uint i, ref Citizen d)
				{
					d.SetVehicle(i, 0, 0u);
					d.SetParkedVehicle(i, 0);
				});
				ForBuildings(delegate (uint i, ref Building d)
				{
					d.m_ownVehicles = 0;
					d.m_guestVehicles = 0;
				});
				ForTransportLines(delegate (uint i, ref TransportLine d)
				{
					d.m_vehicles = 0;
				});
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static void RemoveCitizenInstances()
		{
			try
			{
				int num = ForCitizenInstances(delegate (uint i, ref CitizenInstance d)
				{
					Singleton<CitizenManager>.instance.ReleaseCitizenInstance((ushort)i);
				});
				Util.DebugPrint("Removed", num, "citizens instances");
				ushort[] citizenGrid = Singleton<CitizenManager>.instance.m_citizenGrid;
				for (int j = 0; j < citizenGrid.Length; j++)
				{
					citizenGrid[j] = 0;
				}
				ForCitizens(delegate (uint i, ref Citizen d)
				{
					d.m_instance = 0;
				});
				ForBuildings(delegate (uint i, ref Building d)
				{
					d.m_sourceCitizens = 0;
					d.m_targetCitizens = 0;
				});
				ForNetNodes(delegate (uint i, ref NetNode d)
				{
					d.m_targetCitizens = 0;
				});
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		private static int ForVehicles(ARef<Vehicle> action)
		{
			Vehicle[] buffer = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if (buffer[num2].m_flags != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForParkedVehicles(ARef<VehicleParked> action)
		{
			VehicleParked[] buffer = Singleton<VehicleManager>.instance.m_parkedVehicles.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if (buffer[num2].m_flags != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForCitizenInstances(ARef<CitizenInstance> action)
		{
			CitizenInstance[] buffer = Singleton<CitizenManager>.instance.m_instances.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if ((buffer[num2].m_flags & CitizenInstance.Flags.Created) != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForCitizens(ARef<Citizen> action)
		{
			Citizen[] buffer = Singleton<CitizenManager>.instance.m_citizens.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if ((buffer[num2].m_flags & Citizen.Flags.Created) != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForBuildings(ARef<Building> action)
		{
			Building[] buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if (buffer[num2].m_flags != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForNetNodes(ARef<NetNode> action)
		{
			NetNode[] buffer = Singleton<NetManager>.instance.m_nodes.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if (buffer[num2].m_flags != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}

		private static int ForTransportLines(ARef<TransportLine> action)
		{
			TransportLine[] buffer = Singleton<TransportManager>.instance.m_lines.m_buffer;
			int num = 0;
			for (uint num2 = 1u; num2 < buffer.Length; num2++)
			{
				if (buffer[num2].m_flags != 0)
				{
					try
					{
						action(num2, ref buffer[num2]);
						num++;
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			return num;
		}
	}
}
