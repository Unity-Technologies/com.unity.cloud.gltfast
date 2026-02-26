// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using GLTFast.Addons;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace GLTFast.Tests
{
    class ImportAddonInstanceCollectionTests
    {
        const string k_ExtensionName = "ext";

        ImportAddonInstanceCollection m_Collection;

        AddonA m_AddonA;
        AddonB m_AddonB;
        AddonC m_AddonC;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_Collection = new ImportAddonInstanceCollection();

            m_AddonA = new AddonA();
            m_AddonB = new AddonB();
            m_AddonC = new AddonC { Value = 42 };

            m_Collection.Add(m_AddonA);
            m_Collection.Add(m_AddonB);
            m_Collection.Add(m_AddonC);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Collection.Dispose();
        }

        [Test]
        public void RoundTrip()
        {
            var b = m_Collection.Get<AddonB>();
            Assert.AreSame(m_AddonB, b);

            var a = m_Collection.Get<AddonBase>();
            Assert.AreSame(m_AddonA, a);
        }

        [Test]
        public void AnySupportsGltfExtension()
        {
            Profiler.BeginSample("ImportAddonInstanceCollectionTests.AnySupportsGltfExtension");
            var supports = m_Collection.AnySupportsGltfExtension(k_ExtensionName);
            Profiler.EndSample();
            Assert.IsTrue(supports);
        }

        [Test]
        public void ForEach()
        {
            var count = 0;
            m_Collection.ForEach(instance =>
            {
                count++;
                Debug.Log(instance.ToString());
            });
            Assert.AreEqual(3, count);
        }
    }

    class AddonA : AddonBase { }
    class AddonB : AddonBase { }
    class AddonC : AddonBase { }

    class AddonBase : ImportAddonInstance
    {
        public int Value { get; set; }

        public override bool SupportsGltfExtension(string extensionName)
        {
            return extensionName == "ext" && Value == 42;
        }
        public override void Inject(GltfImportBase gltfImport) { }
        public override void Inject(IInstantiator instantiator) { }
        public override void Dispose() { }
    }
}
