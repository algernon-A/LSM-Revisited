// <copyright file="MemorySource.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) thale5 and algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using AlgernonCommons;
    using LoadingScreenMod;
    using UnityEngine;

    /// <summary>
    /// Memory status text source.
    /// </summary>
    internal sealed class MemorySource : Source
    {
        // Color thresholds for memory use
        private readonly int _ramOrange;
        private readonly int _ramRed;
        private readonly int _pageOrange;
        private readonly int _pageRed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemorySource"/> class.
        /// </summary>
        internal MemorySource()
        {
            // Set color thresholds.
            int systemMegs = SystemInfo.systemMemorySize;
            _ramOrange = (92 * systemMegs) >> 7;
            _ramRed = (106 * systemMegs) >> 7;
            _pageOrange = (107 * systemMegs) >> 7;
            _pageRed = (124 * systemMegs) >> 7;
        }

        /// <summary>
        /// Create text for display.
        /// </summary>
        /// <returns>Text.</returns>
        protected internal override string CreateText()
        {
            try
            {
                // Get memory usage.
                MemoryAPI.GetUsage(out int pageMegas, out int ramMegas);

                // Generate display text.
                string text = ((float)ramMegas / 1024f).ToString("F1") + " GB RAM\n" + ((float)pageMegas / 1024f).ToString("F1") + " GB page";

                // Set text color status based on thresholds.
                if (ramMegas > _ramRed | pageMegas > _pageRed)
                {
                    return "<color #ff5050>" + text + "</color>";
                }

                if (ramMegas > _ramOrange | pageMegas > _pageOrange)
                {
                    return "<color #f0a840>" + text + "</color>";
                }

                // No color; just return standard text.
                return text;
            }
            catch (Exception e)
            {
                Logging.LogException(e, "exception profiling memory use");
                return string.Empty;
            }
        }
    }
}
