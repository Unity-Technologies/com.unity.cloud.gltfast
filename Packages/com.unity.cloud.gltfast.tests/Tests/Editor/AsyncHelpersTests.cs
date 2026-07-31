// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Utils;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    static class AsyncHelpersTests
    {
        [Test]
        public static void RunSync()
        {
            AsyncHelpers.RunSync(SuccessAsync);
        }

        [Test]
        public static void RunSyncException()
        {
            Assert.Throws<AggregateException>(() => AsyncHelpers.RunSync(FailAsync));
            Assert.AreEqual("UnitySynchronizationContext", SynchronizationContext.Current.GetType().Name);
        }

        [Test]
        public static void RunSyncReturns()
        {
            AsyncHelpers.RunSync(SuccessReturnsAsync);
        }

        [Test]
        public static void RunSyncReturnsException()
        {
            Assert.Throws<AggregateException>(() => AsyncHelpers.RunSync(FailReturnsAsync));
            Assert.AreEqual("UnitySynchronizationContext", SynchronizationContext.Current.GetType().Name);
        }

        static async Task SuccessAsync()
        {
            await Task.Delay(1000);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        static async Task FailAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new InvalidOperationException();
        }

        static async Task<int> SuccessReturnsAsync()
        {
            await Task.Delay(1000);
            return 42;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        static async Task<int> FailReturnsAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new InvalidOperationException();
        }
    }
}
