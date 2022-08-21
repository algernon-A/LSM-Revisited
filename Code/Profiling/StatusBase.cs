// <copyright file="Status.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Base class for generating loading screen status text.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected fields")]
    internal class StatusBase
    {
        /// <summary>
        /// Stringbuilder for text output.
        /// </summary>
        protected StringBuilder m_text;

        /// <summary>
        /// Event array index of the last reported event.
        /// </summary>
        protected int m_lastEventIndex = -1;

        /// <summary>
        /// Profiler event list.
        /// </summary>
        protected FastList<LoadingProfiler.Event> m_events;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusBase"/> class.
        /// </summary>
        /// <param name="profiler">Loading profiler.</param>
        internal StatusBase(LoadingProfiler profiler)
        {
            // Reflect events field.
            FieldInfo eventsField = typeof(LoadingProfiler).GetField("m_events", BindingFlags.Instance | BindingFlags.NonPublic);
            m_events = (FastList<LoadingProfiler.Event>)eventsField.GetValue(profiler);
        }
    }
}
