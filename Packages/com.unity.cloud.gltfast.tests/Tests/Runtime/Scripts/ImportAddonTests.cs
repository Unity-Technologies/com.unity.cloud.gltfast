// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Tests
{
    using Addons;

    static class ImportAddonTests
    {
        [Test]
        public static void GetImportAddonInstance()
        {
            var gltf = new GltfImport(logger: new ConsoleLogger());
            var addonA = gltf.GetImportAddonInstance<AddonInstanceA>();
            Assert.IsNull(addonA);

            ImportAddonRegistry.RegisterImportAddon(new AddonB());
            gltf = new GltfImport(logger: new ConsoleLogger());
            addonA = gltf.GetImportAddonInstance<AddonInstanceA>();
            Assert.IsNull(addonA);
            var addonB = gltf.GetImportAddonInstance<InstanceBase>();
            Assert.IsNotNull(addonB);
        }

        [UnityTest]
        public static IEnumerator PostJsonDeserialization()
        {
            yield return AsyncWrapper.WaitForTask(Test());
            yield break;

            async Task Test()
            {
                var gltf = new GltfImport(logger: new ConsoleLogger());
                new PostJsonDeserializationAddon().Inject(gltf);
                Assert.IsTrue(await gltf.LoadGltfJson(@"{""asset"":{""copyright"":""© 2026 Unity Technologies and the glTFast authors.""}}"));
                var root = gltf.GetSourceRoot();
                Assert.NotNull(root?.asset?.name);
                Assert.AreEqual("My Custom Asset Name", root.asset.name);
            }
        }

        [UnityTest]
        public static IEnumerator PostJsonDeserializationFail()
        {
            yield return AsyncWrapper.WaitForTask(Test());
            yield break;

            async Task Test()
            {
                var logger = new CollectingLogger();
                var gltf = new GltfImport(logger: logger);
                new PostJsonDeserializationAddon().Inject(gltf);
                Assert.IsFalse(await gltf.LoadGltfJson(@"{""asset"":{}}"));
                Assert.IsTrue(logger.Items.Any(
                    log => log.Type == LogType.Error && log.ToString() == "Asset copyright is missing."));
            }
        }

        class AddonA : ImportAddon<AddonInstanceA> { }
        class AddonB : ImportAddon<InstanceBase> { }

        class AddonInstanceA : ImportAddonInstance
        {
            public override bool SupportsGltfExtension(string extensionName)
            {
                return false;
            }

            public override void Inject(GltfImportBase gltfImport)
            {
                gltfImport.AddImportAddonInstance(this);
            }

            public override void Inject(IInstantiator instantiator) { }

            public override void Dispose() { }
        }

        class PostJsonDeserializationAddon : InstanceBase, IPostJsonDeserialization
        {
            public bool PostJsonDeserialization()
            {
                var gltf = m_GltfImport.GetSourceRoot();
                gltf.asset.name ??= "My Custom Asset Name";
                if (string.IsNullOrEmpty(gltf.asset.copyright))
                {
                    m_GltfImport.Logger.Error("Asset copyright is missing.");
                    return false;
                }
                return true;
            }
        }

        class InstanceBase : ImportAddonInstance
        {
            protected GltfImport m_GltfImport { get; private set; }

            public override bool SupportsGltfExtension(string extensionName) => false;

            public override void Inject(GltfImportBase gltfImport)
            {
                gltfImport.AddImportAddonInstance(this);
                m_GltfImport = gltfImport as GltfImport;
            }

            public override void Inject(IInstantiator instantiator)
            {
                throw new System.NotImplementedException();
            }

            public override void Dispose()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
