using System;
using UnityEngine;

namespace LoadingScreenMod
{
	internal sealed class MemorySource : Source
	{
		private int systemMegas = SystemInfo.systemMemorySize;

		private int wsOrange;

		private int wsRed;

		private int pfOrange;

		private int pfRed;

		private bool orange;

		private bool red;

		internal MemorySource()
		{
			wsOrange = 92 * systemMegas >> 7;
			wsRed = 106 * systemMegas >> 7;
			pfOrange = 107 * systemMegas >> 7;
			pfRed = 124 * systemMegas >> 7;
		}

		protected internal override string CreateText()
		{
			try
			{
				MemoryAPI.GetUsage(out var pfMegas, out var wsMegas);
				string text = ((float)wsMegas / 1024f).ToString("F1") + " GB\n" + ((float)pfMegas / 1024f).ToString("F1") + " GB";
				orange |= (wsMegas > wsOrange) | (pfMegas > pfOrange);
				red |= (wsMegas > wsRed) | (pfMegas > pfRed);
				if (red)
				{
					return "<color #f04040>" + text + "</color>";
				}
				if (orange)
				{
					return "<color #f0a840>" + text + "</color>";
				}
				return text;
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}
	}
}
