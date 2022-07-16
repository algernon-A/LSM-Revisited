using System.Diagnostics;
using UnityEngine;

namespace LoadingScreenMod
{
	public static class Profiling
	{
		private static readonly Stopwatch stopWatch = new Stopwatch();

		internal static string FAILED;

		internal static string DUPLICATE;

		internal static string MISSING;

		internal static int Millis => (int)stopWatch.ElapsedMilliseconds;

		internal static void Init()
		{
			Sink.builder.Length = 0;
			FAILED = " (" + LoadingScreenModRevisited.Translations.Translate("FAILED") + ')';
			DUPLICATE = " (" + LoadingScreenModRevisited.Translations.Translate("DUPLICATE") + ')';
			MISSING = " (" + LoadingScreenModRevisited.Translations.Translate("MISSING") + ')';
			if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
			{
				MemoryAPI.pfMax = (MemoryAPI.wsMax = 0);
			}
		}

		internal static void Start()
		{
			stopWatch.Reset();
			stopWatch.Start();
		}

		internal static void Stop()
		{
			Sink.builder.Length = 0;
			Sink.builder.Capacity = 0;
			stopWatch.Reset();
		}

		internal static string TimeString(int millis)
		{
			int num = millis / 1000;
			return num / 60 + ":" + (num % 60).ToString("00");
		}
	}
}
