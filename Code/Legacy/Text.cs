using System;
using ColossalFramework.UI;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class Text
	{
		internal readonly Vector3 pos;

		internal readonly Source source;

		private readonly float scale;

		private string text = string.Empty;

		internal Mesh mesh = new Mesh();

		private const float baseScale = 0.002083333f;

		internal Vector3 Scale => new Vector3(scale, scale, scale);

		internal Text(Vector3 pos, Source source, float scaleFactor = 1f)
		{
			this.pos = pos;
			this.source = source;
			scale = 0.002083333f * scaleFactor;
		}

		internal void Clear()
		{
			text = string.Empty;
		}

		internal void Dispose()
		{
			if (mesh != null)
			{
				UnityEngine.Object.Destroy(mesh);
			}
			mesh = null;
		}

		internal void UpdateText()
		{
			string text = source.CreateText();
			if (text != null && text != this.text)
			{
				this.text = text;
				GenerateMesh();
			}
		}

		private void GenerateMesh()
		{
			UIFont uifont = Instance<LoadingScreen>.instance.uifont;
			if (uifont == null)
			{
				return;
			}
			UIFontRenderer uIFontRenderer = uifont.ObtainRenderer();
			UIRenderData uIRenderData = UIRenderData.Obtain();
			try
			{
				mesh.Clear();
				uIFontRenderer.defaultColor = Color.white;
				uIFontRenderer.textScale = 1f;
				uIFontRenderer.pixelRatio = 1f;
				uIFontRenderer.processMarkup = true;
				uIFontRenderer.multiLine = true;
				uIFontRenderer.maxSize = new Vector2(Instance<LoadingScreen>.instance.meshWidth, Instance<LoadingScreen>.instance.meshHeight);
				uIFontRenderer.shadow = false;
				uIFontRenderer.Render(text, uIRenderData);
				mesh.vertices = uIRenderData.vertices.ToArray();
				mesh.colors32 = uIRenderData.colors.ToArray();
				mesh.uv = uIRenderData.uvs.ToArray();
				mesh.triangles = uIRenderData.triangles.ToArray();
			}
			catch (Exception exception)
			{
				Util.DebugPrint("Cannot generate font mesh");
				Debug.LogException(exception);
			}
			finally
			{
				uIFontRenderer.Dispose();
				uIRenderData.Dispose();
			}
		}
	}
}
