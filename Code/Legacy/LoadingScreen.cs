using System;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;

namespace LoadingScreenMod
{
	public sealed class LoadingScreen : DetourUtility<LoadingScreen>
	{
		private const float rotationSpeed = 100f;

		private const float animationScale = 0.2f;

		private const float progressInterval = 0.333333343f;

		private float timer;

		private float progress;

		private float meshTime;

		private float progressTime;

		private float minProgress;

		private float maxProgress = 1f;

		private int meshUpdates;

		internal readonly float meshWidth = Screen.width / 3;

		internal readonly float meshHeight = 3 * Screen.height / 4;

		private Mesh imageMesh;

		private Material imageMaterial;

		private float imageScale;

		private bool imageLoaded;

		private Mesh animationMesh;

		private Material animationMaterial;

		private Material barBGMaterial;

		private Material barFGMaterial;

		private bool animationLoaded;

		private Mesh bgMesh = CreateQuads();

		private Material bgMaterial = CreateMaterial(new Color(0f, 0f, 0f, 0.6f));

		private bool bgLoaded;

		internal UIFont uifont;

		private Material textMaterial;

		private Text[] texts;

		private bool fontLoaded;

		private readonly FieldInfo targetProgressField = typeof(LoadingAnimation).GetField("m_targetProgress", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

		private readonly LoadingAnimation la = Singleton<LoadingManager>.instance.LoadingAnimationComponent;

		private readonly Camera camera;

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

		private LoadingScreen()
		{
			init(typeof(LoadingAnimation), "SetImage");
			init(typeof(LoadingAnimation), "SetText");
			init(typeof(LoadingAnimation), "OnEnable");
			init(typeof(LoadingAnimation), "OnDisable");
			init(typeof(LoadingAnimation), "Update");
			init(typeof(LoadingAnimation), "OnPostRender");
			camera = la.GetComponent<Camera>();
			bgLoaded = bgMesh != null && bgMaterial != null;
		}

		internal void Setup()
		{
			UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Combine(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
			Font.textureRebuilt += FontTextureRebuilt;
			animationMesh = (Mesh)Util.Get(la, "m_animationMesh");
			animationMaterial = (Material)Util.Get(la, "m_animationMaterial");
			barBGMaterial = (Material)Util.Get(la, "m_barBGMaterial");
			barFGMaterial = (Material)Util.Get(la, "m_barFGMaterial");
			animationLoaded = (bool)Util.Get(la, "m_animationLoaded");
			Deploy();
			SetFont();
		}

		internal override void Dispose()
		{
			UIFontManager.callbackRequestCharacterInfo = (UIFontManager.CallbackRequestCharacterInfo)Delegate.Remove(UIFontManager.callbackRequestCharacterInfo, new UIFontManager.CallbackRequestCharacterInfo(RequestCharacterInfo));
			Font.textureRebuilt -= FontTextureRebuilt;
			base.Dispose();
			if (imageMaterial != null)
			{
				UnityEngine.Object.Destroy(imageMaterial);
			}
			Text[] array = texts;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Dispose();
			}
			if (textMaterial != null)
			{
				UnityEngine.Object.Destroy(textMaterial);
			}
			if (bgMesh != null)
			{
				UnityEngine.Object.Destroy(bgMesh);
			}
			if (bgMaterial != null)
			{
				UnityEngine.Object.Destroy(bgMaterial);
			}
			imageMesh = null;
			imageMaterial = null;
			animationMesh = null;
			animationMaterial = null;
			barBGMaterial = null;
			barFGMaterial = null;
			textMaterial = null;
			bgMesh = null;
			bgMaterial = null;
			imageLoaded = (animationLoaded = (fontLoaded = (bgLoaded = false)));
		}


		// TODO: this needs to be updated to match gamecode!
		//[HarmonyPatch(typeof(LoadingAnimation), nameof(LoadingAnimation.SetImage))]
		//[HarmonyPrefix]
		public static bool SetImage(Mesh mesh, Material material, float scale, bool showAnimation)
		{
			LoadingScreen loadingScreen = Instance<LoadingScreen>.instance;
			if (loadingScreen.imageMaterial != null)
			{
				UnityEngine.Object.Destroy(loadingScreen.imageMaterial);
			}
			loadingScreen.imageMesh = mesh;
			loadingScreen.imageMaterial = new Material(material);
			loadingScreen.imageScale = scale;
			loadingScreen.imageLoaded = true;
			return false;
		}

		//[HarmonyPatch(typeof(LoadingAnimation), nameof(LoadingAnimation.SetText))]
		//[HarmonyPrefix]
		public static bool SetText(UIFont font, Color color, float size, string title, string text)
		{
			return false;
		}

		public void SetFont()
		{
			try
			{
				uifont = UIView.GetAView().defaultFont;
				textMaterial = new Material(uifont.material);
				UIFontManager.Invalidate(uifont);
				List<Text> list = new List<Text>(5);
				list.Add(new Text(new Vector3(-1.3f, 0.7f, 10f), new DualProfilerSource(LoadingScreenModRevisited.Translations.Translate("SCENES_AND_ASSETS"), 36)));
				list.Add(new Text(new Vector3(-0.33f, -0.52f, 10f), new SimpleProfilerSource(LoadingScreenModRevisited.Translations.Translate("MAIN"), Singleton<LoadingManager>.instance.m_loadingProfilerMain)));
				list.Add(new Text(new Vector3(-0.33f, -0.62f, 10f), new SimpleProfilerSource(LoadingScreenModRevisited.Translations.Translate("SIMULATION"), Singleton<LoadingManager>.instance.m_loadingProfilerSimulation)));
				list.Add(new Text(new Vector3(-0.12f, 0.7f, 10f), new LineSource(LoadingScreenModRevisited.Translations.Translate("ASSETS_LOADER"), 2, LoadingScreenModRevisited.LevelLoader.AssetLoadingActive)));
				list.Add(new Text(new Vector3(-0.08f, 0.43f, 10f), new TimeSource(), 1.4f));
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
				{
					list.Add(new Text(new Vector3(-0.08f, 0.32f, 10f), new MemorySource(), 1.4f));
				}
				texts = list.ToArray();
				fontLoaded = uifont != null;
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Font setup failed");
				Debug.LogException(exception);
			}
		}

		private void RequestCharacterInfo()
		{
			UIDynamicFont uIDynamicFont = uifont as UIDynamicFont;
			if (!(uIDynamicFont == null) && UIFontManager.IsDirty(uIDynamicFont))
			{
				uIDynamicFont.AddCharacterRequest("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890.-:", 1, FontStyle.Normal);
			}
		}

		private void FontTextureRebuilt(Font font)
		{
			if (uifont != null && font == uifont.baseFont)
			{
				meshTime = -1f;
				Text[] array = texts;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Clear();
				}
			}
		}

		//[HarmonyPatch(typeof(LoadingAnimation), "OnEnable")]
		//[HarmonyPrefix]
		private static bool OnEnable()
		{
			Instance<LoadingScreen>.instance.camera.enabled = true;
			Instance<LoadingScreen>.instance.camera.clearFlags = CameraClearFlags.Color;
			return false;
		}

		//[HarmonyPatch(typeof(LoadingAnimation), "OnDisable")]
		//[HarmonyPrefix]
		private static bool OnDisable()
		{
			Instance<LoadingScreen>.instance.camera.enabled = false;
			Instance<LoadingScreen>.instance.Dispose();
			return false;
		}

		internal void SetProgress(float min, float max, int assetsCount, int assetsTotal, int beginMillis, int nowMillis)
		{
			minProgress = min;
			maxProgress = max;
			if (assetsCount > 0 && nowMillis > beginMillis)
			{
				LineSource loaderSource = LoaderSource;
				loaderSource.Add(assetsCount + " / " + assetsTotal);
				loaderSource.Add(((float)assetsCount * 1000f / (float)(nowMillis - beginMillis)).ToString("G3") + LoadingScreenModRevisited.Translations.Translate("PER_SECOND"));
			}
		}

		private void Progress()
		{
			float num = (float)targetProgressField.GetValue(la);
			if (num >= 0f)
			{
				progress += (Mathf.Clamp01(num + 0.04f) - progress) * 0.2f;
			}
			progress = Mathf.Clamp(progress, minProgress, maxProgress);
		}

		//[HarmonyPatch(typeof(LoadingAnimation), "Update")]
		//[HarmonyPrefix]
		private static bool Update()
		{
			LoadingScreen loadingScreen = Instance<LoadingScreen>.instance;
			float num = Mathf.Min(0.125f, Time.deltaTime);
			loadingScreen.timer += num;
			float time = Time.time;
			if (time - loadingScreen.progressTime >= 0.333333343f)
			{
				loadingScreen.progressTime = time;
				loadingScreen.Progress();
			}
			return false;
		}

		//[HarmonyPatch(typeof(LoadingAnimation), "OnPostRender")]
		//[HarmonyPrefix]
		private static bool OnPostRender()
		{
			LoadingScreen loadingScreen = Instance<LoadingScreen>.instance;
			if (loadingScreen.imageLoaded)
			{
				Texture2D texture2D = loadingScreen.imageMaterial.mainTexture as Texture2D;
				float num = ((texture2D != null) ? ((float)texture2D.width / (float)texture2D.height) : 1f);
				float num2 = 2f * loadingScreen.imageScale;
				if (loadingScreen.imageMaterial.SetPass(0))
				{
					Graphics.DrawMeshNow(loadingScreen.imageMesh, Matrix4x4.TRS(new Vector3(0f, 0f, 10f), Quaternion.identity, new Vector3(num2 * num, num2, num2)));
				}
			}
			if (loadingScreen.animationLoaded)
			{
				Quaternion q = Quaternion.AngleAxis(loadingScreen.timer * 100f, Vector3.back);
				loadingScreen.animationMaterial.color = new Color(0.3f, 0.6f, 1f, 1f);
				Mesh mesh = loadingScreen.animationMesh;
				if (loadingScreen.animationMaterial.SetPass(0))
				{
					Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(new Vector3(0f, -0.04f, 10f), q, new Vector3(0.2f, 0.2f, 0.2f)));
				}
				Vector3 pos = new Vector3(0f, -0.2f, 10f);
				Vector3 s = new Vector3(0.4f, 0.025f, 0.2f);
				loadingScreen.barBGMaterial.color = new Color(1f, 1f, 1f, 1f);
				if (loadingScreen.barBGMaterial.SetPass(0))
				{
					Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(pos, Quaternion.identity, s));
				}
				s.x *= 0.9875f;
				s.y *= 0.8f;
				pos.x -= s.x * (1f - loadingScreen.progress) * 0.5f;
				s.x *= loadingScreen.progress;
				loadingScreen.barFGMaterial.color = new Color(1f, 1f, 1f, 1f);
				if (loadingScreen.barFGMaterial.SetPass(0))
				{
					Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(pos, Quaternion.identity, s));
				}
			}
			if (!loadingScreen.imageLoaded || !loadingScreen.fontLoaded)
			{
				return false;
			}
			if (loadingScreen.bgLoaded && loadingScreen.bgMaterial.SetPass(0))
			{
				Graphics.DrawMeshNow(loadingScreen.bgMesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));
			}
			float time = Time.time;
			if (time - loadingScreen.meshTime >= 0.333333343f || loadingScreen.meshUpdates < 3)
			{
				loadingScreen.meshTime = time;
				loadingScreen.meshUpdates++;
				Text[] array = loadingScreen.texts;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].UpdateText();
				}
			}
			if (loadingScreen.textMaterial.SetPass(0))
			{
				Text[] array = loadingScreen.texts;
				foreach (Text text in array)
				{
					Graphics.DrawMeshNow(text.mesh, Matrix4x4.TRS(text.pos, Quaternion.identity, text.Scale));
				}
			}

			return false;
		}

		private static Mesh CreateQuads()
		{
			List<Vector3> list = new List<Vector3>(16);
			List<int> list2 = new List<int>(24);
			CreateQuad(-1.35f, 0.75f, 0.86f, 1.5f, list, list2);
			CreateQuad(-0.38f, -0.47f, 0.75f, 0.28f, list, list2);
			CreateQuad(-0.17f, 0.75f, 0.34f, 0.2f, list, list2);
			CreateQuad(-0.17f, 0.48f, 0.47f, 0.3f, list, list2);
			return new Mesh
			{
				name = "BG Quads",
				vertices = list.ToArray(),
				triangles = list2.ToArray()
			};
		}

		private static void CreateQuad(float x, float y, float w, float h, List<Vector3> vertices, List<int> triangles)
		{
			int count = vertices.Count;
			vertices.Add(new Vector3(x, y - h, 10f));
			vertices.Add(new Vector3(x + w, y - h, 10f));
			vertices.Add(new Vector3(x, y, 10f));
			vertices.Add(new Vector3(x + w, y, 10f));
			triangles.Add(count);
			triangles.Add(count + 2);
			triangles.Add(count + 1);
			triangles.Add(count + 2);
			triangles.Add(count + 3);
			triangles.Add(count + 1);
		}

		private static Material CreateMaterial(Color color)
		{
			return new Material(Shader.Find("Custom/Loading/AlphaBlend"))
			{
				name = "BG Material",
				color = color,
				hideFlags = HideFlags.HideAndDontSave
			};
		}
	}
}
