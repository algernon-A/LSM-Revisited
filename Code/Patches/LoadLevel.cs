// <copyright file="LoadLevel.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections;
    using AlgernonCommons.Patching;
    using ColossalFramework.Packaging;
    using HarmonyLib;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// Harmony patch to implement custom level loading.
    /// </summary>
    [HarmonyPatch(
        typeof(LoadingManager),
        nameof(LoadingManager.LoadLevel),
        new Type[] { typeof(Package.Asset), typeof(string), typeof(string), typeof(SimulationMetaData), typeof(bool) },
        new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal })]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Harmony")]
    public static class LoadLevel
    {
        /// <summary>
        /// Gets the current city name.
        /// </summary>
        internal static string CityName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether shift-E is currently pressed.
        /// </summary>
        private static bool ShiftE => Input.GetKey(KeyCode.E) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        /// <summary>
        /// Harmony pre-emptive Prefix patch to LoadingManager.LoadLevel to implement custom level loading.
        /// </summary>
        /// <param name="__result">Original method result (LoadLevel coroutine).</param>
        /// <param name="__instance">LoadingManager instance.</param>
        /// <param name="asset">Package asset.</param>
        /// <param name="playerScene">Unity player scene name.</param>
        /// <param name="uiScene">Unity UI scene name.</param>
        /// <param name="ngs">SimulationMetaData.</param>
        /// <param name="forceEnvironmentReload">True to force an environment reload, false (default) otherwise.</param>
        /// <returns>True if the custom loader is activated (loading into game, or left control key is held down), false otherwise (fall through to default game code).</returns>
        public static bool Prefix(ref Coroutine __result, LoadingManager __instance, Package.Asset asset, string playerScene, string uiScene, SimulationMetaData ngs, bool forceEnvironmentReload = false)
        {
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

            // Reset flags.
            LevelLoader.s_simulationFailed = LevelLoader.s_assetLoadingStarted = LevelLoader.s_assetsFinished = false;

            // Don't do anyting further if we're already loading, or the application isn't quitting.
            if (__instance.m_currentlyLoading || __instance.m_applicationQuitting)
            {
                __result = null;

                // Don't execute game code.
                return false;
            }

            // Call any OnLevelUnloading extensions.
            if (__instance.m_LoadingWrapper != null)
            {
                __instance.m_LoadingWrapper.OnLevelUnloading();
            }

            // Patch Custom Animation Loader.
            PatcherManager<Patcher>.Instance.PatchCustomAnimationLoader();

            // Report settings.
            Util.DebugPrint(
                "Options: 2205",
                LSMRSettings.LoadEnabled,
                LSMRSettings.LoadUsed,
                LSMRSettings.ShareTextures,
                LSMRSettings.ShareMaterials,
                LSMRSettings.ShareMeshes,
                LSMRSettings.OptimizeThumbs,
                LSMRSettings.ReportAssets,
                LSMRSettings.CheckAssets,
                LSMRSettings.SkipPrefabs,
                LSMRSettings.HideAssets,
                true);
            LevelLoader.s_optimizeThumbs = LSMRSettings.OptimizeThumbs;
            LSMRSettings.EnableDisable = LSMRSettings.LoadUsed && ShiftE;

            // Reset progress.
            __instance.SetSceneProgress(0f);
            CityName = asset?.name ?? "NewGame";
            Instance<CustomDeserializer>.Create();
            Instance<Fixes>.Create().Deploy();

            // Initialize loading screen.
            LoadingScreen.s_instance = new LoadingScreen();

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

            // Start timer.
            Timing.Start();

            // Reset game profiling.
            __instance.m_loadingProfilerMain.Reset();
            __instance.m_loadingProfilerSimulation.Reset();
            __instance.m_loadingProfilerScenes.Reset();

            // Start Unity coroutine.
            IEnumerator routine = LevelLoader.LoadLevelCoroutine(__instance, asset, playerScene, uiScene, ngs, forceEnvironmentReload);
            __result = __instance.StartCoroutine(routine);

            // Don't execute original method.
            return false;
        }
    }
}