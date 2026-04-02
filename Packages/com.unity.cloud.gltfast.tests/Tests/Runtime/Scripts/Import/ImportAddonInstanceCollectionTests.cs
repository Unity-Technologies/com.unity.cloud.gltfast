// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GLTFast.Addons;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using Texture = GLTFast.Schema.Texture;

namespace GLTFast.Tests
{
    class ImportAddonInstanceCollectionTests
    {
        const int k_TextureCount = 100;
        const int k_AddonCount = 10;
        const string k_ExtensionName = "ext";

        ImportAddonInstanceCollection m_Collection;
        ImportAddonInstanceCollection m_TextureAddons;

        Texture[] m_Textures = new Texture[k_TextureCount];

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

            m_Textures = new Texture[k_TextureCount];
            for (var i = 0; i < k_TextureCount; i++)
            {
                m_Textures[i] = new Texture { source = i };
            }

            m_TextureAddons = new ImportAddonInstanceCollection();
            for (var i = 0; i < k_AddonCount; i++)
            {
                ImportAddonInstance addon = i >= k_AddonCount - 1
                    ? new TextureAddon()
                    : new AddonA();
                m_TextureAddons.Add(addon);
            }
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

        static readonly Func<ImportAddonInstance, bool> s_SupportsGltfExtensionPredicate =
            addon => addon.SupportsGltfExtension(k_ExtensionName);

        [Test]
        public void First()
        {
            Profiler.BeginSample("ImportAddonInstanceCollectionTests.Exists");
            var addon = m_Collection.First(s_SupportsGltfExtensionPredicate);
            Profiler.EndSample();
            Assert.NotNull(addon);
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

        [Test, Performance]
        public void ForEachTryGet()
        {
            var sg = new SampleGroup("Time", SampleUnit.Microsecond);
            Measure.Method(() =>
                {
                    bool OverridesImage(ITextureImageLoader loader, TextureBase texture, out int imageIndex)
                    {
                        return loader.IsAbleToLoad(texture, out imageIndex);
                    }

                    Dictionary<int, int> overrides = null;

                    m_TextureAddons
                        .SubCollection<ITextureImageLoader>()
                        .ForEachTryGet<TextureBase, int>(
                        m_Textures,
                        OverridesImage,
                        (addon, textureIndex, imageIndex) =>
                        {
                            overrides ??= new Dictionary<int, int>();
                            overrides[textureIndex] = imageIndex;
                        }
                    );

                    Assert.IsNotNull(overrides);
                    Assert.AreEqual(k_TextureCount / 4, overrides.Count);
                }
                    )
                .SampleGroup(sg)
                .MeasurementCount(1000)
                .Run();
        }
    }

    class AddonA : AddonBase { }
    class AddonB : AddonBase { }
    class AddonC : AddonBase { }

    class TextureAddon : AddonBase, ITextureImageLoader
    {
        public bool IsAbleToLoad(TextureBase texture, out int imageIndex)
        {
            if (texture.source % 4 == 0)
            {
                imageIndex = texture.source * 2;
                return true;
            }
            imageIndex = -1;
            return false;
        }

        public Task<ImageResult> LoadImage(
            NativeArray<byte>.ReadOnly data,
            bool linear,
            bool readable,
            bool generateMipMaps,
            CancellationToken cancellationToken
        )
        {
            throw new NotImplementedException();
        }
    }

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
