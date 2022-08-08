namespace LoadingScreenModRevisited
{
	using System;
	using AlgernonCommons;
	using ColossalFramework.UI;
	using LoadingScreenMod;
	using UnityEngine;

	/// <summary>
	/// Loading screen text display.
	/// </summary>
	internal sealed class LoadingScreenText
	{
		// Display attributes.
		internal readonly Vector3 position;
		internal Mesh mesh = new Mesh();

		// Text source.
		internal readonly Source source;

		// Current text.
		private string text = string.Empty;

		// Display scale.
		private readonly float scale;


		/// <summary>
		/// Display scale.
		/// </summary>
		internal Vector3 Scale => new Vector3(scale, scale, scale);


		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="position">Text position</param>
		/// <param name="source">Text source</param>
		/// <param name="scaleFactor">Text scale</param>
		internal LoadingScreenText(Vector3 position, Source source, float scaleFactor = 1f)
		{
			this.position = position;
			this.source = source;
			scale = 0.002083333f * scaleFactor;
		}

		
		/// <summary>
		/// Clears the current text.
		/// </summary>
		internal void Clear()
		{
			text = string.Empty;
		}


		/// <summary>
		/// Disposes of the text mesh.
		/// </summary>
		internal void Dispose()
		{
			if (mesh != null)
			{
				UnityEngine.Object.Destroy(mesh);
			}
			mesh = null;
		}


		/// <summary>
		/// Updates the current text.
		/// </summary>
		internal void UpdateText()
		{
			// Get updated text from source.
			string text = source.CreateText();

			// Generate new mesh if text has changed.
			if (text != null && text != this.text)
			{
				this.text = text;
				GenerateMesh();
			}
		}


		/// <summary>
		/// Generates a text mesh for display.
		/// </summary>
		private void GenerateMesh()
		{
			// Don't do anything if font isn't ready.
			UIFont uifont = LoadingScreen.instance.uifont;
			if (uifont == null)
			{
				return;
			}

			// Local references.
			UIFontRenderer fontRenderer = uifont.ObtainRenderer();
			UIRenderData renderData = UIRenderData.Obtain();

			try
			{
				// Render font.
				fontRenderer.defaultColor = Color.white;
				fontRenderer.textScale = 1f;
				fontRenderer.pixelRatio = 1f;
				fontRenderer.processMarkup = true;
				fontRenderer.multiLine = true;
				fontRenderer.maxSize = new Vector2(LoadingScreen.instance.meshWidth, LoadingScreen.instance.meshHeight);
				fontRenderer.shadow = false;
				fontRenderer.Render(text, renderData);

				// Generate mesh.
				mesh.Clear();
				mesh.vertices = renderData.vertices.ToArray();
				mesh.colors32 = renderData.colors.ToArray();
				mesh.uv = renderData.uvs.ToArray();
				mesh.triangles = renderData.triangles.ToArray();
			}
			catch (Exception e)
			{
				Logging.LogException(e, "exception generating font mesh");
			}
			finally
			{
				// Clean up after ourselves.
				fontRenderer.Dispose();
				renderData.Dispose();
			}
		}
	}
}
