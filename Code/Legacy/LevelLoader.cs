using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoadingScreenMod
{
	public sealed class LevelLoader : DetourUtility<LevelLoader>
	{
		public string cityName;

		private readonly HashSet<string> knownFailedAssets = new HashSet<string>();

		private readonly Dictionary<string, bool> knownFastLoads = new Dictionary<string, bool>(2);

		private readonly HashSet<string>[] skippedPrefabs = new HashSet<string>[3];

		internal readonly int[] skipCounts = new int[3];

		internal object loadingLock;

		internal Queue<IEnumerator> mainThreadQueue;

		private DateTime fullLoadTime;

		private DateTime savedSkipStamp;

		private int startMillis;

		internal bool simulationFailed;

		private bool fastLoad;

		internal bool optimizeThumbs;

		internal bool assetsStarted;

		internal bool assetsFinished;

		private KeyValuePair<string, int>[] levelStrings = new KeyValuePair<string, int>[20]
		{
			new KeyValuePair<string, int>("FootballPrefabs", 456200),
			new KeyValuePair<string, int>("Football2Prefabs", 525940),
			new KeyValuePair<string, int>("Football3Prefabs", 526610),
			new KeyValuePair<string, int>("Football4Prefabs", 526611),
			new KeyValuePair<string, int>("Football5Prefabs", 526612),
			new KeyValuePair<string, int>("Station1Prefabs", 547501),
			new KeyValuePair<string, int>("Station2Prefabs", 614582),
			new KeyValuePair<string, int>("Station3Prefabs", 715193),
			new KeyValuePair<string, int>("Station4Prefabs", 815380),
			new KeyValuePair<string, int>("Station5Prefabs", 944070),
			new KeyValuePair<string, int>("Station6Prefabs", 1065490),
			new KeyValuePair<string, int>("Station7Prefabs", 1065491),
			new KeyValuePair<string, int>("Station8Prefabs", 1148021),
			new KeyValuePair<string, int>("Station9Prefabs", 1196100),
			new KeyValuePair<string, int>("Station10Prefabs", 1531472),
			new KeyValuePair<string, int>("Station11Prefabs", 1531473),
			new KeyValuePair<string, int>("FestivalPrefabs", 614581),
			new KeyValuePair<string, int>("ChristmasPrefabs", 715192),
			new KeyValuePair<string, int>("ModderPack1Prefabs", 515190),
			new KeyValuePair<string, int>("ModderPack2Prefabs", 547500)
		};

		private KeyValuePair<string, int>[] levelStringsAiportDLC = new KeyValuePair<string, int>[3]
		{
			new KeyValuePair<string, int>("Station12Prefabs", 1726383),
			new KeyValuePair<string, int>("Station13Prefabs", 1726384),
			new KeyValuePair<string, int>("ModderPack10Prefabs", 1726381)
		};

		internal void SetSkippedPrefabs(HashSet<string>[] prefabs)
		{
			prefabs.CopyTo(skippedPrefabs, 0);
		}

		internal static bool AssetsActive()
		{
			if (Instance<AssetLoader>.HasInstance && Instance<LevelLoader>.instance.assetsStarted)
			{
				return !Instance<LevelLoader>.instance.assetsFinished;
			}
			return false;
		}

		internal bool HasFailed(string fullName)
		{
			return knownFailedAssets.Contains(fullName);
		}

		internal bool AddFailed(string fullName)
		{
			return knownFailedAssets.Add(fullName);
		}

		internal void Reset()
		{
			knownFailedAssets.Clear();
			knownFastLoads.Clear();
			Array.Clear(skipCounts, 0, skipCounts.Length);
		}

		internal override void Dispose()
		{
			base.Dispose();
			Reset();
			Array.Clear(skippedPrefabs, 0, skippedPrefabs.Length);
		}

		public IEnumerator LoadLevelCoroutine(Package.Asset asset, string playerScene, string uiScene, SimulationMetaData ngs, bool forceEnvironmentReload)
		{
			LoadingManager lm = Singleton<LoadingManager>.instance;
			int i = 0;
			yield return null;
			try
			{
				Util.InvokeVoid(lm, "PreLoadLevel");
			}
			catch (Exception exception)
			{
				Util.DebugPrint("PreLoadLevel: exception from some mod.");
				Debug.LogException(exception);
			}
			if (!lm.LoadingAnimationComponent.AnimationLoaded)
			{
				lm.m_loadingProfilerScenes.BeginLoading("LoadingAnimation");
				yield return SceneManager.LoadSceneAsync("LoadingAnimation", LoadSceneMode.Additive);
				lm.m_loadingProfilerScenes.EndLoading();
			}
			DateTime skipStamp = Settings.settings.LoadSkipFile();
			AsyncTask task = (AsyncTask)(LoadSaveStatus.activeTask = Singleton<SimulationManager>.instance.AddAction("Loading", (IEnumerator)Util.Invoke(lm, "LoadSimulationData", asset, ngs)));
			if (lm.m_loadedEnvironment == null)
			{
				fastLoad = false;
			}
			else
			{
				while (!lm.m_metaDataLoaded && !task.completedOrFailed)
				{
					yield return null;
				}
				if (Singleton<SimulationManager>.instance.m_metaData == null)
				{
					Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData();
					Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
					Singleton<SimulationManager>.instance.m_metaData.Merge(ngs);
				}
				Util.InvokeVoid(lm, "MetaDataLoaded");
				string text = Singleton<SimulationManager>.instance.m_metaData.m_MapThemeMetaData?.name;
				fastLoad = Singleton<SimulationManager>.instance.m_metaData.m_environment == lm.m_loadedEnvironment && text == lm.m_loadedMapTheme && !forceEnvironmentReload;
				if (fastLoad)
				{
					if (Settings.settings.loadUsed && !IsKnownFastLoad(asset))
					{
						while (!IsSaveDeserialized())
						{
							yield return null;
						}
						fastLoad = AllAssetsAvailable();
					}
					if (fastLoad)
					{
						if (skipStamp != savedSkipStamp)
						{
							fastLoad = false;
						}
						else if (Settings.settings.SkipPrefabs && !IsKnownFastLoad(asset))
						{
							while (!IsSaveDeserialized())
							{
								yield return null;
							}
							fastLoad = AllPrefabsAvailable();
						}
					}
					if (fastLoad)
					{
						if (Settings.settings.SkipPrefabs && skippedPrefabs[0] != null)
						{
							while (!IsSaveDeserialized())
							{
								yield return null;
							}
							Instance<PrefabLoader>.Create().SetSkippedPrefabs(skippedPrefabs);
							lm.QueueLoadingAction(PrefabLoader.RemoveSkippedFromSimulation());
						}
						lm.QueueLoadingAction((IEnumerator)Util.Invoke(lm, "EssentialScenesLoaded"));
						lm.QueueLoadingAction((IEnumerator)Util.Invoke(lm, "RenderDataReady"));
						Util.DebugPrint("fast load at", Profiling.Millis);
					}
					else
					{
						DestroyLoadedPrefabs();
						lm.m_loadedEnvironment = null;
						lm.m_loadedMapTheme = null;
						Util.DebugPrint("fallback to full load at", Profiling.Millis);
					}
				}
				else
				{
					Util.InvokeVoid(lm, "DestroyAllPrefabs");
					lm.m_loadedEnvironment = null;
					lm.m_loadedMapTheme = null;
					Util.DebugPrint("full load at", Profiling.Millis);
				}
			}
			if (lm.m_loadedEnvironment == null)
			{
				Reset();
				fullLoadTime = DateTime.Now;
				savedSkipStamp = skipStamp;
				Array.Clear(skippedPrefabs, 0, skippedPrefabs.Length);
				loadingLock = Util.Get(lm, "m_loadingLock");
				mainThreadQueue = (Queue<IEnumerator>)Util.Get(lm, "m_mainThreadQueue");
				if (!string.IsNullOrEmpty(playerScene))
				{
					lm.m_loadingProfilerScenes.BeginLoading(playerScene);
					AsyncOperation op2 = SceneManager.LoadSceneAsync(playerScene, LoadSceneMode.Single);
					while (!op2.isDone)
					{
						lm.SetSceneProgress(op2.progress * 0.01f);
						yield return null;
					}
					lm.m_loadingProfilerScenes.EndLoading();
				}
				while (!lm.m_metaDataLoaded && !task.completedOrFailed)
				{
					yield return null;
				}
				if (Singleton<SimulationManager>.instance.m_metaData == null)
				{
					Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData();
					Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
					Singleton<SimulationManager>.instance.m_metaData.Merge(ngs);
				}
				try
				{
					Util.InvokeVoid(lm, "MetaDataLoaded");
				}
				catch (Exception exception2)
				{
					Util.DebugPrint("OnCreated: exception from some mod.");
					Debug.LogException(exception2);
				}
				if (Settings.settings.SkipPrefabs)
				{
					Instance<PrefabLoader>.Create().Deploy();
				}
				KeyValuePair<string, float>[] levels = SetLevels();
				float currentProgress = 0.1f;
				string key;
				for (i = 0; i < levels.Length; i++)
				{
					key = levels[i].Key;
					lm.m_loadingProfilerScenes.BeginLoading(key);
					AsyncOperation op2 = SceneManager.LoadSceneAsync(key, LoadSceneMode.Additive);
					while (!op2.isDone)
					{
						lm.SetSceneProgress(currentProgress + op2.progress * (levels[i].Value - currentProgress));
						yield return null;
					}
					lm.m_loadingProfilerScenes.EndLoading();
					currentProgress = levels[i].Value;
				}
				Instance<PrefabLoader>.instance?.Revert();
				if (Settings.settings.SkipPrefabs)
				{
					lm.QueueLoadingAction(PrefabLoader.RemoveSkippedFromSimulation());
				}
				do
				{
					yield return null;
					yield return null;
					lock (loadingLock)
					{
						i = mainThreadQueue.Count;
					}
				}
				while (i > 0);
				Instance<AssetLoader>.Create().Setup();
				lm.QueueLoadingAction(Instance<AssetLoader>.instance.LoadCustomContent());
				if (Settings.settings.recover)
				{
					lm.QueueLoadingAction(Safenets.Setup());
				}
				RenderManager.Managers_CheckReferences();
				lm.QueueLoadingAction((IEnumerator)Util.Invoke(lm, "EssentialScenesLoaded"));
				RenderManager.Managers_InitRenderData();
				lm.QueueLoadingAction((IEnumerator)Util.Invoke(lm, "RenderDataReady"));
				simulationFailed = HasFailed(task);
				while (!assetsFinished)
				{
					yield return null;
				}
				key = Singleton<SimulationManager>.instance.m_metaData.m_environment + "Properties";
				if (!string.IsNullOrEmpty(key))
				{
					lm.m_loadingProfilerScenes.BeginLoading(key);
					AsyncOperation op2 = SceneManager.LoadSceneAsync(key, LoadSceneMode.Additive);
					while (!op2.isDone)
					{
						lm.SetSceneProgress(0.85f + op2.progress * 0.05f);
						if (optimizeThumbs)
						{
							Instance<CustomDeserializer>.instance.ReceiveAvailable();
						}
						yield return null;
					}
					lm.m_loadingProfilerScenes.EndLoading();
				}
				if (!simulationFailed)
				{
					simulationFailed = HasFailed(task);
				}
				if (!string.IsNullOrEmpty(uiScene))
				{
					lm.m_loadingProfilerScenes.BeginLoading(uiScene);
					AsyncOperation op2 = SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
					while (!op2.isDone)
					{
						lm.SetSceneProgress(0.9f + op2.progress * 0.08f);
						if (optimizeThumbs)
						{
							Instance<CustomDeserializer>.instance.ReceiveAvailable();
						}
						yield return null;
					}
					lm.m_loadingProfilerScenes.EndLoading();
				}
				lm.m_loadedEnvironment = Singleton<SimulationManager>.instance.m_metaData.m_environment;
				lm.m_loadedMapTheme = Singleton<SimulationManager>.instance.m_metaData.m_MapThemeMetaData?.name;
				if (optimizeThumbs)
				{
					Instance<CustomDeserializer>.instance.ReceiveRemaining();
				}
			}
			else
			{
				string text2 = (string)Util.Invoke(lm, "GetLoadingScene");
				if (!string.IsNullOrEmpty(text2))
				{
					lm.m_loadingProfilerScenes.BeginLoading(text2);
					yield return SceneManager.LoadSceneAsync(text2, LoadSceneMode.Additive);
					lm.m_loadingProfilerScenes.EndLoading();
				}
			}
			lm.SetSceneProgress(1f);
			while (!task.completedOrFailed)
			{
				if (!simulationFailed && (i++ & 7) == 0)
				{
					simulationFailed = HasFailed(task);
				}
				yield return null;
			}
			if (!simulationFailed)
			{
				simulationFailed = HasFailed(task);
			}
			lm.m_simulationDataLoaded = lm.m_metaDataLoaded;
			(Util.Get(lm, "m_simulationDataReady") as LoadingManager.SimulationDataReadyHandler)?.Invoke();
			SimulationManager.UpdateMode updateMode = SimulationManager.UpdateMode.Undefined;
			if (ngs != null)
			{
				updateMode = ngs.m_updateMode;
			}
			lm.QueueLoadingAction(CheckPolicies());
			if (Settings.settings.Removals)
			{
				lm.QueueLoadingAction(Safenets.Removals());
			}
			lm.QueueLoadingAction((IEnumerator)Util.Invoke(lm, "LoadLevelComplete", updateMode));
			Instance<PrefabLoader>.instance?.Dispose();
			lm.QueueLoadingAction(LoadingComplete());
			knownFastLoads[asset.checksum] = true;
			AssetLoader.PrintMem();
		}

		private IEnumerator LoadingComplete()
		{
			Singleton<LoadingManager>.instance.LoadingAnimationComponent.enabled = false;
			AssetLoader.PrintMem();
			Instance<AssetLoader>.instance?.Dispose();
			Instance<Fixes>.instance.Dispose();
			Instance<CustomDeserializer>.instance.Dispose();
			Util.DebugPrint("All completed at", Profiling.Millis);
			Profiling.Stop();
			yield break;
		}

		private IEnumerator CheckPolicies()
		{
			PoliciesPanel policiesPanel = ToolsModifierControl.policiesPanel;
			if (policiesPanel != null)
			{
				if (!(bool)Util.Get(policiesPanel, "m_Initialized"))
				{
					Util.DebugPrint("PoliciesPanel not initialized yet. Initializing at", Profiling.Millis);
					try
					{
						Util.InvokeVoid(policiesPanel, "RefreshPanel");
					}
					catch (Exception exception)
					{
						Debug.LogException(exception);
					}
				}
			}
			else
			{
				Util.DebugPrint("PoliciesPanel is null. Cannot initialize it at", Profiling.Millis);
			}
			yield break;
		}

		private KeyValuePair<string, float>[] SetLevels()
		{
			LoadingManager loadingManager = Singleton<LoadingManager>.instance;
			loadingManager.m_supportsExpansion[0] = Check(369150);
			loadingManager.m_supportsExpansion[1] = Check(420610);
			loadingManager.m_supportsExpansion[2] = Check(515191);
			loadingManager.m_supportsExpansion[3] = Check(547502);
			loadingManager.m_supportsExpansion[4] = Check(614580);
			loadingManager.m_supportsExpansion[5] = Check(715191);
			loadingManager.m_supportsExpansion[6] = Check(715194);
			loadingManager.m_supportsExpansion[7] = Check(944071);
			loadingManager.m_supportsExpansion[8] = Check(1146930);
			loadingManager.m_supportsExpansion[9] = Check(1726380);
			bool flag = Singleton<SimulationManager>.instance.m_metaData.m_environment == "Winter";
			if (flag && !loadingManager.m_supportsExpansion[1])
			{
				Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
				flag = false;
			}
			List<KeyValuePair<string, float>> list = new List<KeyValuePair<string, float>>(20);
			string text = (string)Util.Invoke(loadingManager, "GetLoadingScene");
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(new KeyValuePair<string, float>(text, 0.015f));
			}
			list.Add(new KeyValuePair<string, float>(Singleton<SimulationManager>.instance.m_metaData.m_environment + "Prefabs", 0.12f));
			if ((bool)Util.Invoke(loadingManager, "LoginUsed"))
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterLoginPackPrefabs" : "LoginPackPrefabs", 0.121f));
			}
			list.Add(new KeyValuePair<string, float>(flag ? "WinterPreorderPackPrefabs" : "PreorderPackPrefabs", 0.122f));
			list.Add(new KeyValuePair<string, float>(flag ? "WinterSignupPackPrefabs" : "SignupPackPrefabs", 0.123f));
			if (Check(346791))
			{
				list.Add(new KeyValuePair<string, float>("DeluxePackPrefabs", 0.124f));
			}
			if (PlatformService.IsAppOwned(238370u))
			{
				list.Add(new KeyValuePair<string, float>("MagickaPackPrefabs", 0.125f));
			}
			if (loadingManager.m_supportsExpansion[0])
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterExpansion1Prefabs" : "Expansion1Prefabs", 0.126f));
			}
			if (loadingManager.m_supportsExpansion[1])
			{
				list.Add(new KeyValuePair<string, float>("Expansion2Prefabs", 0.127f));
			}
			if (loadingManager.m_supportsExpansion[2])
			{
				list.Add(new KeyValuePair<string, float>("Expansion3Prefabs", 0.128f));
			}
			if (loadingManager.m_supportsExpansion[3])
			{
				list.Add(new KeyValuePair<string, float>("Expansion4Prefabs", 0.129f));
			}
			if (loadingManager.m_supportsExpansion[4])
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterExpansion5Prefabs" : "Expansion5Prefabs", 0.13f));
			}
			if (loadingManager.m_supportsExpansion[5])
			{
				list.Add(new KeyValuePair<string, float>(Singleton<SimulationManager>.instance.m_metaData.m_environment + "Expansion6Prefabs", 0.131f));
			}
			if (loadingManager.m_supportsExpansion[6])
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterExpansion7Prefabs" : "Expansion7Prefabs", 0.132f));
			}
			if (loadingManager.m_supportsExpansion[7])
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterExpansion8Prefabs" : "Expansion8Prefabs", 0.1325f));
			}
			if (loadingManager.m_supportsExpansion[8])
			{
				list.Add(new KeyValuePair<string, float>(flag ? "WinterExpansion9Prefabs" : "Expansion9Prefabs", 0.133f));
			}
			if (loadingManager.m_supportsExpansion[9])
			{
				list.Add(new KeyValuePair<string, float>("Expansion10Prefabs", 0.1335f));
			}
			for (int i = 0; i < levelStrings.Length; i++)
			{
				if (Check(levelStrings[i].Value))
				{
					list.Add(new KeyValuePair<string, float>(levelStrings[i].Key, 0.134f + (float)i * 0.01f / (float)levelStrings.Length));
				}
			}
			if (Check(715190))
			{
				Package.Asset asset = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanSuburbiaStyleName);
				if (asset != null && asset.isEnabled)
				{
					list.Add(new KeyValuePair<string, float>("ModderPack3Prefabs", 0.144f));
				}
			}
			if (Check(1059820))
			{
				list.Add(new KeyValuePair<string, float>("ModderPack4Prefabs", 0.145f));
			}
			if (Check(1148020))
			{
				Package.Asset asset2 = PackageManager.FindAssetByName("System." + DistrictStyle.kModderPack5StyleName);
				if (asset2 != null && asset2.isEnabled)
				{
					list.Add(new KeyValuePair<string, float>("ModderPack5Prefabs", 0.1455f));
				}
			}
			if (Check(1148022))
			{
				list.Add(new KeyValuePair<string, float>("ModderPack6Prefabs", 0.146f));
			}
			if (Check(1531470))
			{
				list.Add(new KeyValuePair<string, float>("ModderPack7Prefabs", 0.1462f));
			}
			if (Check(1531471))
			{
				list.Add(new KeyValuePair<string, float>("ModderPack8Prefabs", 0.1464f));
			}

			// Modder Pack 9 is Content Creator Pack: Map Pack
			for (int j = 0; j < levelStringsAiportDLC.Length; j++)
			{
				if (Check(levelStringsAiportDLC[j].Value))
				{
					list.Add(new KeyValuePair<string, float>(levelStringsAiportDLC[j].Key, 0.1464f + (float)j * 0.00019f / (float)levelStringsAiportDLC.Length));
				}
			}
			if (Check(563850))
			{
				list.Add(new KeyValuePair<string, float>("ChinaPackPrefabs", 0.1466f));
			}
			Package.Asset asset3 = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanStyleName);
			if (asset3 != null && asset3.isEnabled)
			{
				list.Add(new KeyValuePair<string, float>(Singleton<SimulationManager>.instance.m_metaData.m_environment.Equals("Europe") ? "EuropeNormalPrefabs" : "EuropeStylePrefabs", 0.15f));
			}
			return list.ToArray();
		}

		internal static bool Check(int dlc)
		{
			if (SteamHelper.IsDLCOwned((SteamHelper.DLC)dlc))
			{
				if (Settings.settings.SkipPrefabs)
				{
					return !Settings.settings.SkipMatcher.Matches(dlc);
				}
				return true;
			}
			return false;
		}

		private bool IsKnownFastLoad(Package.Asset asset)
		{
			if (knownFastLoads.TryGetValue(asset.checksum, out var value))
			{
				return value;
			}
			try
			{
				value = fullLoadTime < asset.package.Find(asset.package.packageMainAsset).Instantiate<SaveGameMetaData>().timeStamp;
				knownFastLoads[asset.checksum] = value;
				return value;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			return false;
		}

		private bool HasFailed(AsyncTask simulationTask)
		{
			if (simulationTask.failed)
			{
				try
				{
					Exception[] array = ((Queue<Exception>)Util.GetStatic(typeof(UIView), "sLastException")).ToArray();
					string message = null;
					if (array.Length != 0)
					{
						message = array[array.Length - 1].Message;
					}
					Instance<LoadingScreen>.instance.SimulationSource?.Failed(message);
					return true;
				}
				catch (Exception exception)
				{
					Debug.LogException(exception);
				}
				return false;
			}
			return false;
		}

		internal bool IsSaveDeserialized()
		{
			if (startMillis == 0)
			{
				startMillis = Profiling.Millis;
			}
			bool num = Profiling.Millis - startMillis > 12000 || GetSimProgress() > 54;
			if (num)
			{
				startMillis = 0;
			}
			return num;
		}

		internal static int GetSimProgress()
		{
			try
			{
				return Thread.VolatileRead(ref ProfilerSource.GetEvents(Singleton<LoadingManager>.instance.m_loadingProfilerSimulation).m_size);
			}
			catch (Exception)
			{
			}
			return -1;
		}

		private bool AllAssetsAvailable()
		{
			try
			{
				return Instance<UsedAssets>.Create().AllAssetsAvailable(knownFailedAssets);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			return true;
		}

		private bool AllPrefabsAvailable()
		{
			try
			{
				Instance<PrefabLoader>.Create().LookupSimulationPrefabs();
				return Instance<PrefabLoader>.instance.AllPrefabsAvailable();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			return true;
		}

		private static void DestroyLoadedPrefabs()
		{
			DestroyLoaded<NetInfo>();
			DestroyLoaded<BuildingInfo>();
			DestroyLoaded<PropInfo>();
			DestroyLoaded<TreeInfo>();
			DestroyLoaded<TransportInfo>();
			DestroyLoaded<VehicleInfo>();
			DestroyLoaded<CitizenInfo>();
			DestroyLoaded<EventInfo>();
			DestroyLoaded<DisasterInfo>();
			DestroyLoaded<RadioContentInfo>();
			DestroyLoaded<RadioChannelInfo>();
		}

		private static void DestroyLoaded<P>() where P : PrefabInfo
		{
			try
			{
				int num = PrefabCollection<P>.LoadedCount();
				List<P> list = new List<P>(num);
				for (int i = 0; i < num; i++)
				{
					P loaded = PrefabCollection<P>.GetLoaded((uint)i);
					if ((UnityEngine.Object)loaded != (UnityEngine.Object)null)
					{
						loaded.m_prefabDataIndex = -1;
						list.Add(loaded);
					}
				}
				PrefabCollection<P>.DestroyPrefabs(string.Empty, list.ToArray(), null);
				if (num != list.Count)
				{
					Util.Set(Util.GetStatic(typeof(PrefabCollection<P>), "m_scenePrefabs"), "m_size", 0, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				object @static = Util.GetStatic(typeof(PrefabCollection<P>), "m_prefabDict");
				@static.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public).Invoke(@static, null);
				list.Clear();
				list.Capacity = 0;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}
}
