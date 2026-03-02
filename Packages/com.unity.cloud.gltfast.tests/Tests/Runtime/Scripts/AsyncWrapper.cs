// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityEngine;

namespace GLTFast.Tests
{
    static class AsyncWrapper
    {
        /// <summary>
        /// Wraps a <see cref="Task"/> in an <see cref="IEnumerator"/>.
        /// </summary>
        /// <param name="task">The async Task to wait for</param>
        /// <param name="timeout">Optional timeout in seconds</param>
        /// <returns>IEnumerator</returns>
        /// <exception cref="AggregateException"></exception>
        /// <exception cref="TimeoutException">Thrown when a timeout was set and the task took too long</exception>
        public static IEnumerator WaitForTask(Task task, float timeout = -1)
        {
            var startTime = Time.realtimeSinceStartup;

            while (!task.IsCompleted)
            {
                CheckExceptionAndTimeout();
                yield return null;
            }

            CheckExceptionAndTimeout();
            yield break;

            void CheckExceptionAndTimeout()
            {
                Exception exception = task.Exception;
                if (exception != null)
                {
                    if (exception is AggregateException aggregateException)
                    {
                        ExceptionDispatchInfo.Capture(aggregateException.GetBaseException()).Throw();
                    }
                    throw exception;
                }
                if (timeout > 0 && Time.realtimeSinceStartup - startTime > timeout)
                {
                    throw new TimeoutException();
                }
            }
        }

        /// <summary>
        /// Wraps a <see cref="ValueTask{TResult}"/> in an <see cref="IEnumerator"/>.
        /// </summary>
        /// <param name="task">The async ValueTask to wait for</param>
        /// <param name="timeout">Optional timeout in seconds</param>
        /// <returns>IEnumerator</returns>
        /// <exception cref="TimeoutException">Thrown when a timeout was set and the task took too long</exception>
        public static IEnumerator WaitForTask<T>(ValueTask<T> task, float timeout = -1)
        {
            var startTime = Time.realtimeSinceStartup;

            while (!task.IsCompleted)
            {
                CheckTimeout();
                yield return null;
            }

            CheckTimeout();
            yield break;

            void CheckTimeout()
            {
                if (timeout > 0 && Time.realtimeSinceStartup - startTime > timeout)
                {
                    throw new TimeoutException();
                }
            }
        }
    }
}
