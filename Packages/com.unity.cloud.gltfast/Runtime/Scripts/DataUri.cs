// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if !UNITY_WEBGL || UNITY_EDITOR
#define GLTFAST_THREADS
#endif

using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;

namespace GLTFast
{
    static class DataUri
    {
        /// <summary>
        /// Base 64 string to byte array decode speed in bytes per second
        /// Measurements based on a MacBook Pro Intel(R) Core(TM) i9-9980HK CPU @ 2.40GHz
        /// and reduced by ~ 20%
        /// </summary>
        const int k_Base64DecodeSpeed =
#if UNITY_EDITOR
            60_000_000;
#else
            150_000_000;
#endif

        public static bool IsDataUri(ReadOnlySpan<char> dataUri)
        {
            return dataUri.StartsWith("data:", StringComparison.Ordinal);
        }

        public static async ValueTask<IReadOnlyDisposableData> DecodeDataUriAsync(
            string dataUri,
            IDeferAgent deferAgent,
            CancellationToken cancellationToken
        )
        {
            if (!TryGetDataUriDescriptor(
                    dataUri, out _, out var startIndex, out var byteLength))
            {
                return null;
            }

            var data = await DecodeDataUriAsync(
                dataUri, startIndex, byteLength, deferAgent, cancellationToken);
            if (!data.IsCreated)
            {
                return null;
            }

            return new ReadOnlyDisposableData(data);
        }

        public static async ValueTask<NativeArray<byte>> DecodeDataUriAsync(
            string dataUri,
            int startIndex,
            int byteLength,
            IDeferAgent deferAgent,
            CancellationToken cancellationToken,
            bool timeCritical = false
            )
        {
            var predictedTime = dataUri.Length / (float)k_Base64DecodeSpeed;
#if MEASURE_TIMINGS
            var stopWatch = new Stopwatch();
            stopWatch.Start();
#elif GLTFAST_THREADS
            if (!timeCritical || deferAgent.ShouldDefer(predictedTime))
            {
                try
                {
                    return await Task.Run(() => DecodeDataUri(dataUri, startIndex, byteLength), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                }
            }
#endif
            await deferAgent.BreakPoint(predictedTime);
            var result = DecodeDataUri(dataUri, startIndex, byteLength);
#if MEASURE_TIMINGS
            stopWatch.Stop();
            var elapsedSeconds = stopWatch.ElapsedMilliseconds / 1000f;
            var relativeDiff = (elapsedSeconds-predictedTime) / predictedTime;
            if (Mathf.Abs(relativeDiff) > .2f) {
                Debug.LogWarning($"Base 64 unexpected duration! diff: {relativeDiff:0.00}% predicted: {predictedTime} sec actual: {elapsedSeconds} sec");
            }
            var throughput = dataUri.Length / elapsedSeconds;
            Debug.Log($"Base 64 throughput: {throughput} bytes/sec ({dataUri.Length} bytes in {elapsedSeconds} seconds)");
#endif
            return result;
        }

#if !UNITY_6000_0_OR_NEWER
        public static async ValueTask<byte[]> DecodeDataUriToManagedArrayAsync(
            string dataUri,
            int startIndex,
            int byteLength,
            IDeferAgent deferAgent,
            CancellationToken cancellationToken,
            bool timeCritical = false
            )
        {
            var predictedTime = dataUri.Length / (float)k_Base64DecodeSpeed;
#if GLTFAST_THREADS
            if (!timeCritical || deferAgent.ShouldDefer(predictedTime))
            {
                try
                {
                    return await Task.Run(() => DecodeDataUriToManagedArray(dataUri, startIndex, byteLength), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                }
            }
#endif
            await deferAgent.BreakPoint(predictedTime);
            var result = DecodeDataUriToManagedArray(dataUri, startIndex, byteLength);
            return result;
        }
#endif // !UNITY_6000_0_OR_NEWER

        public static bool TryGetDataUriDescriptor(
            string dataUri,
            out ReadOnlySpan<char> mimeType,
            out int startIndex,
            out int byteLength
            )
        {
            var mediaTypeEnd = dataUri.IndexOf(';', 5, Math.Min(dataUri.Length - 5, 1000));
            if (mediaTypeEnd < 0)
            {
                mimeType = null;
                startIndex = 0;
                byteLength = -1;
                return false;
            }
            mimeType = dataUri.AsSpan(5, mediaTypeEnd - 5);
            if (!dataUri.AsSpan(mediaTypeEnd + 1, 7).SequenceEqual("base64,"))
            {
                startIndex = 0;
                byteLength = -1;
                return false;
            }
            var padding = 0;
            if (dataUri.Length > 0 && dataUri[^1] == '=')
            {
                padding = dataUri.Length > 1 && dataUri[^2] == '=' ? 2 : 1;
            }

            startIndex = mediaTypeEnd + 8;
            byteLength = ((dataUri.Length - startIndex) * 3 + 3) / 4 - padding;
            return true;
        }

        static NativeArray<byte> DecodeDataUri(string dataUri, int startIndex, int dataLength)
        {
            Profiler.BeginSample("DecodeDataUri");
            var data = new NativeArray<byte>(dataLength, Allocator.Persistent);
            if (!Convert.TryFromBase64Chars(dataUri.AsSpan(startIndex), data.AsSpan(), out var bytesWritten)
                || bytesWritten != dataLength)
            {
                // Invalidate buffer to signal decoding failed.
                data.Dispose();
            }
            Profiler.EndSample();
            return data;
        }

#if !UNITY_6000_0_OR_NEWER
        static byte[] DecodeDataUriToManagedArray(string dataUri, int startIndex, int dataLength)
        {
            Profiler.BeginSample("DecodeDataUriToManagedArray");
            var data = new byte[dataLength];
            if (!Convert.TryFromBase64Chars(dataUri.AsSpan(startIndex), data.AsSpan(), out var bytesWritten)
                || bytesWritten != dataLength)
            {
                // Invalidate buffer to signal decoding failed.
                data = null;
            }
            Profiler.EndSample();
            return data;
        }
#endif // !UNITY_6000_0_OR_NEWER
    }
}
