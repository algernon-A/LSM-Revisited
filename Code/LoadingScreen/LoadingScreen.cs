using ColossalFramework;
using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	/// <summary>
    /// The actual loading screen itself.
    /// </summary>
	public sealed class LoadingScreen
	{
		// Screen dimensions.
		internal readonly float meshWidth = Screen.width / 3;
		internal readonly float meshHeight = 3 * Screen.height / 4;

		// Progress timers.
		private float timer, progress, meshTime, progressTime, minProgress;
		private float maxProgress = 1f;

		// Mesh update counters.
		private int meshUpdates;

		// Background image components.
		private Mesh imageMesh;
		private Material imageMaterial;
		private float imageScale;

		// Animation components.
		private Mesh animationMesh;
		private Material animationMaterial;
		private Material barBGMaterial;
		private Material barFGMaterial;

		// Text components.
		internal UIFont uifont;
		private Material textMaterial;
		private LoadingScreenText[] texts;

		// Overlay components.
		private Mesh overlayMesh;
		private Material overlayMaterial;

		// Loading animation.
		private readonly LoadingAnimation loadingAnimiation;
		private readonly FieldInfo targetProgressField = typeof(LoadingAnimation).GetField("m_targetProgress", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		// Camera instance reference.
		private readonly Camera camera;

		// Status flags.
		private bool imageLoaded, animationLoaded, overlayLoaded, fontLoaded;

		// Instance reference.
		internal static LoadingScreen instance;


		/// <summary>
		/// Text source - simulation.
		/// </summary>
		internal SimpleProfilerSource SimulationSource
		{
			get
			{
				if (texts == null || texts.Length < 3)
				{
					return null;
				}
				return texts[2].source as SimpleProfilerSource;
			}
		}

		/// <summary>
		/// Text source - asset loader.
		/// </summary>
		internal DualProfilerSource DualSource
		{
			get
			{
				if (texts == null || texts.Length < 1)
				{
					return null;
				}
				return texts[0].source as DualProfilerSource;
			}
		}


		/// <summary>
		/// Text source - loader.
		/// </summary>
		internal LineSource LoaderSource
		{
			get
			{
				if (texts == null || texts.Length < 4)
				{
					return null;
				}
				return texts[3].source as LineSource;
			}
		}


		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.SetImage.
		/// Sets the loading animation image - this patch ensure the image is set here and not in the game instance.
		/// Patch applied manually.
		/// </summary>
		/// <param name="mesh">Image mesh to set</param>
		/// <param name="material">Image material to set</param>
		/// <param name="scale">Image scale</param>
		/// <returns>Always false (never execute original method)</returns>
		public static bool SetImage(Mesh mesh, Material material, float scale)
		{
			// Destroy any existing material.
			if (instance.imageMaterial != null)
			{
				UnityEngine.Object.Destroy(instance.imageMaterial);
			}

			// Apply arguments.
			instance.imageMesh = mesh;
			instance.imageMaterial = new Material(material);
			instance.imageScale = scale;

			// Check if we're using custom background images.
			if (BackgroundImage.ImageMode != ImageMode.Standard)
			{
				// Try to get imgur image.
				Material customMaterial = BackgroundImage.GetImage(material);
				if (customMaterial != null)
				{
					// Success - apply new material instead.
					instance.imageMaterial = customMaterial;
				}
			}

			// Set status flag.
			instance.imageLoaded = true;

			// Always pre-empt original method.
			return false;
		}


		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.SetText.
		/// Used to completely nullify the original game method.
		/// </summary>
		/// <returns>Always false (never execute original method)</returns>
		public static bool SetText() => false;


		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.OnEnable.
		/// Used to ensure camera status.
		/// </summary>
		/// <returns>Always false (never execute original method)</returns>
		public static bool OnEnable()
		{
			instance.camera.enabled = true;
			instance.camera.clearFlags = CameraClearFlags.Color;

			// Always pre-empt original method.
			return false;
		}


		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.OnDisable.
		/// Used to trigger disposal of instance.
		/// </summary>
		/// <returns>Always false (never execute original method)</returns>
		public static bool OnDisable()
		{
			instance.camera.enabled = false;
			instance.Dispose();

			// Always pre-empt original method.
			return false;
		}

		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.Update.
		/// Used to update progress bar.
		/// </summary>
		/// <returns>Always false (never execute original method)</returns>
		public static bool Update()
		{
			// Update timer (cap at 0.125 seconds per update in case something hung).
			LoadingScreen loadingScreen = instance;
			float deltaTime = Mathf.Min(0.125f, Time.deltaTime);
			loadingScreen.timer += deltaTime;

			// Update progress three times per second.
			float now = Time.time;
			if (now - loadingScreen.progressTime >= 0.333333343f)
			{
				loadingScreen.progressTime = now;
				loadingScreen.Progress();
			}

			// Always pre-empt original method.
			return false;
		}


		/// <summary>
		/// Harmony pre-emptive prefix patch for LoadingAnimation.OnPostRender.
		/// Used to draw the loading screen.
		/// </summary>
		/// <returns>Always false (never execute original method)</returns>
		public static bool OnPostRender()
		{
			// Draw background mesh.
			if (instance.imageLoaded)
			{
				Texture2D imageTexture = instance.imageMaterial.mainTexture as Texture2D;
				float aspectRatio = imageTexture != null ? (float)imageTexture.width / (float)imageTexture.height : 1f;
				float scale = 2f * instance.imageScale;
				if (instance.imageMaterial.SetPass(0))
				{
					Graphics.DrawMeshNow(instance.imageMesh, Matrix4x4.TRS(new Vector3(0f, 0f, 10f), Quaternion.identity, new Vector3(scale * aspectRatio, scale, scale)));
				}
			}

			// Draw loading animation.
			if (instance.animationLoaded)
            {
                Quaternion quaternion = Quaternion.AngleAxis(instance.timer * 100f, Vector3.back);
				instance.animationMaterial.color = new Color(0.3f, 0.6f, 1f, 1f);
                Mesh mesh = instance.animationMesh;

				// Draw animation mesh.
                if (instance.animationMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(new Vector3(0f, -0.04f, 10f), quaternion, new Vector3(0.2f, 0.2f, 0.2f)));
                }

				// Draw progress bar background mesh.
                Vector3 position = new Vector3(0f, -0.2f, 10f);
				Vector3 scale = new Vector3(0.4f, 0.025f, 0.2f);
				instance.barBGMaterial.color = new Color(1f, 1f, 1f, 1f);
                if (instance.barBGMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, Quaternion.identity, scale));
                }

				// Draw progress bar foreground mesh - adjusting starting scale to fit within background mesh.
                scale.x *= 0.9875f;
				scale.y *= 0.8f;

				// Scale position according to progress.
				position.x -= scale.x * (1f - instance.progress) * 0.5f;
				scale.x *= instance.progress;
				instance.barFGMaterial.color = new Color(1f, 1f, 1f, 1f);
                if (instance.barFGMaterial.SetPass(0))
                {
                    Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, Quaternion.identity, scale));
                }
            }

			// Don't do anything else if both the background image and the font haven't loaded.
            if (!instance.imageLoaded | !instance.fontLoaded)
			{
				// Always pre-empt original method.
				return false;
			}

			// Draw overlay mesh if it's ready.
			if (instance.overlayLoaded && instance.overlayMaterial.SetPass(0))
			{
				Graphics.DrawMeshNow(instance.overlayMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
			}

			// Update every 0.333 seconds.
			float time = Time.time;
			if (time - instance.meshTime >= 0.333333343f | instance.meshUpdates < 3)
			{
				instance.meshTime = time;
				++instance.meshUpdates;

				// Update texts.
				LoadingScreenText[] array = instance.texts;
				for (int i = 0; i < array.Length; ++i)
				{
					array[i].UpdateText();
				}
			}

			// Draw text.
			if (instance.textMaterial.SetPass(0))
			{
				LoadingScreenText[] array = instance.texts;
				foreach (LoadingScreenText text in array)
				{
					Graphics.DrawMeshNow(text.mesh, Matrix4x4.TRS(text.position, Quaternion.identity, text.Scale));
				}
			}

			// Always pre-empt original method.
			return false;
		}


		/// <summary>
		/// Constructor.
		/// </summary>
		internal LoadingScreen()
		{
			// Create overlay components.
			overlayMesh = CreateOverlayMesh();
			overlayMaterial = CreateOverlayMaterial(new Color(0f, 0f, 0f, 0.8f));
			overlayLoaded = overlayMesh != null & overlayMaterial != null;

			// Set instance references.
			loadingAnimiation = Singleton<LoadingAnimation>.instance;
			camera = loadingAnimiation.GetComponent<Camera>();

			// Get mesh and material instances from loading animation.
			animationMesh = (Mesh)Util.Get(loadingAnimiation, "m_animationMesh");
			animationMaterial = (Material)Util.Get(loadingAnimiation, "m_animationMaterial");
			barBGMaterial = (Material)Util.Get(loadingAnimiation, "m_barBGMaterial");
			barFGMaterial = (Material)Util.Get(loadingAnimiation, "m_barFGMaterial");
			animationLoaded = (bool)Util.Get(loadingAnimiation, "m_animationLoaded");

			// Set up font manager.
			UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Combine(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
			Font.textureRebuilt += FontTextureRebuilt;
			SetFont();

			// Apply Harmony patches to the loading animation.
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetText");
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
			Patcher.PrefixMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");
		}


		/// <summary>
		/// Performs disposal actions.
		/// </summary>
		internal void Dispose()
		{
			// Revert Harmony patches.
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetImage");
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "SetText");
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnEnable");
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnDisable");
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "Update");
			Patcher.UnpatchMethod(typeof(LoadingAnimation), typeof(LoadingScreen), "OnPostRender");

			// Restore font manager callback.
			UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Remove(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
			Font.textureRebuilt -= FontTextureRebuilt;

			// Destroy image object.
			if (imageMaterial != null)
			{
				UnityEngine.Object.Destroy(imageMaterial);
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
			imageMesh = null;
			imageMaterial = null;
			animationMesh = null;
			animationMaterial = null;
			barBGMaterial = null;
			barFGMaterial = null;
			textMaterial = null;
			overlayMesh = null;
			overlayMaterial = null;

			// Clear status.
			imageLoaded = animationLoaded = fontLoaded = overlayLoaded = false;

			// Clear instance.
			instance = null;
		}


		/// <summary>
		/// Sets current asset loading progress.
		/// </summary>
		/// <param name="min">Minimum current progress bound</param>
		/// <param name="max">Maximum current progress bound</param>
		/// <param name="assetsCount">Loaded assets count</param>
		/// <param name="assetsTotal">Total assets</param>
		/// <param name="beginMillis">Start time (milliseconds elapsed)</param>
		/// <param name="nowMillis">Current time (milliseconds elapsed)</param>
		internal void SetProgress(float min, float max, int assetsCount, int assetsTotal, int beginMillis, int nowMillis)
		{
			// Set current progress bounds.
			minProgress = min;
			maxProgress = max;

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
			float animationProgress = (float)targetProgressField.GetValue(loadingAnimiation);
			if (animationProgress >= 0f)
			{
				progress += (Mathf.Clamp01(animationProgress + 0.04f) - progress) * 0.2f;
			}

			// Clamp progress to current min and max bounds.
			progress = Mathf.Clamp(progress, minProgress, maxProgress);
		}


		/// <summary>
		/// Font character request callback.
		/// </summary>
		private void RequestCharacterInfo()
		{
			// Request required characters if we need to.
			UIDynamicFont uIDynamicFont = uifont as UIDynamicFont;
			if (!(uIDynamicFont == null) && UIFontManager.IsDirty(uIDynamicFont))
			{
				uIDynamicFont.AddCharacterRequest("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.-:", 1, FontStyle.Normal);
			}
		}


		/// <summary>
		/// Font texture rebuild callback
		/// </summary>
		/// <param name="font">Rebuilt font</param>
		private void FontTextureRebuilt(Font font)
		{
			// Clear existing text if this font has been rebuilt. 
			if (uifont != null && font == uifont.baseFont)
			{
				meshTime = -1f;
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
				uifont = UIView.GetAView().defaultFont;
				textMaterial = new Material(uifont.material);
				UIFontManager.Invalidate(uifont);

				// Set text titles.
				List<LoadingScreenText> list = new List<LoadingScreenText>(5);
				list.Add(new LoadingScreenText(new Vector3(-1.3f, 0.7f, 10f), new DualProfilerSource(Translations.Translate("SCENES_AND_ASSETS"), 36)));
				list.Add(new LoadingScreenText(new Vector3(-0.33f, -0.52f, 10f), new SimpleProfilerSource(Translations.Translate("MAIN"), Singleton<LoadingManager>.instance.m_loadingProfilerMain)));
				list.Add(new LoadingScreenText(new Vector3(-0.33f, -0.62f, 10f), new SimpleProfilerSource(Translations.Translate("SIMULATION"), Singleton<LoadingManager>.instance.m_loadingProfilerSimulation)));
				list.Add(new LoadingScreenText(new Vector3(-0.12f, 0.7f, 10f), new LineSource(Translations.Translate("ASSETS_LOADER"), 2, LevelLoader.AssetLoadingActive)));
				list.Add(new LoadingScreenText(new Vector3(-0.08f, 0.43f, 10f), new TimeSource(), 1.4f));

				// Set memory usage title if we're on Windows.
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
				{
					list.Add(new LoadingScreenText(new Vector3(-0.08f, 0.32f, 10f), new MemorySource(), 1.4f));
				}

				// Store texts in array.
				texts = list.ToArray();

				// Set status flag.
				fontLoaded = uifont != null;
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception during font setup");
			}
		}


		/// <summary>
		/// Creates the overlay mesh.
		/// </summary>
		/// <returns>New overlay mesh</returns>
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
				triangles = triangles.ToArray()
			};
		}


		/// <summary>
		/// Creats a mesh quad with the given parameters.
		/// </summary>
		/// <param name="x">X position</param>
		/// <param name="y">Y position</param>
		/// <param name="w">Quad width</param>
		/// <param name="h">Quad height</param>
		/// <param name="vertices">Created vertices will be added to this list</param>
		/// <param name="triangles">Created triangles will be added to this list.</param>
		private void CreateQuad(float x, float y, float width, float height, List<Vector3> vertices, List<int> triangles)
		{
			int count = vertices.Count;

			// Create basic quad vertices.
			vertices.Add(new Vector3(x, y - height, 10f));
			vertices.Add(new Vector3(x + width, y - height, 10f));
			vertices.Add(new Vector3(x, y, 10f));
			vertices.Add(new Vector3(x + width, y, 10f));

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
		/// <param name="color">Material color</param>
		/// <returns>New overlay material</returns>
		private Material CreateOverlayMaterial(Color color)
		{
			return new Material(Shader.Find("Custom/Loading/AlphaBlend"))
			{
				name = "OverlayMaterial",
				color = color,
				hideFlags = HideFlags.HideAndDontSave
			};
		}
	}
}
