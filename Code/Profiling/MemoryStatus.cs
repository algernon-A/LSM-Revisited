// <copyright file="MemoryStatus.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Text;
    using AlgernonCommons.Translation;

    /// <summary>
    /// Memory use profiling.
    /// </summary>
    internal class MemoryStatus
    {
        // Text stringbuilder.
        private readonly StringBuilder _memoryText = new StringBuilder(256);

        // Title string length.
        private readonly int _titleLength;

        // Title strings.
        private readonly string _gameRAMTitle = Translations.Translate("GAME_RAM_USE");
        private readonly string _gamePageTitle = Translations.Translate("GAME_EXTRA_USE");
        private readonly string _sysRAMTitle = Translations.Translate("SYS_RAM_USE");
        private readonly string _sysPageTitle = Translations.Translate("SYS_EXTRA_USE");

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStatus"/> class.
        /// </summary>
        internal MemoryStatus()
        {
            // Set text title.
            _memoryText = new StringBuilder(128);
            _memoryText.Append("<color=white>");
            _memoryText.Append(Translations.Translate("MEM_USE"));
            _memoryText.AppendLine("</color>");
            _memoryText.AppendLine();
            _titleLength = _memoryText.Length;
        }

        /// <summary>
        /// Gets the current display text.
        /// </summary>
        internal StringBuilder Text
        {
            get
            {
                // Reset text.
                _memoryText.Length = _titleLength;

                // Get memory usage.
                MemoryAPI.GetMemoryUse(out double gameUsedPhyiscal, out double sysUsedPhysical, out double totalPhysical, out double gameExtraPage, out double sysExtraPage, out double totalPage);

                // Calculate ratios.
                double memUseRatio = sysUsedPhysical / totalPhysical;
                double pageUseRatio = sysExtraPage / totalPage;

                // Add usage strings.
                SetMemoryText(_gameRAMTitle, memUseRatio, gameUsedPhyiscal);
                SetMemoryText(_gamePageTitle, pageUseRatio, gameExtraPage);
                SetMemoryText(_sysRAMTitle, memUseRatio, sysUsedPhysical, totalPhysical);
                SetMemoryText(_sysPageTitle, pageUseRatio, sysExtraPage, -1d);

                return _memoryText;
            }
        }

        /// <summary>
        /// Sets the memory text for usage-only figures (no totals).
        /// </summary>
        /// <param name="title">Line title.</param>
        /// <param name="ratio">Memory usage ratio.</param>
        /// <param name="usage">Memory usage (in GB).</param>
        private void SetMemoryText(string title, double ratio, double usage)
        {
            _memoryText.Append(title);
            _memoryText.Append(": ");
            _memoryText.Append(GetMemoryColor(ratio));
            _memoryText.Append(usage.ToString("N2"));
            _memoryText.AppendLine("GB</color>");
        }

        /// <summary>
        /// Sets the memory text for usage-only figures including a total available figure.
        /// </summary>
        /// <param name="title">Line title.</param>
        /// <param name="ratio">Memory usage ratio.</param>
        /// <param name="usage">Memory usage (in GB).</param>
        /// <param name="total">Total memory available (in GB); -1 to disable.</param>
        private void SetMemoryText(string title, double ratio, double usage, double total)
        {
            _memoryText.Append(title);
            _memoryText.Append(": ");
            _memoryText.Append(GetMemoryColor(ratio));
            _memoryText.Append(usage.ToString("N2"));
            _memoryText.Append("GB ");

            if (total > 0)
            {
                _memoryText.Append(" / ");
                _memoryText.Append(total.ToString("N2"));
                _memoryText.Append("GB");
            }
            _memoryText.AppendLine("</color>");
        }

        /// <summary>
        /// Returns the text color for memory stat display based on the provided memory usage ratio.
        /// </summary>
        /// <param name="memUseRatio">Memory use ratio (ratio of used to total memory).</param>
        /// <returns>Text display color string.</returns>
        private string GetMemoryColor(double memUseRatio)
        {
            if (memUseRatio >= 0.97d)
            {
                return "<color=red>";
            }
            else if (memUseRatio >= 0.92d)
            {
                return "<color=orange>";
            }
            else if (memUseRatio >= 0.85d)
            {
                return "<color=yellow>";
            }
            else
            {
                return "<color=lime>";
            }
        }
    }
}
