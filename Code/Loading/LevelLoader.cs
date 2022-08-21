namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using AlgernonCommons;
    using ColossalFramework;
    using ColossalFramework.Packaging;
    using ColossalFramework.UI;
    using LoadingScreenMod;
    using UnityEngine;
    using UnityEngine.Profiling;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Custom level loader to implement mod loading.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal performant fields")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal performant fields")]
    public static class LevelLoader
    {
        /// <summary>
        /// Count of skipped prefabs.
        /// Used by PrefabLoader.
        /// </summary>
        internal static readonly int[] SkipCounts = new int[3];

        /// <summary>
        /// Main loading lock object.
        /// </summary>
        internal static object s_loadingLock;

        /// <summary>
        /// Main thread queue.
        /// </summary>
        internal static Queue<IEnumerator> s_mainThreadQueue;

        /// <summary>
        /// Used by AssetLoader and DualProfilerSource to flag finishing of asset loading..
        /// </summary>
        internal static bool s_assetsFinished;

        /// <summary>
        /// Indicates whether asset loading has started.
        /// Referred to from loading patch.
        /// </summary>
        internal static bool s_assetLoadingStarted;

        /// <summary>
        /// Indicates whether to optimize asset thumbnails.
        /// Referred to from loading patch.
        /// </summary>
        internal static bool s_optimizeThumbs;

        /// <summary>
        /// Indicates whether the simulation has failed.
        /// Referred to from loading patch.
        /// </summary>
        internal static bool s_simulationFailed;

        // Skipped prefab lists (names by category).
        private static readonly HashSet<string>[] SkippedPrefabs = new HashSet<string>[3];

        // Failed assets.
        private static readonly HashSet<string> KnownFailedAssets = new HashSet<string>();

        // Known savegames and fast-load eligibility.
        private static readonly Dictionary<string, bool> KnownFastLoads = new Dictionary<string, bool>(2);

        // Last loaded skipped prefabs timestamp.
        private static DateTime s_savedSkipTimestamp;

        // Timestamp of last full load (for fast load eligibility checking).
        private static DateTime s_fullLoadTimestamp;

        // Fast load status.
        private static bool s_fastLoadEligible;

        // For timeout check.
        private static int s_startMillis;

        // TODO: refactor this out.
        private static FastList<LoadingProfiler.Event> s_loadingProfilerEvents = (FastList<LoadingProfiler.Event>)typeof(LoadingProfiler).GetField("m_events", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton<LoadingManager>.instance.m_loadingProfilerSimulation);

        /// <summary>
        /// Gets a value indicating whether save deserialization has finished.
        /// </summary>
        internal static bool IsSaveDeserialized
        {
            get
            {
                {
                    // Record start time if we haven't already.
                    if (s_startMillis == 0)
                    {
                        s_startMillis = Timing.ElapsedMilliseconds;
                    }

                    // Check for deserialization against simulation progress, with a 12-minute timeout.
                    bool isDeserialized = SimulationProgress > 54 || Timing.ElapsedMilliseconds - s_startMillis > 12000;

                    // Reset start time if deserialization is complete.
                    if (isDeserialized)
                    {
                        s_startMillis = 0;
                    }

                    return isDeserialized;
                }
            }
        }

        /// <summary>
        /// Gets the current progress of simulation deserialization.
        /// Note: two threads at play here, old values of m_size might be cached for quite some time.
        /// </summary>
        /// <returns>Current simulation deserialization progress (-1 if error).</returns>
        private static int SimulationProgress
        {
            get
            {
                try
                {
                    // Attempt to get progres from the loading manager profiler.
                    return Thread.VolatileRead(ref s_loadingProfilerEvents.m_size);
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
        /// Gets a value indicating whether all required custom assets have been loaded (ignoring knwon failed assets).
        /// </summary>
        private static bool AllAssetsAvailable
        {
            get
            {
                try
                {
                    // Check availability, ignoring known failed assets).
                    if (UsedAssets.Instance == null)
                    {
                        UsedAssets.Create();
                    }

                    return UsedAssets.Instance.AllAssetsAvailable(KnownFailedAssets);
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
        /// Gets a value indicating whether all buiding prefabs have been loaded.
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
        /// The level loading coroutine itself - everything runs through here.
        /// </summary>
        /// <param name="loadingManager">LoadingManager instance.</param>
        /// <param name="savegame">Save to load.</param>
        /// <param name="playerScene">Unity player scene.</param>
        /// <param name="uiScene">Unity UI scene.</param>
        /// <param name="ngs">Simulation metadata.</param>
        /// <param name="forceEnvironmentReload">True to force a reload of the environment, false otherwise.</param>
        /// <returns>Coroutine IEnumerator yield.</returns>
        public static IEnumerator LoadLevelCoroutine(LoadingManager loadingManager, Package.Asset savegame, string playerScene, string uiScene, SimulationMetaData ngs, bool forceEnvironmentReload)
        {
            Logging.KeyMessage("starting LoadLevelCoroutine at ", Timing.ElapsedMilliseconds);

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
                s_fastLoadEligible = false;
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
                    Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData
                    {
                        m_environment = "Sunny",
                    };
                    Singleton<SimulationManager>.instance.m_metaData.Merge(ngs);
                }

                Util.InvokeVoid(loadingManager, "MetaDataLoaded");
                string mapThemeName = Singleton<SimulationManager>.instance.m_metaData.m_MapThemeMetaData?.name;

                // LSM inserts fastload result to original check.
                s_fastLoadEligible = Singleton<SimulationManager>.instance.m_metaData.m_environment == loadingManager.m_loadedEnvironment && mapThemeName == loadingManager.m_loadedMapTheme && !forceEnvironmentReload;

                // The game is nicely optimized when loading from the pause menu. We must specifically address the following situations:
                // - environment (biome) stays the same
                // - map theme stays the same
                // - forceEnvironmentReload is false
                // - 'load used assets' is enabled
                // - not all assets and prefabs used in the save being loaded are currently in memory
                // - prefab skipping has changed.
                if (s_fastLoadEligible)
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

                        s_fastLoadEligible = AllAssetsAvailable;
                    }

                    // Check prefab skipping changes and building prefab availability.
                    if (s_fastLoadEligible)
                    {
                        // Check to see if prefab skipping has changed since the last load.
                        if (skipStamp != s_savedSkipTimestamp)
                        {
                            // Yes - cancel fastload.
                            s_fastLoadEligible = false;
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
                            s_fastLoadEligible = AllBuildingPrefabsAvailable;
                        }
                    }

                    // This is the actual fast load.
                    if (s_fastLoadEligible)
                    {
                        // Skip any prefabs.
                        if (LoadingScreenMod.Settings.settings.SkipPrefabs && SkippedPrefabs[0] != null)
                        {
                            // Wait until save is deserialized.
                            while (!IsSaveDeserialized)
                            {
                                yield return null;
                            }

                            // Implement skipping.
                            Instance<PrefabLoader>.Create().SetSkippedPrefabs(SkippedPrefabs);
                            loadingManager.QueueLoadingAction(PrefabLoader.RemoveSkippedFromSimulation());
                        }

                        // Resume gamecode.
                        loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "EssentialScenesLoaded"));
                        loadingManager.QueueLoadingAction((IEnumerator)Util.Invoke(loadingManager, "RenderDataReady"));

                        // Legacy profiling.
                        Logging.KeyMessage("commencing fast load at ", Timing.ElapsedMilliseconds);
                    }
                    else
                    {
                        // Fastload requirements not met - fall back to full load by destroying all loaded prefabs and clearing the environment and map theme.

                        // Notice that there is a race condition in the base game at this point: DestroyAllPrefabs ruins the simulation
                        // if its deserialization has progressed far enough. Typically there is no problem.
                        DestroyLoadedPrefabs();
                        loadingManager.m_loadedEnvironment = null;
                        loadingManager.m_loadedMapTheme = null;
                        Logging.KeyMessage("falling back to full load at ", Timing.ElapsedMilliseconds);
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
                    Logging.KeyMessage("starting full load at ", Timing.ElapsedMilliseconds);
                }
            }

            // Gamecode.
            if (loadingManager.m_loadedEnvironment == null)
            {
                // Start LSM insert.
                Reset();
                s_fullLoadTimestamp = DateTime.Now;
                s_savedSkipTimestamp = skipStamp;
                Array.Clear(SkippedPrefabs, 0, SkippedPrefabs.Length);
                s_loadingLock = Util.Get(loadingManager, "m_loadingLock");
                s_mainThreadQueue = (Queue<IEnumerator>)Util.Get(loadingManager, "m_mainThreadQueue");

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
                    Singleton<SimulationManager>.instance.m_metaData = new SimulationMetaData
                    {
                        m_environment = "Sunny",
                    };
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
                List<KeyValuePair<string, float>> levels = SetLevels();
                float levelProgressTotal = 0f;
                foreach (KeyValuePair<string, float> level in levels)
                {
                    levelProgressTotal += level.Value;
                }

                float levelProgressMult = 0.15f / levelProgressTotal;

                // LSM progress is 0.1 vs game 0.
                // LSM skips game totalprogress.
                float currentProgress = 0f;
                string key;

                // LSM calls 'levels' what the game calls 'prefabScenes'.
                // LSM progress is already calculated by level, game calculates progress made against total.
                foreach (KeyValuePair<string, float> level in levels)
                {
                    key = level.Key;

                    // LSM skips progress update.
                    loadingManager.m_loadingProfilerScenes.BeginLoading(key);
                    AsyncOperation op = SceneManager.LoadSceneAsync(key, LoadSceneMode.Additive);
                    while (!op.isDone)
                    {
                        // Different progress count by LSM.
                        loadingManager.SetSceneProgress(currentProgress + (op.progress * (level.Value * levelProgressMult)));
                        yield return null;
                    }

                    loadingManager.m_loadingProfilerScenes.EndLoading();
                    currentProgress += level.Value * levelProgressMult;
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
                    lock (s_loadingLock)
                    {
                        i = s_mainThreadQueue.Count;
                    }
                }
                while (i > 0);

                // LSM equivalents of gamecode, just queing LSM loading actions.
                AssetLoader.Create();
                loadingManager.QueueLoadingAction(AssetLoader.Instance.LoadCustomContent());

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
                s_simulationFailed = HasTaskFailed(task);
                while (!s_assetsFinished)
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
                        loadingManager.SetSceneProgress(0.85f + (op.progress * 0.05f));

                        // LSM insert.
                        if (s_optimizeThumbs)
                        {
                            Instance<CustomDeserializer>.instance.ReceiveAvailable();
                        }

                        // Resume gamecode.
                        yield return null;
                    }

                    loadingManager.m_loadingProfilerScenes.EndLoading();
                }

                // LSM insert.
                if (!s_simulationFailed)
                {
                    s_simulationFailed = HasTaskFailed(task);
                }

                // Resume gamecode.
                if (!string.IsNullOrEmpty(uiScene))
                {
                    loadingManager.m_loadingProfilerScenes.BeginLoading(uiScene);
                    AsyncOperation op = SceneManager.LoadSceneAsync(uiScene, LoadSceneMode.Additive);
                    while (!op.isDone)
                    {
                        // LSM modifies constants here.
                        loadingManager.SetSceneProgress(0.9f + (op.progress * 0.08f));

                        // LSM insert.
                        if (s_optimizeThumbs)
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
                if (s_optimizeThumbs)
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
                if (!s_simulationFailed && (i++ & 7) == 0)
                {
                    s_simulationFailed = HasTaskFailed(task);
                }

                // Resume gamecode.
                yield return null;
            }

            // LSM insert.
            if (!s_simulationFailed)
            {
                s_simulationFailed = HasTaskFailed(task);
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
            KnownFastLoads[savegame.checksum] = true;
            AssetLoader.LogStatus();
        }

        /// <summary>
        /// Resets the level loader's data records.
        /// </summary>
        internal static void Reset()
        {
            // Clear lists.
            KnownFailedAssets.Clear();
            KnownFastLoads.Clear();

            // Clear skipped items.
            Array.Clear(SkipCounts, 0, SkipCounts.Length);
        }

        /// <summary>
        /// Sets the skipped prefabs lists to the provided value.
        /// </summary>
        /// <param name="prefabs">Array of skipped prefab lists to set.</param>
        internal static void SetSkippedPrefabs(HashSet<string>[] prefabs)
        {
            prefabs.CopyTo(SkippedPrefabs, 0);
        }

        /// <summary>
        /// Checks if asset loading is stil in progress.
        /// Called via LoadingScreen linesource (so is a method, not a property).
        /// </summary>
        /// <returns>True if asset loading is stil in progress, false otherwise.</returns>
        internal static bool AssetLoadingActive()
        {
            if (AssetLoader.Instance != null && s_assetLoadingStarted)
            {
                return !s_assetsFinished;
            }

            // Fallback to false (asset loader not available, so assuming no action is in progress).
            return false;
        }

        /// <summary>
        /// Adds the given asset to the list of failed assets.
        /// </summary>
        /// <param name="fullName">Full name of asset.</param>
        /// <returns>True if this asset wasn't already recorded as failed, false otherwise.</returns>
        internal static bool AddFailed(string fullName) => KnownFailedAssets.Add(fullName);

        /// <summary>
        /// Checks to see if the given asset is reacorded as having failed.
        /// </summary>
        /// <param name="fullName">Full name of asset.</param>
        /// <returns>True if the asset has failed, false otherwise.</returns>
        internal static bool HasAssetFailed(string fullName) => KnownFailedAssets.Contains(fullName);

        /// <summary>
        /// Checks DLC activation.
        /// </summary>
        /// <param name="dlc">DLC to check.</param>
        /// <returns>True if DLC activated, false otherwise.</returns>
        internal static bool DLC(uint dlc)
        {
            if (SteamHelper.IsDLCOwned((SteamHelper.DLC)dlc))
            {
                if (LoadingScreenMod.Settings.settings.SkipPrefabs)
                {
                    return !LoadingScreenMod.Settings.settings.SkipMatcher.Matches((int)dlc);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Completes the loading process.
        /// Called via LoadingScreen linesource (so is a method, not a property).
        /// </summary>
        /// <returns>Nothing (IEnumerator yield break).</returns>
        private static IEnumerator LoadingComplete()
        {
            // Stop loading animation.
            Singleton<LoadingManager>.instance.LoadingAnimationComponent.enabled = false;

            // Print memory usage.
            AssetLoader.LogStatus();

            // Dispose of loader instances.
            AssetLoader.Dispose();
            Instance<Fixes>.instance.Dispose();
            Instance<CustomDeserializer>.instance.Dispose();

            // Stop profiling.
            Logging.KeyMessage("loading completed at ", Timing.ElapsedMilliseconds);
            Timing.Stop();

            yield break;
        }

        /// <summary>
        /// Checks to see if this savegame is a known fast load candidate.
        /// The savegame is a fast load if it is pre-known or its time stamp is newer than the full load time stamp.
        /// </summary>
        private static bool IsKnownFastLoad(Package.Asset asset)
        {
            // Check to see if we've already loaded this savegame.
            if (KnownFastLoads.TryGetValue(asset.checksum, out bool isFastLoad))
            {
                // Yes - return recorded status.
                return isFastLoad;
            }

            try
            {
                // Check to see if the save's timestamp is newer than the timestamp of the last full load.
                isFastLoad = s_fullLoadTimestamp < asset.package.Find(asset.package.packageMainAsset).Instantiate<SaveGameMetaData>().timeStamp;

                // Record status in our dictionary before returning.
                KnownFastLoads[asset.checksum] = isFastLoad;
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
        /// <param name="simulationTask">Simulation thread.</param>
        /// <returns>True if the task has failed, false otherwise.</returns>
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
                    // TODO: Simulation failed message.
                    //LoadingScreen.s_instance.SimulationSource?.Failed(message);
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
        /// <returns>Nothing (IEnumerator yield break).</returns>
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
                    Logging.KeyMessage("PoliciesPanel not initialized yet. Initializing at ", Timing.ElapsedMilliseconds);
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
                Logging.Error("PoliciesPanel reference is null; initialization failed at ", Timing.ElapsedMilliseconds);
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
            DestroyLoaded<global::EventInfo>();
            DestroyLoaded<DisasterInfo>();
            DestroyLoaded<RadioContentInfo>();
            DestroyLoaded<RadioChannelInfo>();
        }

        /// <summary>
        /// Destroys scene prefabs of the given type, leaving simulation prefabs unaffected.
        /// </summary>
        /// <typeparam name="TPrefab">Prefab type.</typeparam>
        private static void DestroyLoaded<TPrefab>()
            where TPrefab : PrefabInfo
        {
            try
            {
                // Iterate through all loaded prefabs.
                int prefabCount = PrefabCollection<TPrefab>.LoadedCount();
                List<TPrefab> selectedPrefabs = new List<TPrefab>(prefabCount);
                for (int i = 0; i < prefabCount; ++i)
                {
                    // If this prefab is loaded, add it to our list for destruction.
                    TPrefab loadedPrefab = PrefabCollection<TPrefab>.GetLoaded((uint)i);
                    if (loadedPrefab != null)
                    {
                        loadedPrefab.m_prefabDataIndex = -1;
                        selectedPrefabs.Add(loadedPrefab);
                    }
                }

                // Destroy all collected prefabs.
                PrefabCollection<TPrefab>.DestroyPrefabs(string.Empty, selectedPrefabs.ToArray(), null);

                // Just in case - ensure m_scenePrefabs.m_size is set to 0.
                if (prefabCount != selectedPrefabs.Count)
                {
                    Util.Set(Util.GetStatic(typeof(PrefabCollection<TPrefab>), "m_scenePrefabs"), "m_size", 0, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                // Just in case - clear prefab dictionary.
                object prefabDict = Util.GetStatic(typeof(PrefabCollection<TPrefab>), "m_prefabDict");
                prefabDict.GetType().GetMethod("Clear", BindingFlags.Instance | BindingFlags.Public).Invoke(prefabDict, null);

                // Clear list and and capacity immediately to free up memory as soon as possible.
                selectedPrefabs.Clear();
                selectedPrefabs.Capacity = 0;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception destroying loaded prefabs of type ", typeof(TPrefab));
            }
        }

        /// <summary>
        /// Calculates the scene levels to load.
        /// </summary>
        /// <returns>List of scene levels and corresponding progress values.</returns>
        private static List<KeyValuePair<string, float>> SetLevels()
        {
            LoadingManager loadingManager = Singleton<LoadingManager>.instance;
            loadingManager.m_supportsExpansion[0] = DLC(369150u);
            loadingManager.m_supportsExpansion[1] = DLC(420610u);
            loadingManager.m_supportsExpansion[2] = DLC(515191u);
            loadingManager.m_supportsExpansion[3] = DLC(547502u);
            loadingManager.m_supportsExpansion[4] = DLC(614580u);
            loadingManager.m_supportsExpansion[5] = DLC(715191u);
            loadingManager.m_supportsExpansion[6] = DLC(715194u);
            loadingManager.m_supportsExpansion[7] = DLC(944071u);
            loadingManager.m_supportsExpansion[8] = DLC(1146930u);
            loadingManager.m_supportsExpansion[9] = DLC(1726380u);
            bool isWinter = Singleton<SimulationManager>.instance.m_metaData.m_environment == "Winter";
            if (isWinter && !loadingManager.m_supportsExpansion[1])
            {
                Singleton<SimulationManager>.instance.m_metaData.m_environment = "Sunny";
                isWinter = false;
            }

            // Cut.
            List<KeyValuePair<string, float>> prefabScenes = new List<KeyValuePair<string, float>>(32);
            string text = (string)Util.Invoke(loadingManager, "GetLoadingScene");

            // LSM insert.
            if (!string.IsNullOrEmpty(text))
            {
                prefabScenes.Add(new KeyValuePair<string, float>(text, 0.015f));
            }

            // Gamecode equivalent.
            prefabScenes.Add(new KeyValuePair<string, float>(Singleton<SimulationManager>.instance.m_metaData.m_environment + "Prefabs", 1.27f));
            if ((bool)Util.Invoke(loadingManager, "LoginUsed"))
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "LoginPackPrefabs" : "WinterLoginPackPrefabs", 0.01f));
            }

            prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "PreorderPackPrefabs" : "WinterPreorderPackPrefabs", 0.02f));
            prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "SignupPackPrefabs" : "WinterSignupPackPrefabs", 0.01f));
            if (DLC(346791u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("DeluxePackPrefabs", 0.02f));
            }

            if (APP(238370u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("MagickaPackPrefabs", 0.01f));
            }

            if (loadingManager.m_supportsExpansion[0])
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "Expansion1Prefabs" : "WinterExpansion1Prefabs", 0.17f));
            }

            if (loadingManager.m_supportsExpansion[1])
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Expansion2Prefabs", 0.04f));
            }

            if (loadingManager.m_supportsExpansion[2])
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Expansion3Prefabs", 0.04f));
            }

            if (loadingManager.m_supportsExpansion[3])
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Expansion4Prefabs", 0.04f));
            }

            if (loadingManager.m_supportsExpansion[4])
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "Expansion5Prefabs" : "WinterExpansion5Prefabs", 0.2f));
            }

            if (loadingManager.m_supportsExpansion[5])
            {
                prefabScenes.Add(new KeyValuePair<string, float>(Singleton<SimulationManager>.instance.m_metaData.m_environment + "Expansion6Prefabs", 0.1f));
            }

            if (loadingManager.m_supportsExpansion[6])
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "Expansion7Prefabs" : "WinterExpansion7Prefabs", 0.1f));
            }

            if (loadingManager.m_supportsExpansion[7])
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "Expansion8Prefabs" : "WinterExpansion8Prefabs", 0.1f));
            }

            if (loadingManager.m_supportsExpansion[8])
            {
                prefabScenes.Add(new KeyValuePair<string, float>((!isWinter) ? "Expansion9Prefabs" : "WinterExpansion9Prefabs", 0.1f));
            }

            if (loadingManager.m_supportsExpansion[9])
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Expansion10Prefabs", 0.1f));
            }

            if (DLC(456200u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("FootballPrefabs", 0.01f));
            }

            if (DLC(525940u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Football2Prefabs", 0.01f));
            }

            if (DLC(526610u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Football3Prefabs", 0.01f));
            }

            if (DLC(526611u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Football4Prefabs", 0.01f));
            }

            if (DLC(526612u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Football5Prefabs", 0.01f));
            }

            if (DLC(547501u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station1Prefabs", 0.01f));
            }

            if (DLC(614582u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station2Prefabs", 0.01f));
            }

            if (DLC(715193u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station3Prefabs", 0.01f));
            }

            if (DLC(815380u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station4Prefabs", 0.01f));
            }

            if (DLC(944070u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station5Prefabs", 0.01f));
            }

            if (DLC(1065490u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station6Prefabs", 0.01f));
            }

            if (DLC(1065491u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station7Prefabs", 0.01f));
            }

            if (DLC(1148021u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station8Prefabs", 0.01f));
            }

            if (DLC(1196100u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station9Prefabs", 0.01f));
            }

            if (DLC(1531472u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station10Prefabs", 0.01f));
            }

            if (DLC(1531473u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station11Prefabs", 0.01f));
            }

            if (DLC(1726383u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station12Prefabs", 0.01f));
            }

            if (DLC(1726384u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("Station13Prefabs", 0.01f));
            }

            if (DLC(614581u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("FestivalPrefabs", 0.01f));
            }

            if (DLC(715192u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ChristmasPrefabs", 0.01f));
            }

            if (DLC(515190u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack1Prefabs", 0.03f));
            }

            if (DLC(547500u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack2Prefabs", 0.03f));
            }

            if (DLC(715190u))
            {
                Package.Asset asset2 = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanSuburbiaStyleName);
                if (asset2 != null && asset2.isEnabled)
                {
                    prefabScenes.Add(new KeyValuePair<string, float>("ModderPack3Prefabs", 0.03f));
                }
            }

            if (DLC(1059820u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack4Prefabs", 0.03f));
            }

            if (DLC(1148020u))
            {
                Package.Asset asset3 = PackageManager.FindAssetByName("System." + DistrictStyle.kModderPack5StyleName);
                if (asset3 != null && asset3.isEnabled)
                {
                    prefabScenes.Add(new KeyValuePair<string, float>("ModderPack5Prefabs", 0.03f));
                }
            }

            if (DLC(1148022u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack6Prefabs", 0.03f));
            }

            if (DLC(1531470u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack7Prefabs", 0.03f));
            }

            if (DLC(1531471u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack8Prefabs", 0.03f));
            }

            if (DLC(1726381u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ModderPack10Prefabs", 0.03f));
            }

            if (DLC(563850u))
            {
                prefabScenes.Add(new KeyValuePair<string, float>("ChinaPackPrefabs", 0.02f));
            }

            Package.Asset europeanStyles = PackageManager.FindAssetByName("System." + DistrictStyle.kEuropeanStyleName);
            if (europeanStyles != null && europeanStyles.isEnabled)
            {
                if (Singleton<SimulationManager>.instance.m_metaData.m_environment.Equals("Europe"))
                {
                    prefabScenes.Add(new KeyValuePair<string, float>("EuropeNormalPrefabs", 0.13f));
                }
                else
                {
                    prefabScenes.Add(new KeyValuePair<string, float>("EuropeStylePrefabs", 0.1f));
                }
            }

            return prefabScenes;
        }

        /// <summary>
        /// Checks for app ownership.
        /// </summary>
        /// <param name="id">App id to check.</param>
        /// <returns>True if app owned, false otherwise.</returns>
        private static bool APP(uint id) => SteamHelper.IsAppOwned(id);
    }
}