// <copyright file="ThreadStatus.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Text;
    using AlgernonCommons.Translation;

    /// <summary>
    /// Report on thread loading progress.
    /// </summary>
    internal class ThreadStatus : StatusBase
    {
        // Title string length.
        private int _titleLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadStatus"/> class.
        /// </summary>
        /// <param name="profiler">Loading profiler.</param>
        /// <param name="titleKey">Title text translation key.</param>
        internal ThreadStatus(LoadingProfiler profiler, string titleKey)
            : base(profiler)
        {
            m_text = new StringBuilder(128);
            m_text.Append("<color=white>");
            m_text.Append(Translations.Translate(titleKey));
            m_text.AppendLine("</color>");
            _titleLength = m_text.Length;
        }

        /// <summary>
        /// Gets the current display text.
        /// </summary>
        internal StringBuilder Text
        {
            get
            {
                // Check for any new scene events.
                LoadingProfiler.Event[] buffer = m_events.m_buffer;
                for (int i = 0; i < m_events.m_size; ++i)
                {
                    switch (buffer[i].m_type)
                    {
                        // Only interested in loading events.
                        case LoadingProfiler.Type.BeginLoading:
                        case LoadingProfiler.Type.BeginSerialize:
                        case LoadingProfiler.Type.BeginDeserialize:
                        case LoadingProfiler.Type.BeginAfterDeserialize:
                            // We only want the latest event; skip events that we've already encountered.
                            if (i > m_lastEventIndex)
                            {
                                m_lastEventIndex = i;

                                // Preserve title and append new text immediately after, replacing any previous text.
                                m_text.Length = _titleLength;
                                m_text.AppendLine(buffer[i].m_name);
                            }

                            break;
                    }
                }

                return m_text;
            }
        }
    }
}
