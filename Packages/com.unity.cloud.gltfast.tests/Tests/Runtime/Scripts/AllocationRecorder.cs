// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using NUnit.Framework;
using Unity.Profiling;

namespace Unity.Cloud.Gltfast.Tests
{
    /// <summary>
    /// Sums managed allocation over the frames an operation spans, from one caller sample per frame.
    /// </summary>
    /// <remarks>
    /// The only Mono-compatible source of per-frame allocated bytes: `GC.TryStartNoGCRegion` throws
    /// `NotImplementedException` and `GC.GetAllocatedBytesForCurrentThread` returns 0.
    /// </remarks>
    class AllocationRecorder : IDisposable
    {
        internal const string CounterName = "GC Allocated In Frame";

        ProfilerRecorder m_Recorder;

        public static AllocationRecorder Start()
        {
            return new AllocationRecorder
            {
                m_Recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, CounterName)
            };
        }

        /// <summary>
        /// Skips the calling test where the platform carries no profiler counters.
        /// </summary>
        public void RequireValid()
        {
            if (!m_Recorder.Valid)
            {
                Assert.Ignore($"Profiler counter \"{CounterName}\" is unavailable.");
            }
        }

        public long TotalBytes { get; private set; }

        /// <summary>
        /// Adds the previous frame's managed allocation to <see cref="TotalBytes"/>.
        /// </summary>
        /// <remarks>
        /// Counts only the frames it is called on, so a caller that skips a frame under-reports and one
        /// that calls twice within a frame double-counts.
        /// </remarks>
        public void SampleFrame()
        {
            TotalBytes += m_Recorder.LastValue;
        }

        public void Dispose()
        {
            if (m_Recorder.Valid)
            {
                m_Recorder.Dispose();
            }
        }
    }
}
