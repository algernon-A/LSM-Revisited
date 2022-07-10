using System;
using System.Collections;
using UnityEngine;
using ColossalFramework.Packaging;
using HarmonyLib;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	/// <summary>
	/// Harmony patch to implement custom level loading.
	/// </summary>
	[HarmonyPatch(typeof(LoadingManager), nameof(LoadingManager.LoadLevel),
			new Type[] { typeof(Package.Asset), typeof(string), typeof(string), typeof(SimulationMetaData), typeof(bool) },
			new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
	public static class LoadLevel
    {
		/// <summary>
		/// Harmony pre-emptive Prefix patch to LoadingManager.LoadLevel to implement custom level loading.
		/// </summary>
		/// <param name="__result">Original method result (LoadLevel coroutine)</param>
		/// <param name="__instance">LoadingManager instance</param>
		/// <param name="asset">Package asset</param>
		/// <param name="playerScene">Unity player scene name</param>
		/// <param name="uiScene">Unity UI scene name</param>
		/// <param name="ngs">SimulationMetaData</param>
		/// <param name="forceEnvironmentReload">True to force an environment reload, false (default) otherwise</param>
		/// <returns>True if the custom loader is activated (loading into game, or left control key is held down), false otherwise (fall through to default game code)</returns>
		public static bool Prefix(ref Coroutine __result, LoadingManager __instance, Package.Asset asset, string playerScene, string uiScene, SimulationMetaData ngs, bool forceEnvironmentReload = false)
		{
			// TODO: Update internationalization.
			L10n.SetCurrent();

			// Custom loader is is active when loading into game, or otherwise when a control key is held down.
			if (!(ngs.m_updateMode == SimulationManager.UpdateMode.LoadGame ||
				ngs.m_updateMode == SimulationManager.UpdateMode.NewGameFromMap
				|| ngs.m_updateMode == SimulationManager.UpdateMode.NewGameFromScenario ||
				Input.GetKey(KeyCode.LeftControl)) ||
				Input.GetKey(KeyCode.RightControl))
			{
				// Mod not active - fall back to game code.
				return true;
			}

			// Ensure level loader is active.
			LevelLoader.simulationFailed = LevelLoader.assetLoadingStarted = LevelLoader.assetsFinished = false;

			// Handle unloading.
			if (!__instance.m_currentlyLoading && !__instance.m_applicationQuitting)
			{
				// Call any OnLevelUnloading extensions.
				if (__instance.m_LoadingWrapper != null)
				{
					__instance.m_LoadingWrapper.OnLevelUnloading();
				}

				// Reset legacy settings.
				LoadingScreenMod.Settings settings = LoadingScreenMod.Settings.settings;
				Util.DebugPrint("Options: 2205", settings.loadEnabled, settings.loadUsed, settings.shareTextures, settings.shareMaterials, settings.shareMeshes, settings.optimizeThumbs, settings.reportAssets, settings.checkAssets, settings.skipPrefabs, settings.hideAssets, settings.useReportDate);
				LevelLoader.optimizeThumbs = settings.optimizeThumbs;
				settings.enableDisable = settings.loadUsed && ShiftE;

				// Reset progress.
				__instance.SetSceneProgress(0f);
				LevelLoader.cityName = ((asset != null) ? asset.name : null) ?? "NewGame";
				Profiling.Init();
				Instance<CustomDeserializer>.Create();
				Instance<Fixes>.Create().Deploy();
				Instance<LoadingScreen>.Create().Setup();

				// Reset LoadingManager flags.
				__instance.LoadingAnimationComponent.enabled = true;
				__instance.m_currentlyLoading = true;
				__instance.m_metaDataLoaded = false;
				__instance.m_simulationDataLoaded = false;
				__instance.m_loadingComplete = false;
				__instance.m_renderDataReady = false;
				__instance.m_essentialScenesLoaded = false;
				__instance.m_brokenAssets = string.Empty;

				// Set LoadingManager private progress counters.
				Util.Set(__instance, "m_sceneProgress", 0f);
				Util.Set(__instance, "m_simulationProgress", 0f);

				// Start legacy LSM profiling
				Profiling.Start();
				
				// Reset game profiling.
				__instance.m_loadingProfilerMain.Reset();
				__instance.m_loadingProfilerSimulation.Reset();
				__instance.m_loadingProfilerScenes.Reset();

				// Start Unity coroutine.
				IEnumerator routine = LevelLoader.LoadLevelCoroutine(__instance, asset, playerScene, uiScene, ngs, forceEnvironmentReload);
				__result = __instance.StartCoroutine(routine);
				return false;
			}
			__result = null;
			return false;
		}


		/// <summary>
		/// Returns true if shift-E is currently pressed.
		/// </summary>
		private static bool ShiftE => Input.GetKey(KeyCode.E) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
	}
}