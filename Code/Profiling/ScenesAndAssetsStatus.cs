// <copyright file="ScenesAndAssetsStatus.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Text;
    using AlgernonCommons.Translation;
    using ColossalFramework;

    /// <summary>
    /// Report on scene and asset loading progress.
    /// </summary>
    internal sealed class ScenesAndAssetsStatus : StatusBase
    {
        // Status text labels.
        private readonly string _failedText = " (" + Translations.Translate("FAILED") + ')';
        private readonly string _duplicateText = " (" + Translations.Translate("DUPLICATE") + ')';
        private readonly string _missingText = " (" + Translations.Translate("MISSING") + ')';
        private readonly string _failedString = Translations.Translate("FAILED");
        private readonly string _missingString = Translations.Translate("MISSING");

        // Missing/failed asset message.
        private readonly StringBuilder _problemCountText;

        // Line counters.
        private readonly int _titleLength;
        private int _maxLines = 0;
        private int _numLines = 0;

        // Asset counts.
        private int _assetsNotFound = 0;
        private int _assetsFailed = 0;
        private int _assetsDuplicate = 0;
        private bool _problemCountChanged = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScenesAndAssetsStatus"/> class.
        /// </summary>
        internal ScenesAndAssetsStatus()
            : base(Singleton<LoadingManager>.instance.m_loadingProfilerScenes)
        {
            // Initialize stringbuilder with title.
            m_text = new StringBuilder(4096);
            m_text.Append("<color=white>");
            m_text.Append(Translations.Translate("SCENES_AND_ASSETS"));
            m_text.AppendLine("</color>");
            m_text.AppendLine();
            _titleLength = m_text.Length;

            // Intialize problem count stringbuilder.
            _problemCountText = new StringBuilder(128);
            _problemCountText.AppendLine();
        }

        /// <summary>
        /// Sets the maximum number of lines to display (inclusive of title lines).
        /// </summary>
        internal int MaxLines { set => _maxLines = value - 2; }

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
                                EnsureLines();
                                m_text.AppendLine(buffer[i].m_name);
                            }

                            break;
                    }
                }

                return m_text;
            }
        }

        /// <summary>
        /// Gets the current asset problem count string.
        /// </summary>
        internal StringBuilder ProblemString
        {
            get
            {
                // If nothing's changed since last time, just return the current text.
                if (!_problemCountChanged)
                {
                    return _problemCountText;
                }

                // Status has changed; regenerate string.
                _problemCountText.Length = 0;
                if (_assetsFailed != 0)
                {
                    // Failed assets
                    _problemCountText.Append("<color=red>");
                    _problemCountText.Append(_assetsFailed);
                    _problemCountText.Append(' ');
                    _problemCountText.Append(_failedString);
                    _problemCountText.Append("</color>");
                }

                if (_assetsNotFound != 0)
                {
                    // Append comma if we've also got failed assets.
                    if (_assetsFailed != 0)
                    {
                        _problemCountText.Append(", ");
                    }

                    // Missing assets.
                    _problemCountText.Append("<color=orange>");
                    _problemCountText.Append(_assetsNotFound);
                    _problemCountText.Append(' ');
                    _problemCountText.Append(_missingString);
                    _problemCountText.Append("</color> ");
                }

                // Trailing newline.
                _problemCountText.AppendLine();

                // Reset changed flag.
                _problemCountChanged = false;

                return _problemCountText;
            }
        }

        /// <summary>
        /// Adds a text line to the output text.
        /// </summary>
        /// <param name="line">Line to add.</param>
        internal void AddLine(string line)
        {
            EnsureLines();
            m_text.AppendLine(line);
        }

        /// <summary>
        /// Adds an asset loading record.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        internal void AddAsset(string assetName)
        {
            EnsureLines();
            m_text.AppendLine(assetName);
        }

        /// <summary>
        /// Adds a missing asset record.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        internal void AssetNotFound(string assetName)
        {
            ++_assetsNotFound;
            _problemCountChanged = true;
            EnsureLines();
            m_text.Append("<color=orange>");
            m_text.Append(assetName);
            m_text.Append(_missingText);
            m_text.AppendLine("</color>");
        }

        /// <summary>
        /// Adds a failed asset record.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        internal void AssetFailed(string assetName)
        {
            ++_assetsFailed;
            _problemCountChanged = true;
            EnsureLines();
            m_text.Append("<color=red>");
            m_text.Append(assetName);
            m_text.Append(_failedText);
            m_text.AppendLine("</color>");
        }

        /// <summary>
        /// Adds a duplicate asset record.
        /// </summary>
        /// <param name="assetName">Asset name.</param>
        internal void AssetDuplicate(string assetName)
        {
            ++_assetsDuplicate;
            EnsureLines();
            m_text.Append("<color=cyan>");
            m_text.Append(assetName);
            m_text.Append(_duplicateText);
            m_text.AppendLine("</color>");
        }

        /// <summary>
        /// Checks to see if we're already at the maximum number of lines to display, and if so, removes the first (non-title) line.
        /// </summary>
        private void EnsureLines()
        {
            // Check to see if we're already at the max number of lines.
            if (_numLines >= _maxLines)
            {
                // Start at the end of the title and iterate though, looking for the first newline.
                int i = _titleLength;
                do
                {
                    ++i;
                }
                while (m_text[i] != '\n');

                // Newline found - remove all text between the end of the title and the newline (inclusive of the latter).
                m_text.Remove(_titleLength, i - _titleLength + 1);
            }
            else
            {
                // Maximum number not reached - increment line counter.
                ++_numLines;
            }
        }
    }
}