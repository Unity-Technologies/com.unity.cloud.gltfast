// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GLTFast.Export;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Tests
{
    using Logging;

    static class LoggerTest
    {
        [Test]
        public static void CollectingLoggerTest()
        {
            var r = new CollectingLogger();
            r.Error(LogCode.Download, "404", "https://something.com/nowherfound.glb");

            Assert.AreEqual(1, r.Count);
            var items = r.Items.ToArray();
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 404", items[0].ToString());
        }

        [Test]
        public static void CollectingLoggerLogTest()
        {
            var r = new CollectingLogger();
            ICodeLogger l = r;
            l.Log(LogType.Error, LogCode.Download, "401", "https://something.com/nowherfound.glb");
            l.Log(LogType.Assert, LogCode.Download, "402", "https://something.com/nowherfound.glb");
            l.Log(LogType.Warning, LogCode.Download, "403", "https://something.com/nowherfound.glb");
            l.Log(LogType.Log, LogCode.Download, "404", "https://something.com/nowherfound.glb");
            l.Log(LogType.Exception, LogCode.Download, "405", "https://something.com/nowherfound.glb");

            Assert.AreEqual(5, r.Count);
            var items = r.Items.ToArray();
            Assert.AreEqual(LogType.Error, items[0].Type);
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 401", items[0].ToString());
            Assert.AreEqual(LogType.Assert, items[1].Type);
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 402", items[1].ToString());
            Assert.AreEqual(LogType.Warning, items[2].Type);
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 403", items[2].ToString());
            Assert.AreEqual(LogType.Log, items[3].Type);
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 404", items[3].ToString());
            Assert.AreEqual(LogType.Exception, items[4].Type);
            Assert.AreEqual("Download URL https://something.com/nowherfound.glb failed: 405", items[4].ToString());
        }

        [Test]
        public static void ConsoleLoggerTest()
        {
            LogAssert.Expect(LogType.Error, "Download URL https://something.com/nowherfound.glb failed: 404");
            ConsoleLogger.Instance.Error(
                LogCode.Download, "404", "https://something.com/nowherfound.glb");
        }

        [Test]
        public static void ConsoleLoggerLogTest()
        {
            LogAssert.Expect(LogType.Error, "Download URL https://something.com/nowherfound.glb failed: 401");
            LogAssert.Expect(LogType.Assert, "Download URL https://something.com/nowherfound.glb failed: 402");
            LogAssert.Expect(LogType.Warning, "Download URL https://something.com/nowherfound.glb failed: 403");
            LogAssert.Expect(LogType.Log, "Download URL https://something.com/nowherfound.glb failed: 404");
            LogAssert.Expect(LogType.Exception, "Download URL https://something.com/nowherfound.glb failed: 405");

            ICodeLogger l = ConsoleLogger.Instance;
            l.Log(LogType.Error, LogCode.Download, "401", "https://something.com/nowherfound.glb");
            l.Log(LogType.Assert, LogCode.Download, "402", "https://something.com/nowherfound.glb");
            l.Log(LogType.Warning, LogCode.Download, "403", "https://something.com/nowherfound.glb");
            l.Log(LogType.Log, LogCode.Download, "404", "https://something.com/nowherfound.glb");
            l.Log(LogType.Exception, LogCode.Download, "405", "https://something.com/nowherfound.glb");
        }

        [Test]
        public static void NullLogger_Emits_Nothing_For_All_Methods()
        {
            LogAssert.NoUnexpectedReceived();
            Assert.DoesNotThrow(() =>
            {
                var silent = new NullLogger();

                silent.Error(LogCode.Download, "404", "https://example.com/a.glb");
                silent.Warning(LogCode.Download, "warn");
                silent.Info(LogCode.Download, "info");
                silent.Error("plain error");
                silent.Warning("plain warning");
                silent.Info("plain info");

                ICodeLogger asInterface = silent;
                asInterface.Error(LogCode.Download, "404");
                asInterface.Warning(LogCode.Download, "warn");
                asInterface.Info(LogCode.Download, "info");
                asInterface.Error("plain error");
                asInterface.Warning("plain warning");
                asInterface.Info("plain info");
                asInterface.Log(LogType.Error, LogCode.Download, "401");
                asInterface.Log(LogType.Warning, LogCode.Download, "402");
                asInterface.Log(LogType.Log, LogCode.Download, "403");
                asInterface.Log(LogType.Assert, LogCode.Download, "404");
                asInterface.Log(LogType.Exception, LogCode.Download, "405");
            });
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public static IEnumerator GltfWriter_SaveToStream_NonBinary_LogsErrorViaConsole_WhenNoLoggerPassed()
        {
            LogAssert.Expect(
                LogType.Error,
                new Regex(".*Save to Stream currently only works for self-contained glTF-Binary.*"));
            var writer = new GltfWriter(new ExportSettings { Format = GltfFormat.Json });
            var stream = new MemoryStream();
            var task = writer.SaveToStreamAndDispose(stream);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.IsFalse(task.Result, "SaveToStreamAndDispose should fail for non-binary format.");
            stream.Dispose();
        }

        [UnityTest]
        public static IEnumerator GltfWriter_AnyPublicApi_AfterDispose_ThrowsInvalidOperationException()
        {
            var writer = new GltfWriter(new ExportSettings { Format = GltfFormat.Binary }, logger: new NullLogger());
            var nodeId = writer.AddNode(name: "n");
            writer.AddScene(new List<uint> { nodeId });
            var tmpPath = Path.Combine(Application.temporaryCachePath, $"gltfwriter-dispose-{Guid.NewGuid():N}.glb");
            var saveTask = writer.SaveToFileAndDisposeInternal(tmpPath, true);
            yield return AsyncWrapper.WaitForTask(saveTask);
            Assert.IsTrue(saveTask.Result, "Initial SaveToFileAndDisposeInternal should succeed.");

            Assert.Throws<InvalidOperationException>(() => writer.AddNode(name: "after"));

            if (File.Exists(tmpPath))
            {
                try
                {
                    File.Delete(tmpPath);
                }
                catch
                {
                }
            }
        }

        [UnityTest]
        public static IEnumerator GltfAssetBase_Instantiate_NullImporter_NoConsoleOutput_NoCrash()
        {
            var go = new GameObject("GltfAssetBase-Instantiate-NullImporter");
            GltfAsset asset;
            try
            {
                asset = go.AddComponent<GltfAsset>();
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(go);
                throw;
            }

            var instantiateTask = asset.Instantiate(logger: null);
            yield return AsyncWrapper.WaitForTask(instantiateTask);
            Assert.IsFalse(instantiateTask.Result, "Instantiate with no Importer should return false.");

            var instantiateSceneTask = asset.InstantiateScene(0, logger: null);
            yield return AsyncWrapper.WaitForTask(instantiateSceneTask);
            Assert.IsFalse(instantiateSceneTask.Result, "InstantiateScene with no Importer should return false.");

            LogAssert.NoUnexpectedReceived();
            UnityEngine.Object.DestroyImmediate(go);
        }

        [UnityTest]
        public static IEnumerator GltfBoundsAsset_InstantiateScene_NullImporter_NoConsoleOutput_NoCrash()
        {
            var go = new GameObject("GltfBoundsAsset-InstantiateScene-NullImporter");
            GltfBoundsAsset asset;
            try
            {
                asset = go.AddComponent<GltfBoundsAsset>();
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(go);
                throw;
            }

            var instantiateSceneTask = asset.InstantiateScene(0, logger: null);
            yield return AsyncWrapper.WaitForTask(instantiateSceneTask);
            Assert.IsFalse(instantiateSceneTask.Result, "InstantiateScene with no Importer should return false.");

            LogAssert.NoUnexpectedReceived();
            UnityEngine.Object.DestroyImmediate(go);
        }

        // 13-byte glTF-Binary header (magic + version=2 + totalLength=13) plus a
        // 1-byte stub too short for the 8-byte chunk header, which deterministically
        // triggers LogCode.ChunkIncomplete.
        [UnityTest]
        public static IEnumerator NullLogger_DeliberatelyInvalidGltf_NoConsoleOutput_NoException()
        {
            var malformed = new byte[]
            {
                0x67, 0x6C, 0x54, 0x46, // magic
                0x02, 0x00, 0x00, 0x00, // version
                0x0D, 0x00, 0x00, 0x00, // totalLength = 13
                0x00,                   // partial chunk header (8 bytes expected)
            };

            using var import = new GltfImport(logger: new NullLogger());

            var loadTask = import.Load(malformed);
            yield return AsyncWrapper.WaitForTask(loadTask);

            Assert.IsFalse(
                loadTask.Result,
                "Malformed glTF-Binary must report failure via return value, not exception.");
            LogAssert.NoUnexpectedReceived();
        }

        static ICodeLogger ReadStoredLogger(object instance)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            for (var type = instance.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                var field = type.GetField("m_Logger", flags);
                if (field != null)
                    return (ICodeLogger)field.GetValue(instance);
            }
            throw new AssertionException($"{instance.GetType().Name} has no m_Logger field.");
        }

        [Test]
        public static void GltfImport_NullLogger_FallsBackToConsole()
        {
            using var import = new GltfImport(deferAgent: new UninterruptedDeferAgent(), logger: null);
            Assert.IsInstanceOf<ConsoleLogger>(import.Logger);
        }

        [Test]
        public static void GltfImport_NullLoggerInstance_StoredAsNull()
        {
            using var import = new GltfImport(deferAgent: new UninterruptedDeferAgent(), logger: NullLogger.Instance);
            Assert.IsNull(import.Logger);
        }

        [Test]
        public static void GltfWriter_NullLogger_FallsBackToConsole()
        {
            var writer = new GltfWriter(new ExportSettings { Format = GltfFormat.Binary }, logger: null);
            Assert.IsInstanceOf<ConsoleLogger>(ReadStoredLogger(writer));
        }

        [Test]
        public static void GltfWriter_NullLoggerInstance_StoredAsNull()
        {
            var writer = new GltfWriter(new ExportSettings { Format = GltfFormat.Binary }, logger: NullLogger.Instance);
            Assert.IsNull(ReadStoredLogger(writer));
        }

        [Test]
        public static void GameObjectInstantiator_NullLogger_FallsBackToConsole()
        {
            var parent = new GameObject(nameof(GameObjectInstantiator_NullLogger_FallsBackToConsole));
            try
            {
                var instantiator = new GameObjectInstantiator(gltf: null, parent: parent.transform, logger: null);
                Assert.IsInstanceOf<ConsoleLogger>(ReadStoredLogger(instantiator));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public static void GameObjectInstantiator_NullLoggerInstance_StoredAsNull()
        {
            var parent = new GameObject(nameof(GameObjectInstantiator_NullLoggerInstance_StoredAsNull));
            try
            {
                var instantiator = new GameObjectInstantiator(gltf: null, parent: parent.transform, logger: NullLogger.Instance);
                Assert.IsNull(ReadStoredLogger(instantiator));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public static void GameObjectBoundsInstantiator_NullLogger_FallsBackToConsole()
        {
            var parent = new GameObject(nameof(GameObjectBoundsInstantiator_NullLogger_FallsBackToConsole));
            try
            {
                var instantiator = new GameObjectBoundsInstantiator(gltf: null, parent: parent.transform, logger: null);
                Assert.IsInstanceOf<ConsoleLogger>(ReadStoredLogger(instantiator));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public static void GameObjectBoundsInstantiator_NullLoggerInstance_StoredAsNull()
        {
            var parent = new GameObject(nameof(GameObjectBoundsInstantiator_NullLoggerInstance_StoredAsNull));
            try
            {
                var instantiator = new GameObjectBoundsInstantiator(gltf: null, parent: parent.transform, logger: NullLogger.Instance);
                Assert.IsNull(ReadStoredLogger(instantiator));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

#if UNITY_ENTITIES_GRAPHICS
        [Test]
        public static void EntityInstantiator_NullLogger_FallsBackToConsole()
        {
            var instantiator = new EntityInstantiator(gltf: null, parent: default, logger: null);
            Assert.IsInstanceOf<ConsoleLogger>(ReadStoredLogger(instantiator));
        }

        [Test]
        public static void EntityInstantiator_NullLoggerInstance_StoredAsNull()
        {
            var instantiator = new EntityInstantiator(gltf: null, parent: default, logger: NullLogger.Instance);
            Assert.IsNull(ReadStoredLogger(instantiator));
        }
#endif

        [UnityTest]
        public static IEnumerator GltfWriter_AddImage_AfterDispose_ThrowsInvalidOperationException()
        {
            var writer = new GltfWriter(new ExportSettings { Format = GltfFormat.Binary }, logger: new NullLogger());
            var nodeId = writer.AddNode(name: "n");
            writer.AddScene(new List<uint> { nodeId });
            var tmpPath = Path.Combine(Application.temporaryCachePath, $"addimage-dispose-{Guid.NewGuid():N}.glb");
            var saveTask = writer.SaveToFileAndDisposeInternal(tmpPath, true);
            yield return AsyncWrapper.WaitForTask(saveTask);
            Assert.IsTrue(saveTask.Result, "Initial SaveToFileAndDisposeInternal should succeed.");

            Assert.Throws<InvalidOperationException>(() => writer.AddImage(null));

            if (File.Exists(tmpPath))
            {
                try
                {
                    File.Delete(tmpPath);
                }
                catch
                {
                }
            }
        }

#if UNITY_ANIMATION
        [Test]
        public static void GameObjectInstantiator_AnimationComponentFail_LogsErrorViaConsole_WhenNoLoggerPassed()
        {
            var parent = new GameObject("GameObjectInstantiator-AnimationFail");
            try
            {
                parent.AddComponent<Animation>();

                var settings = new InstantiationSettings
                {
                    SceneObjectCreation = SceneObjectCreation.Never
                };

                var instantiator = new GameObjectInstantiator(
                    gltf: null,
                    parent: parent.transform,
                    settings: settings);

                instantiator.BeginScene("scene", new uint[] { 0 });

                var clip = new AnimationClip { legacy = true, name = "clip" };

                // The default ConsoleLogger must surface the fallback error.
                LogAssert.Expect(LogType.Error, "Could not create Animation component.");
                // Adding a second Animation component is blocked by [DisallowMultipleComponent],
                // so Unity also emits its own error whose wording is version-dependent. Ignore
                // that residual message without requiring its exact text.
                var previousIgnore = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;
                try
                {
                    instantiator.AddAnimation(new[] { clip });
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = previousIgnore;
                }

                Assert.IsInstanceOf<ConsoleLogger>(ReadStoredLogger(instantiator));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }
#endif

#if DEBUG
        [Test]
        public static void GetFullMessage_KnownCode_ReturnsTemplate()
        {
            Assert.AreEqual(
                "Invalid JSON chunk",
                LogMessages.GetFullMessage(LogCode.ChunkJsonInvalid));
        }

        [Test]
        public static void GetFullMessage_KnownCode_FormatsArguments()
        {
            Assert.AreEqual(
                "Unknown chunk type 42",
                LogMessages.GetFullMessage(LogCode.ChunkUnknown, "42"));
        }
#endif

        [Test]
        public static void GetFullMessage_UnknownCode_NoArgs_ReturnsCodeString()
        {
            var unknown = (LogCode)9999;
            Assert.AreEqual(
                unknown.ToString(),
                LogMessages.GetFullMessage(unknown));
        }

        [Test]
        public static void GetFullMessage_UnknownCode_WithArgs_AppendsArguments()
        {
            var unknown = (LogCode)9999;
            Assert.AreEqual(
                $"{unknown};m1;m2",
                LogMessages.GetFullMessage(unknown, "m1", "m2"));
        }

        [Test]
        public static void GetFullMessage_None_NoArgs_ReturnsEmpty()
        {
            Assert.AreEqual(
                "",
                LogMessages.GetFullMessage(LogCode.None));
        }

        [Test]
        public static void GetFullMessage_None_WithArgs_JoinsWithSemicolon()
        {
            Assert.AreEqual(
                "m1;m2",
                LogMessages.GetFullMessage(LogCode.None, "m1", "m2"));
        }

        [Test]
        public static void TestLogItemEquals()
        {
            var a = new LogItem(LogType.Error, LogCode.None);
            var b = new LogItem(LogType.Error, LogCode.None);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "MyMessage");
            b = new LogItem(LogType.Log, LogCode.None, "MyMessage");
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "FirstMessage", "SecondMessage");
            b = new LogItem(LogType.Log, LogCode.None, "FirstMessage", "SecondMessage");
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Error, LogCode.None);
            b = new LogItem(LogType.Assert, LogCode.None);
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Error, LogCode.None);
            b = new LogItem(LogType.Error, LogCode.EmbedSlow);
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "MyMessage");
            b = new LogItem(LogType.Log, LogCode.None);
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "MyMessage");
            b = new LogItem(LogType.Log, LogCode.None, "DifferentMessage");
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "FirstMessage", "SecondMessage");
            b = new LogItem(LogType.Log, LogCode.None, "FirstMessage", "DifferentSecondMessage");
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());

            a = new LogItem(LogType.Log, LogCode.None, "FirstMessage", "SecondMessage");
            b = new LogItem(LogType.Log, LogCode.None, "FirstMessage");
            Assert.AreNotEqual(a, b);
            Assert.AreNotEqual(a.GetHashCode(), b.GetHashCode());
        }

        internal static void AssertLogger(CollectingLogger logger)
        {
            AssertLogCodes(logger.Items, null);
        }

        internal static void AssertLogger(CollectingLogger logger, IEnumerable<LogCode> expectedLogCodes)
        {
            AssertLogCodes(logger.Items, expectedLogCodes);
        }

        internal static void AssertLogCodes(IEnumerable<LogItem> logItems, IEnumerable<LogCode> expectedLogCodes)
        {
            Dictionary<LogCode, bool> expectedLogCodeFound = null;
            if (expectedLogCodes != null)
            {
                expectedLogCodeFound = new Dictionary<LogCode, bool>();
                foreach (var logCode in expectedLogCodes)
                {
                    expectedLogCodeFound[logCode] = false;
                }
            }

            if (logItems != null)
            {
                foreach (var item in logItems)
                {
                    switch (item.Type)
                    {
                        case LogType.Assert:
                        case LogType.Error:
                        case LogType.Exception:
                            if (expectedLogCodeFound?.Keys.Contains(item.Code) == true)
                            {
                                expectedLogCodeFound[item.Code] = true;
                                // Informal log
                                Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, item.ToString());
                            }
                            else
                            {
                                item.Log();
                                throw new AssertionException($"Unhandled {item.Type} message {item} ({item.Code}).");
                            }
                            break;
                        case LogType.Warning:
                        case LogType.Log:
                        default:
                            item.Log();
                            break;
                    }
                }
            }

            if (expectedLogCodeFound != null)
            {
                foreach (var b in expectedLogCodeFound.Where(b => !b.Value))
                {
                    throw new AssertionException($"Missing expected log message {b.Key}.");
                }
            }
        }

        internal static void AssertLogger(CollectingLogger logger, IEnumerable<LogItem> expectedLogItems)
        {
            AssertLogItems(logger.Items, expectedLogItems);
        }

        static void AssertLogItems(IEnumerable<LogItem> logItems, IEnumerable<LogItem> expectedLogItems)
        {
            var items = expectedLogItems.ToList();

            foreach (var item in logItems)
            {
                var index = items.IndexOf(item);
                if (index >= 0)
                {
                    items.RemoveAt(index);
                    // Informal log
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, item.ToString());
                    continue;
                }

                item.Log();
            }

            foreach (var b in items)
            {
                throw new AssertionException($"Missing expected log message \"{b}\".");
            }
        }
    }
}
