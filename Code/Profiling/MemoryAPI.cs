// <copyright file="MemoryAPI.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Memory use profiling.
    /// </summary>
    internal static class MemoryAPI
    {
        // Convert bytes to GB.
        private const double ByteToGB = 1d / (1024d * 1024d * 1024d);

        // Memory access.
        private static readonly IntPtr ProcessHandle;
        private static PROCESS_MEMORY_COUNTERS s_processCounters;
        private static MEMORYSTATUSEX s_statusEX;

        // Maximum counts.
        private static double s_maxGameRAM = 0d;
        private static double s_maxGamePage = 0d;
        private static double s_maxSysRAM = 0d;
        private static double s_maxSysPage = 0d;

        /// <summary>
        /// Initializes static members of the <see cref="MemoryAPI"/> class.
        /// </summary>
        static MemoryAPI()
        {
            ProcessHandle = GetCurrentProcess();
            s_processCounters = default;
            s_processCounters.cb = (uint)Marshal.SizeOf(s_processCounters);
            s_statusEX = new MEMORYSTATUSEX(0);
        }

        /// <summary>
        /// Gets current memory use.
        /// </summary>
        /// <param name="gameUsedRAM">Physical RAM currently used by game, in GB.</param>
        /// <param name="sysUsedRAM">Total system RAM currently in use, in GB.</param>
        /// <param name="totalRAM">Total system RAM, in GB.</param>
        /// <param name="gameUsedPage">Virtual memory currently used by game, in GB.</param>
        /// <param name="sysUsedPage">>Total system virtual memory currently in use, in GB.</param>
        /// <param name="totalPage">Total system virtual memory size, in GB.</param>
        internal static void GetMemoryUse(out double gameUsedRAM, out double sysUsedRAM, out double totalRAM, out double gameUsedPage, out double sysUsedPage, out double totalPage)
        {
            // Get process memory usage.
            GetProcessMemoryInfo(ProcessHandle, out s_processCounters, s_processCounters.cb);

            // Get system RAM use.
            GlobalMemoryStatusEx(ref s_statusEX);

            gameUsedRAM = s_processCounters.WorkingSetSize * ByteToGB;
            sysUsedRAM = (s_statusEX.TotalPhysical - s_statusEX.AvailablePhysical) * ByteToGB;
            totalRAM = s_statusEX.TotalPhysical * ByteToGB;

            gameUsedPage = s_processCounters.PagefileUsage * ByteToGB;
            sysUsedPage = (s_statusEX.TotalPageFile - s_statusEX.AvailablePageFile) * ByteToGB;
            totalPage = s_statusEX.TotalPageFile * ByteToGB;

            // Update maximums.
            if (gameUsedRAM > s_maxGameRAM)
            {
                s_maxGameRAM = gameUsedRAM;
            }

            if (gameUsedPage > s_maxGamePage)
            {
                s_maxGamePage = gameUsedPage;
            }

            if (sysUsedRAM > s_maxSysRAM)
            {
                s_maxSysRAM = sysUsedRAM;
            }

            if (sysUsedPage > s_maxSysPage)
            {
                s_maxSysPage = sysUsedPage;
            }
        }

        /// <summary>
        /// Gets peak memory use.
        /// </summary>
        /// <param name="maxGameRAM">Peak game physical RAM usage.</param>
        /// <param name="maxGamePage">Peak game virtual memory usage.</param>
        /// <param name="maxSysRAM">Peak system physical RAM usage.</param>
        /// <param name="maxSysPage">Peak system virtual memory usage.</param>
        internal static void GetPeakMemoryUse(out double maxGameRAM, out double maxGamePage, out double maxSysRAM, out double maxSysPage)
        {
            maxGameRAM = s_maxGameRAM;
            maxGamePage = s_maxGamePage;
            maxSysRAM = s_maxSysRAM;
            maxSysPage = s_maxSysPage;
        }

        /// <summary>
        /// Retrieves information about the system's current usage of both physical and virtual memory.
        /// </summary>
        /// <param name="lpBuffer">MEMORYSTATUSEX struct.</param>
        /// <returns>Populated MEMORYSTATUSEX struct.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        /// <summary>
        /// Retrieves a pseudo handle for the current process.
        /// </summary>
        /// <returns>Pseudo handle to the current process.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// Retrieves information about the memory usage of the specified process.
        /// </summary>
        /// <param name="hProcess">Process handle.</param>
        /// <param name="ppsmemCounters">Pointer to the counters structure to receive information about the memory usage of the process.</param>
        /// <param name="size">The size of the ppsmemCounters structure, in bytes.</param>
        /// <returns>True if the function succeeds, false otherwise.</returns>
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS ppsmemCounters, uint size);

        /// <summary>
        /// Contains the memory statistics for a process.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Size = 72)]
        private struct PROCESS_MEMORY_COUNTERS
        {
            /// <summary>
            /// Structure size, in bytes - MUST be assigned before use.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Official terminology")]
            internal uint cb;

            /// <summary>
            /// Number of page faults.
            /// </summary>
            internal uint PageFaultCount;

            /// <summary>
            /// Peak working set size, in bytes.
            /// </summary>
            internal ulong PeakWorkingSetSize;

            /// <summary>
            /// Current working set size, in bytes.
            /// </summary>
            internal ulong WorkingSetSize;

            /// <summary>
            /// Peak paged pool usage, in bytes.
            /// </summary>
            internal ulong QuotaPeakPagedPoolUsage;

            /// <summary>
            /// Current paged pool usage, in bytes.
            /// </summary>\
            internal ulong QuotaPagedPoolUsage;

            /// <summary>
            /// Peak nonpaged pool usage, in bytes.
            /// </summary>\
            internal ulong QuotaPeakNonPagedPoolUsage;

            /// <summary>
            /// Current nonpaged pool usage, in bytes.
            /// </summary>
            internal ulong QuotaNonPagedPoolUsage;

            /// <summary>
            /// Commit Charge value in bytes for this process.
            /// </summary>
            internal ulong PagefileUsage;

            /// <summary>
            /// Peak value in bytes of the Commit Charge during the lifetime of this process.
            /// </summary>
            internal ulong PeakPagefileUsage;
        }

        /// <summary>
        /// Contains information about the current state of both physical and virtual memory, including extended memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct MEMORYSTATUSEX
        {
            /// <summary>
            /// Structure size, in bytes - MUST be assigned before use.
            /// </summary>
            internal readonly uint Length;

            /// <summary>
            /// Current memory use.
            /// </summary>
            internal readonly uint MemoryLoad;

            /// <summary>
            /// Total physical memory, in bytes.
            /// </summary>
            internal readonly ulong TotalPhysical;

            /// <summary>
            /// Available physical memory, in bytes.
            /// </summary>
            internal readonly ulong AvailablePhysical;

            /// <summary>
            /// Total page file size, in bytes.
            /// </summary>
            internal readonly ulong TotalPageFile;

            /// <summary>
            /// Avaiable page file size, in bytes.
            /// </summary>
            internal readonly ulong AvailablePageFile;

            /// <summary>
            /// Total virtual memory size, in bytes.
            /// </summary>
            internal readonly ulong TotalVirtual;

            /// <summary>
            /// Available virtual memory size, in bytes.
            /// </summary>
            internal readonly ulong AvailableVirtual;

            /// <summary>
            /// Extended field - keep at zero.
            /// </summary>
            internal readonly ulong AvailablelExtendedVirtual;

            /// <summary>
            /// Initializes a new instance of the <see cref="MEMORYSTATUSEX"/> struct.
            /// </summary>
            /// <param name="_">Ignored (dummary param to force struct constructor).</param>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Ignored parameter")]
            internal MEMORYSTATUSEX(int _)
            {
                Length = 0;
                MemoryLoad = 0;
                TotalPhysical = 0;
                AvailablePhysical = 0;
                TotalPageFile = 0;
                AvailablePageFile = 0;
                TotalVirtual = 0;
                AvailableVirtual = 0;
                AvailablelExtendedVirtual = 0;

                // Assign length once all fields are defined.
                Length = (uint)Marshal.SizeOf(this);
            }
        }
    }
}
