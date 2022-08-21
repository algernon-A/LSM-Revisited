// <copyright file="Timing.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Diagnostics;
    using System.Text;
    using AlgernonCommons.Translation;

    /// <summary>
    /// Timing profiler.
    /// </summary>
    internal class Timing
    {
        // Stopwatch.
        private static readonly Stopwatch StopWatch = new Stopwatch();

        // Text output.
        private static readonly StringBuilder TimeText = new StringBuilder(64);

        // Text title lentgh.
        private static int s_textTitleLength;

        /// <summary>
        /// Gets the elapsed milliseconds.
        /// </summary>
        internal static int ElapsedMilliseconds => (int)StopWatch.ElapsedMilliseconds;

        /// <summary>
        /// Gets a formatted human-readable string representing the current elapsed time.
        /// </summary>
        internal static string CurrentTime
        {
            get
            {
                TimeText.Length = s_textTitleLength;
                TimeText.Append(TimeString(StopWatch.ElapsedMilliseconds));
                return TimeText.ToString();
            }
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        internal static void Start()
        {
            StopWatch.Reset();
            StopWatch.Start();

            // Set text title.
            TimeText.Length = 0;
            TimeText.Append("<color=white>");
            TimeText.Append(Translations.Translate("ELAPSED_TIME"));
            TimeText.AppendLine("</color>");
            TimeText.AppendLine();

            // Record title length.
            s_textTitleLength = TimeText.Length;
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        internal static void Stop() => StopWatch.Stop();

        /// <summary>
        /// Conerts the given duration to a reporting string.
        /// </summary>
        /// <param name="milliseconds">Duration in milliseconds.</param>
        /// <returns>Formatted time string.</returns>
        internal static string TimeString(long milliseconds)
        {
            long seconds = milliseconds / 1000;
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }
    }
}
