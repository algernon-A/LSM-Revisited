// <copyright file="LoadingScreen.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using AlgernonCommons;
    using AlgernonCommons.Translation;
    using ColossalFramework;
    using ColossalFramework.UI;
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

        /// <summary>
        /// Text font.
        /// </summary>
        internal UIFont m_uifont;

        // Loading animation.
        private readonly LoadingAnimation _loadingAnimiation;
        private readonly FieldInfo _targetProgressField = typeof(LoadingAnimation).GetField("m_targetProgress", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        // Camera instance reference.
        private readonly Camera _camera;

        // Progress timers.
        private float _timer;
        private float _progress;
        private float _meshTime;
        private float _progressTime;
        private float _minProgress;
        private float _maxProgress = 1f;

        // Mesh update counters.
        private int _meshUpdates;

        // Background image components.
        private Mesh _imageMesh;
        private Material _imageMaterial;
        private float _imageScale;

        // Animation components.
        private Mesh _animationMesh;
        private Material _animationMaterial;
        private Material _barBGMaterial;
        private Material _barFGMaterial;

        // Text components.
        private Material textMaterial;
        private LoadingScreenText[] texts;

        // Overlay components.
        private Mesh overlayMesh;
        private Material overlayMaterial;

        // Status flags.
        private bool _imageLoaded;
        private bool _animationLoaded;
        private bool _overlayLoaded;
        private bool _fontLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingScreen"/> class.
        /// </summary>
        internal LoadingScreen()
        {
            // Create overlay components.
            overlayMesh = CreateOverlayMesh();
            overlayMaterial = CreateOverlayMaterial(new Color(0f, 0f, 0f, 0.8f));
            _overlayLoaded = overlayMesh != null & overlayMaterial != null;

            // Set instance references.
            _loadingAnimiation = Singleton<LoadingAnimation>.instance;
            _camera = _loadingAnimiation.GetComponent<Camera>();

            // Get mesh and material instances from loading animation.
            _animationMesh = (Mesh)Util.Get(_loadingAnimiation, "m_animationMesh");
            _animationMaterial = (Material)Util.Get(_loadingAnimiation, "m_animationMaterial");
            _barBGMaterial = (Material)Util.Get(_loadingAnimiation, "m_barBGMaterial");
            _barFGMaterial = (Material)Util.Get(_loadingAnimiation, "m_barFGMaterial");
            _animationLoaded = (bool)Util.Get(_loadingAnimiation, "m_animationLoaded");

            // Set up font manager.
            UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Combine(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
            Font.textureRebuilt += FontTextureRebuilt;
            SetFont();

            // Apply Harmony patches to the loading animation.
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetText");
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
            Patcher.Instance.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");
        }

        /// <summary>
        /// Gets the simulation text source.
        /// </summary>
        internal SimpleProfilerSource SimulationSource
        {
            get
            {
                if (texts == null || texts.Length < 3)
                {
                    return null;
                }

                return texts[2].m_source as SimpleProfilerSource;
            }
        }

        /// <summary>
        /// Gets the asset loader text source.
        /// </summary>
        internal DualProfilerSource DualSource
        {
            get
            {
                if (texts == null || texts.Length < 1)
                {
                    return null;
                }

                return texts[0].m_source as DualProfilerSource;
            }
        }

        /// <summary>
        /// Gets the loader text source.
        /// </summary>
        internal LineSource LoaderSource
        {
            get
            {
                if (texts == null || texts.Length < 4)
                {
                    return null;
                }

                return texts[3].m_source as LineSource;
            }
        }

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
            s_instance._imageScale = scale;

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
                Texture2D imageTexture = s_instance._imageMaterial.mainTexture as Texture2D;
                float aspectRatio = imageTexture != null ? (float)imageTexture.width / (float)imageTexture.height : 1f;
                float scale = 2f * s_instance._imageScale;
                if (s_instance._imageMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(s_instance._imageMesh, Matrix4x4.TRS(new Vector3(0f, 0f, 10f), Quaternion.identity, new Vector3(scale * aspectRatio, scale, scale)));
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

            // Don't do anything else if both the background image and the font haven't loaded.
            if (!s_instance._imageLoaded | !s_instance._fontLoaded)
            {
                // Always pre-empt original method.
                return false;
            }

            // Draw overlay mesh if it's ready.
            if (s_instance._overlayLoaded && s_instance.overlayMaterial.SetPass(0))
            {
                Graphics.DrawMeshNow(s_instance.overlayMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
            }

            // Update every 0.333 seconds.
            float time = Time.time;
            if (time - s_instance._meshTime >= 0.333333343f | s_instance._meshUpdates < 3)
            {
                s_instance._meshTime = time;
                ++s_instance._meshUpdates;

                // Update texts.
                LoadingScreenText[] array = s_instance.texts;
                for (int i = 0; i < array.Length; ++i)
                {
                    array[i].UpdateText();
                }
            }

            // Draw text.
            if (s_instance.textMaterial.SetPass(0))
            {
                LoadingScreenText[] array = s_instance.texts;
                foreach (LoadingScreenText text in array)
                {
                    Graphics.DrawMeshNow(text.m_mesh, Matrix4x4.TRS(text.m_position, Quaternion.identity, text.m_scale));
                }
            }

            // Always pre-empt original method.
            return false;
        }

        /// <summary>
        /// Performs disposal actions.
        /// </summary>
        internal void Dispose()
        {
            // Revert Harmony patches.
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetText");
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
            Patcher.Instance.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");

            // Restore font manager callback.
            UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Remove(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
            Font.textureRebuilt -= FontTextureRebuilt;

            // Destroy image object.
            if (_imageMaterial != null)
            {
                UnityEngine.Object.Destroy(_imageMaterial);
            }

            // Clear texts.
            LoadingScreenText[] array = texts;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].Dispose();
            }

            // Destroy materials and meshes.
            if (textMaterial != null)
            {
                UnityEngine.Object.Destroy(textMaterial);
            }

            if (overlayMesh != null)
            {
                UnityEngine.Object.Destroy(overlayMesh);
            }

            if (overlayMaterial != null)
            {
                UnityEngine.Object.Destroy(overlayMaterial);
            }

            // Set references to null.
            _imageMesh = null;
            _imageMaterial = null;
            _animationMesh = null;
            _animationMaterial = null;
            _barBGMaterial = null;
            _barFGMaterial = null;
            textMaterial = null;
            overlayMesh = null;
            overlayMaterial = null;

            // Clear status.
            _imageLoaded = _animationLoaded = _fontLoaded = _overlayLoaded = false;

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
                LineSource loaderSource = LoaderSource;
                loaderSource.Add(assetsCount + " / " + assetsTotal);
                loaderSource.Add((assetsCount * 1000f / (nowMillis - beginMillis)).ToString("G3") + ' ' + Translations.Translate("PER_SECOND"));
            }
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
        /// Font character request callback.
        /// </summary>
        private void RequestCharacterInfo()
        {
            // Request required characters if we need to.
            UIDynamicFont uIDynamicFont = m_uifont as UIDynamicFont;
            if (!(uIDynamicFont == null) && UIFontManager.IsDirty(uIDynamicFont))
            {
                uIDynamicFont.AddCharacterRequest("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.-:", 1, FontStyle.Normal);
            }
        }

        /// <summary>
        /// Font texture rebuild callback.
        /// </summary>
        /// <param name="font">Rebuilt font.</param>
        private void FontTextureRebuilt(Font font)
        {
            // Clear existing text if this font has been rebuilt.
            if (m_uifont != null && font == m_uifont.baseFont)
            {
                _meshTime = -1f;
                LoadingScreenText[] array = texts;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Clear();
                }
            }
        }

        /// <summary>
        ///  Sets up the loading screen font.
        /// </summary>
        private void SetFont()
        {
            try
            {
                // Get font and text material.
                m_uifont = UIView.GetAView().defaultFont;
                textMaterial = new Material(m_uifont.material);
                UIFontManager.Invalidate(m_uifont);

                // Set text titles.
                List<LoadingScreenText> list = new List<LoadingScreenText>(5)
                {
                    new LoadingScreenText(new Vector3(-1.3f, 0.7f, 10f), new DualProfilerSource(Translations.Translate("SCENES_AND_ASSETS"), 36)),
                    new LoadingScreenText(new Vector3(-0.33f, -0.52f, 10f), new SimpleProfilerSource(Translations.Translate("MAIN"), Singleton<LoadingManager>.instance.m_loadingProfilerMain)),
                    new LoadingScreenText(new Vector3(-0.33f, -0.62f, 10f), new SimpleProfilerSource(Translations.Translate("SIMULATION"), Singleton<LoadingManager>.instance.m_loadingProfilerSimulation)),
                    new LoadingScreenText(new Vector3(-0.12f, 0.7f, 10f), new LineSource(Translations.Translate("ASSETS_LOADER"), 2, LevelLoader.AssetLoadingActive)),
                    new LoadingScreenText(new Vector3(-0.08f, 0.43f, 10f), new TimeSource(), 1.4f),
                };

                // Set memory usage title if we're on Windows.
                if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                {
                    list.Add(new LoadingScreenText(new Vector3(-0.08f, 0.32f, 10f), new MemorySource(), 1.4f));
                }

                // Store texts in array.
                texts = list.ToArray();

                // Set status flag.
                _fontLoaded = m_uifont != null;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception during font setup");
            }
        }

        /// <summary>
        /// Creates the overlay mesh.
        /// </summary>
        /// <returns>New overlay mesh.</returns>
        private Mesh CreateOverlayMesh()
        {
            // Verts and tris.
            List<Vector3> vertices = new List<Vector3>(16);
            List<int> triangles = new List<int>(24);

            // Create background quads.
            CreateQuad(-1.35f, 0.75f, 0.86f, 1.5f, vertices, triangles);
            CreateQuad(-0.38f, -0.47f, 0.75f, 0.28f, vertices, triangles);
            CreateQuad(-0.17f, 0.75f, 0.47f, 0.2f, vertices, triangles);
            CreateQuad(-0.17f, 0.48f, 0.47f, 0.3f, vertices, triangles);

            return new Mesh
            {
                name = "BG Quads",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
            };
        }

        /// <summary>
        /// Creats a mesh quad with the given parameters.
        /// </summary>
        /// <param name="xPos">X position.</param>
        /// <param name="yPos">Y position.</param>
        /// <param name="width">Quad width.</param>
        /// <param name="height">Quad height.</param>
        /// <param name="vertices">Created vertices will be added to this list.</param>
        /// <param name="triangles">Created triangles will be added to this list.</param>
        private void CreateQuad(float xPos, float yPos, float width, float height, List<Vector3> vertices, List<int> triangles)
        {
            int count = vertices.Count;

            // Create basic quad vertices.
            vertices.Add(new Vector3(xPos, yPos - height, 10f));
            vertices.Add(new Vector3(xPos + width, yPos - height, 10f));
            vertices.Add(new Vector3(xPos, yPos, 10f));
            vertices.Add(new Vector3(xPos + width, yPos, 10f));

            // Create two tris to make up quad.
            triangles.Add(count);
            triangles.Add(count + 2);
            triangles.Add(count + 1);
            triangles.Add(count + 2);
            triangles.Add(count + 3);
            triangles.Add(count + 1);
        }

        /// <summary>
        /// Creates the overlay material.
        /// </summary>
        /// <param name="color">Material color.</param>
        /// <returns>New overlay material.</returns>
        private Material CreateOverlayMaterial(Color color)
        {
            return new Material(Shader.Find("Custom/Loading/AlphaBlend"))
            {
                name = "OverlayMaterial",
                color = color,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
