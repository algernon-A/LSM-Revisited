using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	/// <summary>
	/// Custom level loader to implement mod loading.
	/// </summary>
    public static class LevelLoader
	{
		// Used by PrefabLoader.
		internal static readonly int[] skipCounts = new int[3];
		internal static object loadingLock;
		internal static Queue<IEnumerator> mainThreadQueue;

		// Used by AssetLoader and DualProfilerSource to flag finishing of asset loading..
		internal static bool assetsFinished;

		// Used by reports.
		internal static string cityName;

		// Referred to from loading patch.
		internal static bool assetLoadingStarted;
		internal static bool optimizeThumbs;
		internal static bool simulationFailed;


		// Skipped prefab lists (names by category).
		private static readonly HashSet<string>[] skippedPrefabs = new HashSet<string>[3];

		// Failed assets.
		private static readonly HashSet<string> knownFailedAssets = new HashSet<string>();

		// Known savegames and fast-load eligibility.
		private static readonly Dictionary<string, bool> knownFastLoads = new Dictionary<string, bool>(2);

		// Last loaded skipped prefabs timestamp.
		private static DateTime savedSkipTimestamp;

		// Timestamp of last full load (for fast load eligibility checking).
		private static DateTime fullLoadTimestamp;

		// Fast load status.
		private static bool fastLoadEligible;

		// For timeout check.
		private static int startMillis;


		/// <summary>
		/// The level loading coroutine itself - everything runs through here.
		/// </summary>
		/// <param name="loadingManager">LoadingManager instance</param>
		/// <param name="savegame">Save to load</param>
		/// <param name="playerScene">Unity player scene</param>
		/// <param name="uiScene">Unity UI scene</param>
		/// <param name="ngs">Simulation metadata</param>
		/// <param name="forceEnvironmentReload">True to force a reload of the environment, false otherwise</param>
		/// <returns></returns>
		public static IEnumerator LoadLevelCoroutine(LoadingManager loadingManager, Package.Asset savegame, string playerScene, string uiScene, SimulationMetaData ngs, bool forceEnvironmentReload)
		{
			Logging.KeyMessage("starting LoadLevelCoroutine at ", Profiling.Millis);

			// LSM.
			int i = 0;

			// Gamecode.
			yield return null;

			// Game SetSceneProgress(0f);

			// Try LoadingScreen private methods.
			AsyncTask task;
			try
			{
				Util.InvokeVoid(loadingManager, "PreLoadLevel");
				task = (AsyncTask)(LoadSaveStatus.activeTask = Singleton<SimulationManager>.instance.AddAction("Loading", (IEnumerator)Util.Invoke(loadingManager, "LoadSimulationData", savegame, ngs)));
			}
			catch (Exception e)
			{
				Logging.LogException(e, "couldn't invoke LoadingManager private method");
				yield break;
			}

			// Base scene loading. 
			if (!loadingManager.LoadingAnimationComponent.AnimationLoaded)
			{
				loadingManager.m_loadingProfilerScenes.BeginLoading("LoadingAnimation");
				yield return SceneManager.LoadSceneAsync("LoadingAnimation", LoadSceneMode.Additive);
				loadingManager.m_loadingProfilerScenes.EndLoading();
			}

			// LSM.
			DateTime skipStamp = LoadingScreenMod.Settings.settings.LoadSkipFile();

			// LSM.
			if (loadingManager.m_loadedEnvironment == null)
			{
				fastLoadEligible = false;
			}
			// Gamecode.
			else
			{
				// Gamecode.
				while (!loadingManager.m_metaDataLoaded && !task.completedOrFailed)
				{
					yield return null;
				}

				// If metadata doesn't exist, then set reasonable defaults.
				if (Singleton<SimulationManager>.instance.m_metaData == null)
				{
					Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData();
					Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
					Singleton<SimulationManager>.instance.m_metaData.Merge(ngs);
				}
				Util.InvokeVoid(loadingManager, "MetaDataLoaded");
				string mapThemeName = Singleton<SimulationManager>.instance.m_metaData.m_MapThemeMetaData?.name;

				// LSM inserts fastload result to original check.
				fastLoadEligible = Singleton<SimulationManager>.instance.m_metaData.m_environment == loadingManager.m_loadedEnvironment && mapThemeName == loadingManager.m_loadedMapTheme && !forceEnvironmentReload;


				// The game is nicely optimized when loading from the pause menu. We must specifically address the following situations:
				// - environment (biome) stays the same
				// - map theme stays the same
				// - forceEnvironmentReload is false
				// - 'load used assets' is enabled
				// - not all assets and prefabs used in the save being loaded are currently in memory
				// - prefab skipping has changed.
				if (fastLoadEligible)
				{
					// Additional checks for fastload inserted by LSM.

					// Check if this savegame has known fastload status.
					bool isKnownFastLoad = IsKnownFastLoad(savegame);

					// Check custom asset availability, if this isn't already a known fastload candidate.
					if (LoadingScreenMod.Settings.settings.loadUsed && !isKnownFastLoad)
					{
						while (!IsSaveDeserialized)
						{
							yield return null;
						}
						fastLoadEligible = AllAssetsAvailable;
					}

					// Check prefab skipping changes and building prefab availability.
					if (fastLoadEligible)
					{
						// Check to see if prefab skipping has changed since the last load.
						if (skipStamp != savedSkipTimestamp)
						{
							// Yes - cancel fastload.
							fastLoadEligible = false;
						}

						// If we're skipping prefabs, then check that all prefabs are available.
						else if (LoadingScreenMod.Settings.settings.SkipPrefabs && !isKnownFastLoad)
						{
							// Wait until save is deserialized.
							while (!IsSaveDeserialized)
							{
								yield return null;
							}

							// All building prefabs need to be available for a fastload to occur.
							fastLoadEligible = AllBuildingPrefabsAvailable;
						}
					}

					// This is the actual fast load.
					if (fastLoadEligible)
					{
						// Skip any prefabs.
						if (LoadingScreenMod.Settings.settings.SkipPrefabs && skippedPrefabs[0] != null)
						{
							// Wait until save is deserialized.
							while (!IsSaveDeserialized)
							{
								yield return null;
							}

							// Implement skipping.
							Instance<PrefabLoader>.Create().SetSkippedPrefabs(skippedPrefabs);
							loadingManager.QueueLoadingAction(PrefabLoader.RemoveSkippedFromSimulation());
						}

						// Resume gamecode.

						loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "EssentialScenesLoaded"));
						loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "RenderDataReady"));

						// Legacy profiling.
						Logging.KeyMessage("commencing fast load at ", Profiling.Millis);
					}
					else
					{
						// Fastload requirements not met - fall back to full load by destroying all loaded prefabs and clearing the environment and map theme.

						// Notice that there is a race condition in the base game at this point: DestroyAllPrefabs ruins the simulation
						// if its deserialization has progressed far enough. Typically there is no problem.
						DestroyLoadedPrefabs();
						loadingManager.m_loadedEnvironment = null;
						loadingManager.m_loadedMapTheme = null;
						Logging.KeyMessage("falling back to full load at ", Profiling.Millis);
					}
				}
				// Resume Gamecode.
				else
				{
					// Full save load - start by destroying all loaded prefabs and clearing the environment and map theme.
					Util.InvokeVoid(loadingManager, "DestroyAllPrefabs");
					loadingManager.m_loadedEnvironment = null;
					loadingManager.m_loadedMapTheme = null;

					// Legacy profiling.
					Logging.KeyMessage("starting full load at ", Profiling.Millis);
				}
			}

			// Gamecode.
			if (loadingManager.m_loadedEnvironment == null)
			{
				// Start LSM insert.
				Reset();
				fullLoadTimestamp = DateTime.Now;
				savedSkipTimestamp = skipStamp;
				Array.Clear(skippedPrefabs, 0, skippedPrefabs.Length);
				loadingLock = Util.Get(loadingManager, "m_loadingLock");
				mainThreadQueue = (Queue<IEnumerator>)Util.Get(loadingManager, "m_mainThreadQueue");

				// Resume gamecode.
				if (!string.IsNullOrEmpty(playerScene))
				{
					loadingManager.m_loadingProfilerScenes.BeginLoading(playerScene);
					AsyncOperation op = SceneManager.LoadSceneAsync(playerScene, LoadSceneMode.Single);
					while (!op.isDone)
					{
						loadingManager.SetSceneProgress(op.progress * 0.01f);
						yield return null;
					}
					loadingManager.m_loadingProfilerScenes.EndLoading();
				}
				while (!loadingManager.m_metaDataLoaded && !task.completedOrFailed)
				{
					yield return null;
				}
				if (Singleton<SimulationManager>.instance.m_metaData == null)
				{
					Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData();
					Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
					Singleton<SimulationManager>.instance.m_metaData.Merge(ngs);
				}

				// Try private method.
				try
				{
					Util.InvokeVoid(loadingManager, "MetaDataLoaded");
				}
				catch (Exception e)
				{
					Logging.LogException(e, "exception invoking LoadingManager.MetaDataLoaded");
				}

				// LSM insert.
				if (LoadingScreenMod.Settings.settings.SkipPrefabs)
				{
					Instance<PrefabLoader>.Create().Deploy();
				}

				// LSM levels check.
				KeyValuePair<string, float>[] levels = SetLevels();

				// LSM progress is 0.1 vs game 0.
				// LSM skips game totalprogress.
				float currentProgress = 0.1f;
				string key;

				// LSM calls 'levels' what the game calls 'prefabScenes'.
				// LSM progress is already calculated by level, game calculates progress made against total.
				for (i = 0; i < levels.Length; i++)
				{
					key = levels[i].Key;
					// LSM skips progress update.

					loadingManager.m_loadingProfilerScenes.BeginLoading(key);
					AsyncOperation op = SceneManager.LoadSceneAsync(key, LoadSceneMode.Additive);
					while (!op.isDone)
					{
						// Different progress count by LSM.
						loadingManager.SetSceneProgress(currentProgress + op.progress * (levels[i].Value - currentProgress));
						yield return null;
					}
					loadingManager.m_loadingProfilerScenes.EndLoading();
					currentProgress = levels[i].Value;
				}

				// LSM inserts.
				Instance<PrefabLoader>.instance?.Revert();
				if (LoadingScreenMod.Settings.settings.SkipPrefabs)
				{
					loadingManager.QueueLoadingAction(PrefabLoader.RemoveSkippedFromSimulation());
				}

				// Some major mods (Network Extensions 1 & 2, Single Train Track, Metro Overhaul) have a race condition issue
				// in their NetInfo Installer. Everything goes fine if LoadCustomContent() below is NOT queued before the
				// said Installers have finished. This is just a workaround for the issue. The actual fix should be in
				// the Installers. Notice that the built-in loader of the game is also affected.
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

				// LSM equivalents of gamecode, just queing LSM loading actions.
				Instance<AssetLoader>.Create().Setup();
				loadingManager.QueueLoadingAction(Instance<AssetLoader>.instance.LoadCustomContent());

				// LSM Safenets insert.
				if (LoadingScreenMod.Settings.settings.recover)
				{
					loadingManager.QueueLoadingAction(Safenets.Setup());
				}
				RenderManager.Managers_CheckReferences();
				loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "EssentialScenesLoaded"));
				RenderManager.Managers_InitRenderData();
				loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "RenderDataReady"));

				// LSM insert.
				simulationFailed = HasTaskFailed(task);
				while (!assetsFinished)
				{
					yield return null;
				}

				// Resume gamecode.
				string propertiesScene = Singleton<SimulationManager>.instance.m_metaData.m_environment + "Properties";
				if (!string.IsNullOrEmpty(propertiesScene))
				{
					loadingManager.m_loadingProfilerScenes.BeginLoading(propertiesScene);
					AsyncOperation op = SceneManager.LoadSceneAsync(propertiesScene, LoadSceneMode.Additive);
					while (!op.isDone)
					{
						// LSM modifies constants herre.
						loadingManager.SetSceneProgress(0.85f + op.progress * 0.05f);

						// LSM insert.
						if (optimizeThumbs)
						{
							Instance<CustomDeserializer>.instance.ReceiveAvailable();
						}

						// Resume gamecode.
						yield return null;
					}
					loadingManager.m_loadingProfilerScenes.EndLoading();
				}

				// LSM insert.
				if (!simulationFailed)
				{
					simulationFailed = HasTaskFailed(task);
				}

				// Resume gamecode.
				if (!string.IsNullOrEmpty(uiScene))
				{
					loadingManager.m_loadingProfilerScenes.BeginLoading(uiScene);
					AsyncOperation op = SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
					while (!op.isDone)
					{
						// LSM modifies constants here.
						loadingManager.SetSceneProgress(0.9f + op.progress * 0.08f);

						// LSM insert.
						if (optimizeThumbs)
						{
							Instance<CustomDeserializer>.instance.ReceiveAvailable();
						}

						// Resume gamecode.
						yield return null;
					}
					loadingManager.m_loadingProfilerScenes.EndLoading();
				}
				loadingManager.m_loadedEnvironment = Singleton<SimulationManager>.instance.m_metaData.m_environment;
				loadingManager.m_loadedMapTheme = Singleton<SimulationManager>.instance.m_metaData.m_MapThemeMetaData?.name;

				// LSM insert.
				if (optimizeThumbs)
				{
					Instance<CustomDeserializer>.instance.ReceiveRemaining();
				}
				// End LSM insert.
			}
			else
			{
				string loadingScene2 = (string)Util.Invoke(loadingManager, "GetLoadingScene");
				if (!string.IsNullOrEmpty(loadingScene2))
				{
					loadingManager.m_loadingProfilerScenes.BeginLoading(loadingScene2);
					yield return SceneManager.LoadSceneAsync(loadingScene2, LoadSceneMode.Additive);
					loadingManager.m_loadingProfilerScenes.EndLoading();
				}
			}
			loadingManager.SetSceneProgress(1f);
			while (!task.completedOrFailed)
			{
				// LSM insert.
				if (!simulationFailed && (i++ & 7) == 0)
				{
					simulationFailed = HasTaskFailed(task);
				}

				// Resume gamecode.
				yield return null;
			}

			// LSM insert.
			if (!simulationFailed)
			{
				simulationFailed = HasTaskFailed(task);
			}

			// Resume gamecode.
			loadingManager.m_simulationDataLoaded = loadingManager.m_metaDataLoaded;

			// LSM equivalent of m_simulationDataReady call.
			(Util.Get(loadingManager, "m_simulationDataReady") as LoadingManager.SimulationDataReadyHandler)?.Invoke();

			SimulationManager.UpdateMode updateMode = SimulationManager.UpdateMode.Undefined;
			if (ngs != null)
			{
				updateMode = ngs.m_updateMode;
			}

			// LSM insert.
			loadingManager.QueueLoadingAction(CheckPolicies());
			if (LoadingScreenMod.Settings.settings.Removals)
			{
				loadingManager.QueueLoadingAction(Safenets.Removals());
			}

			// Resume gamecode.
			loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "LoadLevelComplete", updateMode));

			// Drop gamecode re PopsManager.

			// LSM inserts.
			Instance<PrefabLoader>.instance?.Dispose();
			loadingManager.QueueLoadingAction(LoadingComplete());

			// Add save to list of known fast loads.
			knownFastLoads[savegame.checksum] = true;
			AssetLoader.PrintMem();
		}

		//---- Refactored start



		/// <summary>
		/// Returns the current progress of simulation deserialization.
		/// Note: two threads at play here, old values of m_size might be cached for quite some time.
		/// </summary>
		/// <returns>Current simulation deserialization progress (-1 if error)</returns>
		private static int SimulationProgress
		{
			get
			{
				try
				{
					// Attempt to get progres from the loading manager profiler.
					return Thread.VolatileRead(ref ProfilerSource.GetEvents(Singleton<LoadingManager>.instance.m_loadingProfilerSimulation).m_size);
				}
				catch
				{
					// Don't care.
				}

				// If we got here, something went wrong; return -1.
				return -1;
			}
		}


		/// <summary>
		/// Checks to see if save deserialization has finished.
		/// </summary>
		internal static bool IsSaveDeserialized
		{
			get
			{
				{
					// Record start time if we haven't already.
					if (startMillis == 0)
					{
						startMillis = Profiling.Millis;
					}

					// Check for deserialization against simulation progress, with a 12-minute timeout.
					bool isDeserialized = SimulationProgress > 54 || Profiling.Millis - startMillis > 12000 ;

					// Reset start time if deserialization is complete.
					if (isDeserialized)
					{
						startMillis = 0;
					}

					return isDeserialized;
				}
			}
		}


		/// <summary>
		/// Returns true if if all required custom assets have been loaded (ignoring knwon failed assets).
		/// </summary>
		private static bool AllAssetsAvailable
		{
			get
			{
				try
				{
					// Check availability, ignoring known failed assets).
					return Instance<UsedAssets>.Create().AllAssetsAvailable(knownFailedAssets);
				}
				catch (Exception e)
				{
					Logging.LogException(e, "exception checking required custom asset loading status");
				}

				// If something went wrong, return true.
				return true;
			}
		}


		/// <summary>
		/// Returns true if all buiding prefabs have been loaded.
		/// </summary>
		private static bool AllBuildingPrefabsAvailable
		{
			get
			{
				try
				{
					// Check building prefab loading status.
					Instance<PrefabLoader>.Create().LookupSimulationPrefabs();
					return Instance<PrefabLoader>.instance.AllPrefabsAvailable();
				}
				catch (Exception e)
				{
					Logging.LogException(e, "exception checking required building prefab loading status");
				}

				// If something went wrong, return true.
				return true;
			}
		}


		/// <summary>
		/// Resets the level loader's data records.
		/// </summary>
		internal static void Reset()
		{
			// Clear lists.
			knownFailedAssets.Clear();
			knownFastLoads.Clear();

			// Clear skipped items.
			Array.Clear(skipCounts, 0, skipCounts.Length);
		}


		/// <summary>
		/// Sets the skipped prefabs lists to the provided value.
		/// </summary>
		/// <param name="prefabs">Array of skipped prefab lists to set</param>
		internal static void SetSkippedPrefabs(HashSet<string>[] prefabs)
		{
			prefabs.CopyTo(skippedPrefabs, 0);
		}


		/// <summary>
		/// Checks if asset loading is stil in progress.
		/// Called via LoadingScreen linesource (so is a method, not a property).
		/// </summary>
		/// <returns>True if asset loading is stil in progress, false otherwise</returns>
		internal static bool AssetLoadingActive()
		{
			if (Instance<AssetLoader>.HasInstance && assetLoadingStarted)
			{
				return !assetsFinished;
			}

			// Fallback to false (asset loader not available, so assuming no action is in progress).
			return false;
		}


		/// <summary>
		/// Completes the loading process.
		/// Called via LoadingScreen linesource (so is a method, not a property).
		/// </summary>
		/// <returns>Nothing (IEnumerator yield break)e</returns>
		private static IEnumerator LoadingComplete()
		{
			// Stop loading animation.
			Singleton<LoadingManager>.instance.LoadingAnimationComponent.enabled = false;

			// Print memory usage.
			AssetLoader.PrintMem();

			// Dispose of loader instances.
			Instance<AssetLoader>.instance?.Dispose();
			Instance<Fixes>.instance.Dispose();
			Instance<CustomDeserializer>.instance.Dispose();

			// Stop profiling.
			Logging.KeyMessage("loading completed at ", Profiling.Millis);
			Profiling.Stop();

			yield break;
		}


		/// <summary>
		/// Adds the given asset to the list of failed assets.
		/// </summary>
		/// <param name="fullName">Full name of asset</param>
		/// <returns>True if this asset wasn't already recorded as failed, false otherwise</returns>
		internal static bool AddFailed(string fullName) => knownFailedAssets.Add(fullName);


		/// <summary>
		/// Checks to see if the given asset is reacorded as having failed.
		/// </summary>
		/// <param name="fullName">Full name of asset</param>
		/// <returns>True if the asset has failed, false otherwise</returns>
		internal static bool HasAssetFailed(string fullName) =>  knownFailedAssets.Contains(fullName);


		/// <summary>
		/// Checks to see if this savegame is a known fast load candidate.
		/// The savegame is a fast load if it is pre-known or its time stamp is newer than the full load time stamp.
		/// </summary>
		private static bool IsKnownFastLoad(Package.Asset asset)
		{
			// Check to see if we've already loaded this savegame.
			if (knownFastLoads.TryGetValue(asset.checksum, out bool isFastLoad))
			{
				// Yes - return recorded status.
				return isFastLoad;
			}

			try
			{
				// Check to see if the save's timestamp is newer than the timestamp of the last full load.
				isFastLoad = fullLoadTimestamp < asset.package.Find(asset.package.packageMainAsset).Instantiate<SaveGameMetaData>().timeStamp;

				// Record status in our dictionary before returning.
				knownFastLoads[asset.checksum] = isFastLoad;
				return isFastLoad;
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception checking fast load status");
			}

			// Default to false (not known fast load).
			return false;
		}



		/// <summary>
		/// Checks to see if the simulation thread has failed, and if so, displays the failure message.
		/// </summary>
		/// <param name="simulationTask">Simulation thread</param>
		/// <returns>True if the task has failed, false otherwise</returns>
		private static bool HasTaskFailed(AsyncTask simulationTask)
		{
			// Check to see if task has failed.
			if (simulationTask.failed)
			{
				try
				{
					// Extract failure message.
					Exception[] array = ((Queue<Exception>)Util.GetStatic(typeof(UIView), "sLastException")).ToArray();
					string message = null;
					if (array.Length != 0)
					{
						message = array[array.Length - 1].Message;
					}

					// Display failure message.
					Instance<LoadingScreen>.instance.SimulationSource?.Failed(message);
					return true;
				}
				catch (Exception e)
				{
					Logging.LogException(e, "exception extracting simulation failure message");
				}
			}

			// If we got here, the task hasn't failed.
			return false;
		}


		/// <summary>
		/// Checks to see if the policies panel has been initialized, and if not, tries to initialize it.
		/// </summary>
		/// <returns>Nothing (IEnumerator yield break)</returns>
		private static IEnumerator CheckPolicies()
		{
			// Attempt to retrieve policies panel.
			PoliciesPanel policiesPanel = ToolsModifierControl.policiesPanel;
			if (policiesPanel != null)
			{
				// Reflect panel initialized flag.
				if (!(bool)Util.Get(policiesPanel, "m_Initialized"))
				{
					// Not initialized yet - try to force initialization via call to RefreshPanel.
					Logging.KeyMessage("PoliciesPanel not initialized yet. Initializing at ", Profiling.Millis);
					try
					{
						Util.InvokeVoid(policiesPanel, "RefreshPanel");
					}
					catch (Exception e)
					{
						Logging.LogException(e, "exception refreshing policies panel");
					}
				}
			}
			else
			{
				Logging.Error("PoliciesPanel reference is null; initialization failed at ", Profiling.Millis);
			}
			yield break;
		}


		/// <summary>
		/// Destroys all loadded scene prefabs, leaving simulation prefabs unaffected.
		/// </summary>
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


		/// <summary>
		/// Destroys scene prefabs of the given type, leaving simulation prefabs unaffected.
		/// </summary>
		/// <typeparam name="P"></typeparam>
		private static void DestroyLoaded<P>() where P : PrefabInfo
		{
			try
			{
				// Iterate through all loaded prefabs.
				int prefabCount = PrefabCollection<P>.LoadedCount();
				List<P> selectedPrefabs = new List<P>(prefabCount);
				for (int i = 0; i < prefabCount; ++i)
				{
					// If this prefab is loaded, add it to our list for destruction.
					P loadedPrefab = PrefabCollection<P>.GetLoaded((uint)i);
					if (loadedPrefab != null)
					{
						loadedPrefab.m_prefabDataIndex = -1;
						selectedPrefabs.Add(loadedPrefab);
					}
				}

				// Destroy all collected prefabs.
				PrefabCollection<P>.DestroyPrefabs(string.Empty, selectedPrefabs.ToArray(), null);

				// Just in case - ensure m_scenePrefabs.m_size is set to 0.
				if (prefabCount != selectedPrefabs.Count)
				{
					Util.Set(Util.GetStatic(typeof(PrefabCollection<P>), "m_scenePrefabs"), "m_size", 0, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}

				// Just in case - clear prefab dictionary.
				object prefabDict = Util.GetStatic(typeof(PrefabCollection<P>), "m_prefabDict");
				prefabDict.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public).Invoke(prefabDict, null);

				// Clear list and and capacity immediately to free up memory as soon as possible.
				selectedPrefabs.Clear();
				selectedPrefabs.Capacity = 0;
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception destroying loaded prefabs of type ", typeof(P));
			}
		}


		//---- Refactored end.


		private static KeyValuePair<string, int>[] levelStrings = new KeyValuePair<string, int>[20]
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

		private static KeyValuePair<string, int>[] levelStringsAiportDLC = new KeyValuePair<string, int>[3]
		{
			new KeyValuePair<string, int>("Station12Prefabs", 1726383),
			new KeyValuePair<string, int>("Station13Prefabs", 1726384),
			new KeyValuePair<string, int>("ModderPack10Prefabs", 1726381)
		};


		private static KeyValuePair<string, float>[] SetLevels()
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


		// Equivalent to GameCode DLC.
		internal static bool Check(int dlc)
		{
			if (SteamHelper.IsDLCOwned((SteamHelper.DLC)dlc))
			{
				if (LoadingScreenMod.Settings.settings.SkipPrefabs)
				{
					return !LoadingScreenMod.Settings.settings.SkipMatcher.Matches(dlc);
				}
				return true;
			}
			return false;
		}
	}
}