using System;
using UnityEngine;
using LoadingScreenMod;


namespace LoadingScreenModRevisited
{
	/// <summary>
	/// Memory status text source.
	/// </summary>
	internal sealed class MemorySource : Source
	{
		// Color thresholds for memory use
		private readonly int ramOrange, ramRed, pageOrange, pageRed;

		// Current color status.
		private bool orange, red;


		/// <summary>
		/// Constructor.
		/// </summary>
		internal MemorySource()
		{
			// Set color thresholds.
			int systemMegs = SystemInfo.systemMemorySize;
			ramOrange = 92 * systemMegs >> 7;
			ramRed = 106 * systemMegs >> 7;
			pageOrange = 107 * systemMegs >> 7;
			pageRed = 124 * systemMegs >> 7;
		}


		/// <summary>
		/// Create text for display.
		/// </summary>
		/// <returns>Text</returns>
		protected internal override string CreateText()
		{
			try
			{
				// Get memory usage.
				MemoryAPI.GetUsage(out int pageMegas, out int ramMegas);

				// Generate display text.
				string text = ((float)ramMegas / 1024f).ToString("F1") + " GB RAM\n" + ((float)pageMegas / 1024f).ToString("F1") + " GB page";

				// Set text color status based on thresholds.
				orange |= (ramMegas > ramOrange) | (pageMegas > pageOrange);
				red |= (ramMegas > ramRed) | (pageMegas > pageRed);
				if (red)
				{
					return "<color #ff5050>" + text + "</color>";
				}
				if (orange)
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
