// <copyright file="MemoryStatus.cs" company="algernon (K. Algernon A. Sheppard)">
// Copyright (c) algernon (K. Algernon A. Sheppard). All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>

namespace LoadingScreenModRevisited
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;
    using AlgernonCommons.Translation;

    /// <summary>
    /// Memory use profiling.
    /// </summary>
    internal class MemoryStatus
    {
        // Convert bytes to GB.
        private const double ByteToGB = 1d / (1024d * 1024d * 1024d);

        // Text stringbuilder.
        private readonly StringBuilder _memoryText = new StringBuilder(256);

        // Title string length.
        private readonly int _titleLength;

        // Title strings.
        private readonly string _gameRAMTitle = Translations.Translate("GAME_RAM_USE");
        private readonly string _gamePageTitle = Translations.Translate("GAME_PAGE_USE");
        private readonly string _sysRAMTitle = Translations.Translate("SYS_RAM_USE");
        private readonly string _sysPageTitle = Translations.Translate("SYS_PAGE_USE");

        // Memory access.
        private readonly IntPtr _processHandle;
        private PROCESS_MEMORY_COUNTERS _processCounters;
        private MEMORYSTATUSEX _statusEX;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStatus"/> class.
        /// </summary>
        internal MemoryStatus()
        {
            // Memory status reporting.
            _processHandle = GetCurrentProcess();
            _processCounters = default;
            _processCounters.cb = (uint)Marshal.SizeOf(_processCounters);
            _statusEX = new MEMORYSTATUSEX(0);

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

                // Get process memory usage.
                GetProcessMemoryInfo(_processHandle, out _processCounters, _processCounters.cb);

                // Get system RAM use.
                GlobalMemoryStatusEx(ref _statusEX);
                ulong memInUse = _statusEX.TotalPhysical - _statusEX.AvailablePhysical;
                double memUseRatio = memInUse / _statusEX.TotalPhysical;

                // Calculate pagefile stats.
                ulong pageInInUse = _statusEX.TotalPageFile - _statusEX.AvailablePageFile;
                double pageUseRatio = pageInInUse / _statusEX.TotalPageFile;

                // Add usage strings.
                SetMemoryText(_gameRAMTitle, memUseRatio, _processCounters.WorkingSetSize);
                SetMemoryText(_gamePageTitle, pageUseRatio, _processCounters.PagefileUsage);
                SetMemoryText(_sysRAMTitle, memUseRatio, memInUse, _statusEX.TotalPhysical);
                SetMemoryText(_sysPageTitle, pageUseRatio, pageInInUse, _statusEX.TotalPageFile);

                return _memoryText;
            }
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
        /// Sets the memory text for usage-only figures (no totals).
        /// </summary>
        /// <param name="title">Line title.</param>
        /// <param name="ratio">Memory usage ratio.</param>
        /// <param name="usage">Memory usage (in bytes).</param>
        private void SetMemoryText(string title, double ratio, ulong usage)
        {
            _memoryText.Append(title);
            _memoryText.Append(": ");
            _memoryText.Append(GetMemoryColor(ratio));
            _memoryText.Append((usage * ByteToGB).ToString("N2"));
            _memoryText.AppendLine("GB</color>");
        }

        /// <summary>
        /// Sets the memory text for usage-only figures including a total available figure.
        /// </summary>
        /// <param name="title">Line title.</param>
        /// <param name="ratio">Memory usage ratio.</param>
        /// <param name="usage">Memory usage (in bytes).</param>
        /// <param name="total">Total memory available (in bytes).</param>
        private void SetMemoryText(string title, double ratio, ulong usage, ulong total)
        {
            _memoryText.Append(title);
            _memoryText.Append(": ");
            _memoryText.Append(GetMemoryColor(ratio));
            _memoryText.Append((usage * ByteToGB).ToString("N2"));
            _memoryText.Append("GB / ");
            _memoryText.Append((total * ByteToGB).ToString("N2"));
            _memoryText.AppendLine("GB</color>");
        }

        /// <summary>
        /// Returns the text color for memory stat display based on the provided memory usage ratio.
        /// </summary>
        /// <param name="memUseRatio">Memory use ratio (ratio of used to total memory).</param>
        /// <returns>Text display color string.</returns>
        private string GetMemoryColor(double memUseRatio)
        {
            if (memUseRatio >= 0.95d)
            {
                return "<color=red>";
            }
            else if (memUseRatio >= 0.90d)
            {
                return "<color=orange>";
            }
            else if (memUseRatio >= 0.80d)
            {
                return "<color=yellow>";
            }
            else
            {
                return "<color=lime>";
            }
        }

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
        private struct MEMORYSTATUSEX
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
