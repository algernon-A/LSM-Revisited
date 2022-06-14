using System;
using System.Runtime.InteropServices;

namespace LoadingScreenMod
{
	public static class MemoryAPI
	{
		[StructLayout(LayoutKind.Sequential, Size = 72)]
		private struct PROCESS_MEMORY_COUNTERS
		{
			public uint cb;

			public uint PageFaultCount;

			public ulong PeakWorkingSetSize;

			public ulong WorkingSetSize;

			public ulong QuotaPeakPagedPoolUsage;

			public ulong QuotaPagedPoolUsage;

			public ulong QuotaPeakNonPagedPoolUsage;

			public ulong QuotaNonPagedPoolUsage;

			public ulong PagefileUsage;

			public ulong PeakPagefileUsage;
		}

		private static IntPtr handle;

		internal static int pfMax;

		internal static int wsMax;

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("psapi.dll", SetLastError = true)]
		private static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

		public static void GetUsage(out int pfMegas, out int wsMegas)
		{
			if (handle == IntPtr.Zero)
			{
				handle = GetCurrentProcess();
			}
			PROCESS_MEMORY_COUNTERS counters = default(PROCESS_MEMORY_COUNTERS);
			counters.cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS));
			GetProcessMemoryInfo(handle, out counters, counters.cb);
			pfMegas = (int)(counters.PagefileUsage >> 20);
			wsMegas = (int)(counters.WorkingSetSize >> 20);
			pfMax = Math.Max(pfMax, pfMegas);
			wsMax = Math.Max(wsMax, wsMegas);
		}
	}
}
