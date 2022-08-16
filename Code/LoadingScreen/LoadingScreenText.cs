// <copyright file="LoadingScreenText.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Internal performant fields")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1304:Non-private readonly fields should begin with upper-case letter", Justification = "Internal performant fields")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Internal performant fields")]
    internal sealed class LoadingScreenText
    {
        /// <summary>
        /// Text display position.
        /// </summary>
        internal readonly Vector3 m_position;

        /// <summary>
        /// Text source.
        /// </summary>
        internal readonly Source m_source;

        /// <summary>
        /// Text display scale.
        /// </summary>
        internal readonly Vector3 m_scale;

        /// <summary>
        /// Text mesh.
        /// </summary>
        internal Mesh m_mesh = new Mesh();

        // Current text.
        private string _text = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingScreenText"/> class.
        /// </summary>
        /// <param name="position">Text position.</param>
        /// <param name="source">Text source.</param>
        /// <param name="scaleFactor">Text scale factor.</param>
        internal LoadingScreenText(Vector3 position, Source source, float scaleFactor = 1f)
        {
            m_position = position;
            m_source = source;

            // Set text scale.
            float scale = 0.002083333f * scaleFactor;
            m_scale = new Vector3(scale, scale, scale);
        }

        /// <summary>
        /// Clears the current text.
        /// </summary>
        internal void Clear()
        {
            _text = string.Empty;
        }

        /// <summary>
        /// Disposes of the text mesh.
        /// </summary>
        internal void Dispose()
        {
            if (m_mesh != null)
            {
                UnityEngine.Object.Destroy(m_mesh);
            }

            m_mesh = null;
        }

        /// <summary>
        /// Updates the current text.
        /// </summary>
        internal void UpdateText()
        {
            // Get updated text from source.
            string text = m_source.CreateText();

            // Generate new mesh if text has changed.
            if (text != null && text != _text)
            {
                _text = text;
                GenerateMesh();
            }
        }

        /// <summary>
        /// Generates a text mesh for display.
        /// </summary>
        private void GenerateMesh()
        {
            // Don't do anything if font isn't ready.
            UIFont uifont = LoadingScreen.s_instance.m_uifont;
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
                fontRenderer.maxSize = new Vector2(LoadingScreen.s_instance.m_meshWidth, LoadingScreen.s_instance.m_meshHeight);
                fontRenderer.shadow = false;
                fontRenderer.Render(_text, renderData);

                // Generate mesh.
                m_mesh.Clear();
                m_mesh.vertices = renderData.vertices.ToArray();
                m_mesh.colors32 = renderData.colors.ToArray();
                m_mesh.uv = renderData.uvs.ToArray();
                m_mesh.triangles = renderData.triangles.ToArray();
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
