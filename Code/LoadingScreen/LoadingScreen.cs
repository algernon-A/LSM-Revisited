// <copyright file="LoadingScreen.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Collections;
    using System.Reflection;
    using System.Text;
    using AlgernonCommons.Patching;
    using AlgernonCommons.Translation;
    using ColossalFramework;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// The actual loading screen itself.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal performant fields")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:Non-private readonly fields should begin with upper-case letter", Justification = "Internal performant fields")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal performant fields")]
    public sealed class LoadingScreen
    {
        /// <summary>
        /// Active instance.
        /// </summary>
        internal static LoadingScreen s_instance;

        /// <summary>
        /// Text mesh width.
        /// </summary>
        internal readonly float m_meshWidth = Screen.width / 3;

        /// <summary>
        /// Text mesh height.
        /// </summary>
        internal readonly float m_meshHeight = 3 * Screen.height / 4;

        // Private layout constants.
        private const float ScreenMargin = 50f;
        private const float BoxWidth = 480f;

        // Is this Windows?
        private static readonly bool IsWindows = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        // Display text.
        private static readonly StringBuilder ThreadText = new StringBuilder(256);
        private static readonly StringBuilder AssetLoaderText = new StringBuilder(256);
        private static StringBuilder s_memoryText = new StringBuilder(256);
        private static StringBuilder s_scenesAndAssetsText = new StringBuilder(4096);

        // Unity GUI style.
        private static GUIStyle s_style;

        // GUI overlay layout.
        private static float s_scenesAssetsBoxHeight;
        private static float s_timingBoxHeight;
        private static float s_assetBoxHeight;
        private static float s_memoryBoxHeight;
        private static float s_threadBoxHeight;

        // Loading animation.
        private readonly LoadingAnimation _loadingAnimiation;
        private readonly FieldInfo _targetProgressField = typeof(LoadingAnimation).GetField("m_targetProgress", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        // Camera instance reference.
        private readonly Camera _camera;

        // Status treackers.
        private ScenesAndAssetsStatus _scenesAndAssetsStatus;

        // Progress timers.
        private float _timer;
        private float _progress;
        private float _progressTime;
        private float _minProgress;
        private float _maxProgress = 1f;

        // Background image components.
        private Mesh _imageMesh;
        private Material _imageMaterial;
        private float _xScale;
        private float _yScale;

        // Animation components.
        private Mesh _animationMesh;
        private Material _animationMaterial;
        private Material _barBGMaterial;
        private Material _barFGMaterial;

        // Status flags.
        private bool _imageLoaded;
        private bool _animationLoaded;
        private bool _loading = true;

        // Asset loading progress title length.
        private int _assetTitleLength;
        private string _perSecondString;

        // Failed status.
        private string _simulationFailedMessage = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingScreen"/> class.
        /// </summary>
        internal LoadingScreen()
        {
            // Set instance references.
            _loadingAnimiation = Singleton<LoadingAnimation>.instance;
            _camera = _loadingAnimiation.GetComponent<Camera>();

            // Get mesh and material instances from loading animation.
            _animationMesh = (Mesh)Util.Get(_loadingAnimiation, "m_animationMesh");
            _animationMaterial = (Material)Util.Get(_loadingAnimiation, "m_animationMaterial");
            _barBGMaterial = (Material)Util.Get(_loadingAnimiation, "m_barBGMaterial");
            _barFGMaterial = (Material)Util.Get(_loadingAnimiation, "m_barFGMaterial");
            _animationLoaded = (bool)Util.Get(_loadingAnimiation, "m_animationLoaded");

            // Create scene and asset status monitor.
            _scenesAndAssetsStatus = new ScenesAndAssetsStatus();

            // Apply Harmony patches to the loading animation.
            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
            PatcherManager<Patcher>.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");
            PatcherManager<Patcher>.Instance.PostfixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnGUI");

            // Start Unity overlay update coroutine.
            Singleton<LoadingManager>.instance.StartCoroutine(UpdateText());
        }

        /// <summary>
        /// Gets the scenes and asset status tracker.
        /// </summary>
        internal ScenesAndAssetsStatus SceneAndAssetStatus => _scenesAndAssetsStatus;

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.SetImage.
        /// Sets the loading animation image - this patch ensure the image is set here and not in the game instance.
        /// Patch applied manually.
        /// </summary>
        /// <param name="mesh">Image mesh to set.</param>
        /// <param name="material">Image material to set.</param>
        /// <param name="scale">Image scale.</param>
        /// <returns>Always false (never execute original method).</returns>
        public static bool SetImage(Mesh mesh, Material material, float scale)
        {
            // Destroy any existing material.
            if (s_instance._imageMaterial != null)
            {
                UnityEngine.Object.Destroy(s_instance._imageMaterial);
            }

            // Apply arguments.
            s_instance._imageMesh = mesh;
            s_instance._imageMaterial = new Material(material);

            // Check if we're using custom background images.
            if (BackgroundImage.CurrentImageMode != BackgroundImage.ImageMode.Standard)
            {
                // Try to get imgur image.
                Material customMaterial = BackgroundImage.GetImage(material);
                if (customMaterial != null)
                {
                    // Success - apply new material instead.
                    s_instance._imageMaterial = customMaterial;
                }
            }

            // Calculate aspect ratios.
            Texture imageTexture = s_instance._imageMaterial?.mainTexture;
            float imageAspectRatio = imageTexture != null ? (float)imageTexture.width / imageTexture.height : 1f;
            float screenAspectRatio = (float)Screen.currentResolution.width / Screen.currentResolution.height;

            // Scaling parameters.
            float imageScale = 2f * scale;

            // Set safe defaults.
            s_instance._xScale = imageScale;
            s_instance._yScale = imageScale;

            // Calculate scaling.
            switch (BackgroundImage.ImageScaling)
            {
                case ScaleMode.ScaleToFit:
                    if (imageAspectRatio > screenAspectRatio)
                    {
                        // Wide images (aspect ratios greater than the screen).
                        s_instance._xScale = imageScale * screenAspectRatio;
                        s_instance._yScale = imageScale * screenAspectRatio / imageAspectRatio;
                    }
                    else
                    {
                        // Non- wide iages (aspect ratios equal or less than the screen).
                        s_instance._xScale = imageScale * imageAspectRatio;
                    }

                    break;

                case ScaleMode.ScaleAndCrop:
                    if (imageAspectRatio > screenAspectRatio)
                    {
                        // Wide images (aspect ratios greater than the screen).
                        s_instance._xScale = imageScale * imageAspectRatio;
                    }
                    else
                    {
                        // Non- wide iages (aspect ratios equal or less than the screen).
                        s_instance._xScale = imageScale * screenAspectRatio;
                        s_instance._yScale = imageScale * screenAspectRatio / imageAspectRatio;
                    }

                    break;

                case ScaleMode.StretchToFill:
                    // Works for ABLC BUTTONS strech previewImage AND also ABLC PREVIEW and also REPAINT and also 1.5.
                    s_instance._xScale = imageScale * screenAspectRatio;
                    break;
            }

            // Set status flag.
            s_instance._imageLoaded = true;

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.SetText.
        /// Used to completely nullify the original game method.
        /// </summary>
        /// <returns>Always false (never execute original method).</returns>
        public static bool SetText() => false;

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.OnEnable.
        /// Used to ensure camera status.
        /// </summary>
        /// <returns>Always false (never execute original method).</returns>
        public static bool OnEnable()
        {
            s_instance._camera.enabled = true;
            s_instance._camera.clearFlags = CameraClearFlags.Color;

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.OnDisable.
        /// Used to trigger disposal of instance.
        /// </summary>
        /// <returns>Always false (never execute original method).</returns>
        public static bool OnDisable()
        {
            s_instance._camera.enabled = false;
            s_instance.Dispose();

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.Update.
        /// Used to update progress bar.
        /// </summary>
        /// <returns>Always false (never execute original method).</returns>
        public static bool Update()
        {
            // Update timer (cap at 0.125 seconds per update in case something hung).
            LoadingScreen loadingScreen = s_instance;
            float deltaTime = Mathf.Min(0.125f, Time.deltaTime);
            loadingScreen._timer += deltaTime;

            // Update progress three times per second.
            float now = Time.time;
            if (now - loadingScreen._progressTime >= 0.333333343f)
            {
                loadingScreen._progressTime = now;
                loadingScreen.Progress();
            }

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Harmony pre-emptive prefix patch for LoadingAnimation.OnPostRender.
        /// Used to draw the loading screen.
        /// </summary>
        /// <returns>Always false (never execute original method).</returns>
        public static bool OnPostRender()
        {
            // Draw background mesh.
            if (s_instance._imageLoaded)
            {
                // Draw image.
                if (s_instance._imageMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(s_instance._imageMesh, Matrix4x4.TRS(new Vector3(0f, 0f, 10f), Quaternion.identity, new Vector3(s_instance._xScale, s_instance._yScale, 1f)));
                }
            }

            // Draw loading animation.
            if (s_instance._animationLoaded)
            {
                Quaternion quaternion = Quaternion.AngleAxis(s_instance._timer * 100f, Vector3.back);
                s_instance._animationMaterial.color = new Color(0.3f, 0.6f, 1f, 1f);
                Mesh mesh = s_instance._animationMesh;

                // Draw animation mesh.
                if (s_instance._animationMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(new Vector3(0f, -0.04f, 10f), quaternion, new Vector3(0.2f, 0.2f, 0.2f)));
                }

                // Draw progress bar background mesh.
                Vector3 position = new Vector3(0f, -0.2f, 10f);
                Vector3 scale = new Vector3(0.4f, 0.025f, 0.2f);
                s_instance._barBGMaterial.color = new Color(1f, 1f, 1f, 1f);
                if (s_instance._barBGMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, Quaternion.identity, scale));
                }

                // Draw progress bar foreground mesh - adjusting starting scale to fit within background mesh.
                scale.x *= 0.9875f;
                scale.y *= 0.8f;

                // Scale position according to progress.
                position.x -= scale.x * (1f - s_instance._progress) * 0.5f;
                scale.x *= s_instance._progress;
                s_instance._barFGMaterial.color = new Color(1f, 1f, 1f, 1f);
                if (s_instance._barFGMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, Quaternion.identity, scale));
                }
            }

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Harmony Postfix to LoadingAnimation.OnGUI to implement Unity status ovelay.
        /// Based on the look of Quistar's loader prototype, which I quite like.
        /// </summary>
        public static void OnGUI()
        {
            // Perform first-time setup if required.
            if (s_style == null)
            {
                s_style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    margin = new RectOffset(10, 10, 10, 10),
                    padding = new RectOffset(10, 10, 10, 10),
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    border = new RectOffset(3, 3, 3, 3),
                    richText = true,
                };

                // Set maximumum lines.
                s_instance._scenesAndAssetsStatus.MaxLines = (int)((Screen.height - (ScreenMargin * 2f)) / s_style.lineHeight) - 1;

                // Calculate text box heights.
                s_scenesAssetsBoxHeight = Screen.height - (ScreenMargin * 2f);
                s_timingBoxHeight = (s_style.lineHeight * 3f) + (s_style.padding.top * 2f);
                s_assetBoxHeight = (s_style.lineHeight * 4f) + (s_style.padding.top * 2f);
                s_memoryBoxHeight = (s_style.lineHeight * 6f) + (s_style.padding.top * 2f);
                s_threadBoxHeight = (s_style.lineHeight * 6f) + (s_style.padding.top * 2f);
            }

            // Y-position indicator.
            float currentY = ScreenMargin;

            // Scenes and assets.
            GUI.Box(new Rect(ScreenMargin, ScreenMargin, BoxWidth, s_scenesAssetsBoxHeight), s_scenesAndAssetsText.ToString(), s_style);

            // Rightmost column relative x-position..
            float rightColumnnX = Screen.width - ScreenMargin - BoxWidth;

            // Standard box height

            // Timing.
            GUI.Box(new Rect(rightColumnnX, currentY, BoxWidth, s_timingBoxHeight), Timing.CurrentTime, s_style);
            currentY += s_timingBoxHeight + ScreenMargin;

            // Asset loader box.
            GUI.Box(new Rect(rightColumnnX, currentY, BoxWidth, s_assetBoxHeight), AssetLoaderText.ToString(), s_style);
            currentY += s_assetBoxHeight + ScreenMargin;

            // Memory status (Windows only).
            if (IsWindows)
            {
                Rect memoryRect = new Rect(rightColumnnX, currentY, BoxWidth, s_memoryBoxHeight);
                GUI.Box(memoryRect, s_memoryText.ToString(), s_style);
                currentY += s_memoryBoxHeight + ScreenMargin;
            }

            // Thread status.
            GUI.Box(new Rect(rightColumnnX, currentY, BoxWidth, s_threadBoxHeight), ThreadText.ToString(), s_style);
        }

        /// <summary>
        /// Performs disposal actions.
        /// </summary>
        internal void Dispose()
        {
            // Stop text update coroutine.
            _loading = false;

            // Revert Harmony patches.
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetText");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");
            PatcherManager<Patcher>.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnGUI");

            // Destroy image object.
            if (_imageMaterial != null)
            {
                UnityEngine.Object.Destroy(_imageMaterial);
            }

            // Set references to null.
            _imageMesh = null;
            _imageMaterial = null;
            _animationMesh = null;
            _animationMaterial = null;
            _barBGMaterial = null;
            _barFGMaterial = null;
            _scenesAndAssetsStatus = null;

            // Clear status.
            _imageLoaded = _animationLoaded = false;

            // Clear instance.
            s_instance = null;
        }

        /// <summary>
        /// Sets current asset loading progress.
        /// </summary>
        /// <param name="min">Minimum current progress bound.</param>
        /// <param name="max">Maximum current progress bound.</param>
        /// <param name="assetsCount">Loaded assets count.</param>
        /// <param name="assetsTotal">Total assets.</param>
        /// <param name="beginMillis">Start time (milliseconds elapsed).</param>
        /// <param name="nowMillis">Current time (milliseconds elapsed).</param>
        internal void SetProgress(float min, float max, int assetsCount, int assetsTotal, int beginMillis, int nowMillis)
        {
            // Set current progress bounds.
            _minProgress = min;
            _maxProgress = max;

            // Display asset loading count and time.
            if (assetsCount > 0 && nowMillis > beginMillis)
            {
                // Set asset loader text.
                AssetLoaderText.Length = _assetTitleLength;
                AssetLoaderText.Append(assetsCount);
                AssetLoaderText.Append(" / ");
                AssetLoaderText.Append(assetsTotal);
                AssetLoaderText.AppendLine();
                AssetLoaderText.Append((assetsCount * 1000f / (nowMillis - beginMillis)).ToString("G3"));
                AssetLoaderText.Append(' ');
                AssetLoaderText.Append(_perSecondString);
            }
        }

        /// <summary>
        /// Called by LevelLoader if the simulation fails.
        /// </summary>
        /// <param name="message">Simulation failure message.</param>
        internal void SimulationFailed(string message)
        {
            _simulationFailedMessage = "<color=red>" + Translations.Translate("SIMULATION_FAILED") + ": " + message + "</color>";
        }

        /// <summary>
        /// Updates displayed progress.
        /// </summary>
        private void Progress()
        {
            float animationProgress = (float)_targetProgressField.GetValue(_loadingAnimiation);
            if (animationProgress >= 0f)
            {
                _progress += (Mathf.Clamp01(animationProgress + 0.04f) - _progress) * 0.2f;
            }

            // Clamp progress to current min and max bounds.
            _progress = Mathf.Clamp(_progress, _minProgress, _maxProgress);
        }

        /// <summary>
        /// Screen text display update coroutine.
        /// </summary>
        /// <returns>Coroutine IEnumerator yield.</returns>
        private IEnumerator UpdateText()
        {
            // Set up text generation monitors.
            LoadingManager loadingManager = Singleton<LoadingManager>.instance;
            ThreadStatus mainThreadStatus = new ThreadStatus(loadingManager.m_loadingProfilerMain, "MAIN");
            ThreadStatus simulationStatus = new ThreadStatus(loadingManager.m_loadingProfilerSimulation, "SIMULATION");

            // Memory status is for Windows only.
            MemoryStatus memoryStatus = IsWindows ? new MemoryStatus() : null;

            // Update every quarter second.
            WaitForSeconds wait = new WaitForSeconds(0.25f);

            // Set asset loading text title.
            AssetLoaderText.Length = 0;
            AssetLoaderText.Append("<color=white>");
            AssetLoaderText.Append(Translations.Translate("CUSTOM_ASSETS_LOADED"));
            AssetLoaderText.AppendLine("</color>");
            AssetLoaderText.AppendLine();
            _assetTitleLength = AssetLoaderText.Length;
            _perSecondString = Translations.Translate("PER_SECOND");

            yield return null;

            while (_loading)
            {
                // Scenes and assets text.
                s_scenesAndAssetsText = _scenesAndAssetsStatus.Text;

                // Thread monitoring text.
                ThreadText.Length = 0;
                ThreadText.Append(mainThreadStatus.Text);
                ThreadText.AppendLine();
                ThreadText.Append(simulationStatus.Text);

                // If any simulation failed message is set, display it.
                if (_simulationFailedMessage != null)
                {
                    ThreadText.Append(_simulationFailedMessage);
                }

                // Memory useage text.
                if (memoryStatus != null)
                {
                    s_memoryText = memoryStatus.Text;
                }

                // Wait for next update.
                yield return wait;
            }
        }
    }
}
