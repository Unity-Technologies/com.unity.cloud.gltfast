// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if !UNITY_ENTITIES_GRAPHICS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.Cloud.Gltfast.Tests.Import
{
    [Category("Import")]
    class NodeNamingTests
    {
        // A single non-indexed triangle: 3 VEC3 float positions (36 bytes).
        const string k_TriangleBufferBase64 =
            "AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAA";

        // Two instance translations for EXT_mesh_gpu_instancing: 2 VEC3 float (24 bytes).
        const string k_InstanceTranslationBufferBase64 =
            "AAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAA";

        static string SingleMeshGltf(string nodeName, string meshName)
        {
            var nodeNameJson = nodeName == null ? "" : $@"""name"":""{nodeName}"",";
            var meshNameJson = meshName == null ? "" : $@"""name"":""{meshName}"",";
            return $@"{{
""asset"":{{""version"":""2.0""}},
""scene"":0,
""scenes"":[{{""nodes"":[0]}}],
""nodes"":[{{{nodeNameJson}""mesh"":0}}],
""meshes"":[{{{meshNameJson}""primitives"":[{{""attributes"":{{""POSITION"":0}}}}]}}],
""accessors"":[{{""bufferView"":0,""componentType"":5126,""count"":3,""type"":""VEC3"",""min"":[0.0,0.0,0.0],""max"":[1.0,1.0,0.0]}}],
""bufferViews"":[{{""buffer"":0,""byteOffset"":0,""byteLength"":36,""target"":34962}}],
""buffers"":[{{""uri"":""data:application/octet-stream;base64,{k_TriangleBufferBase64}"",""byteLength"":36}}]
}}";
        }

        // Unnamed node with a named mesh instanced via EXT_mesh_gpu_instancing (2 instances).
        static string InstancedMeshGltf(string meshName)
        {
            return $@"{{
""asset"":{{""version"":""2.0""}},
""extensionsUsed"":[""EXT_mesh_gpu_instancing""],
""scene"":0,
""scenes"":[{{""nodes"":[0]}}],
""nodes"":[{{""mesh"":0,""extensions"":{{""EXT_mesh_gpu_instancing"":{{""attributes"":{{""TRANSLATION"":1}}}}}}}}],
""meshes"":[{{""name"":""{meshName}"",""primitives"":[{{""attributes"":{{""POSITION"":0}}}}]}}],
""accessors"":[
{{""bufferView"":0,""componentType"":5126,""count"":3,""type"":""VEC3"",""min"":[0.0,0.0,0.0],""max"":[1.0,1.0,0.0]}},
{{""bufferView"":1,""componentType"":5126,""count"":2,""type"":""VEC3"",""min"":[0.0,0.0,0.0],""max"":[2.0,0.0,0.0]}}
],
""bufferViews"":[
{{""buffer"":0,""byteOffset"":0,""byteLength"":36,""target"":34962}},
{{""buffer"":1,""byteOffset"":0,""byteLength"":24,""target"":34962}}
],
""buffers"":[
{{""uri"":""data:application/octet-stream;base64,{k_TriangleBufferBase64}"",""byteLength"":36}},
{{""uri"":""data:application/octet-stream;base64,{k_InstanceTranslationBufferBase64}"",""byteLength"":24}}
]
}}";
        }

        static Task<string> InstantiateFirstNodeName(string nodeName, string meshName)
            => InstantiateFirstNodeNameFromJson(SingleMeshGltf(nodeName, meshName), null);

        static async Task<GltfImport> LoadGltf(string gltfJson)
        {
            var gltf = new GltfImport(deferAgent: new UninterruptedDeferAgent());
            if (!await gltf.LoadGltfJsonAsync(gltfJson))
            {
                gltf.Dispose();
                Assert.Fail("Loading glTF JSON failed.");
            }
            return gltf;
        }

        static async Task<(T instantiator, string nodeName)> InstantiateFirstNode<T>(
            string gltfJson,
            Func<GltfImport, Transform, T> createInstantiator
            )
            where T : GameObjectInstantiator
        {
            using var gltf = await LoadGltf(gltfJson);

            var parent = new GameObject();
            try
            {
                var instantiator = createInstantiator(gltf, parent.transform);
                Assert.IsTrue(await gltf.InstantiateMainSceneAsync(instantiator));
                return (instantiator, parent.transform.GetChild(0).name);
            }
            finally
            {
                Object.Destroy(parent);
            }
        }

        static async Task<string> InstantiateFirstNodeNameFromJson(string gltfJson, InstantiationSettings settings)
        {
            var result = await InstantiateFirstNode(
                gltfJson,
                (gltf, parent) => new GameObjectInstantiator(gltf, parent, settings: settings));
            return result.nodeName;
        }

        static async Task<string> RecordedFirstNodeName(NameRecordingInstantiator instantiator, string gltfJson)
        {
            using var gltf = await LoadGltf(gltfJson);
            var success = await gltf.InstantiateMainSceneAsync((IInstantiator)instantiator);
            Assert.IsTrue(success);
            return instantiator.GetName(0);
        }

        static async Task<(string overriddenName, string nodeName)> SubclassedFirstNodeName(string gltfJson)
        {
            var (instantiator, nodeName) = await InstantiateFirstNode(
                gltfJson,
                (gltf, parent) => new NameOverridingInstantiator(gltf, parent));
            return (instantiator.OverriddenName, nodeName);
        }

        [UnityTest]
        public IEnumerator ExplicitNodeName()
        {
            var task = InstantiateFirstNodeName("MyNode", "MyMesh");
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result);
        }

        [UnityTest]
        public IEnumerator FallsBackToMeshName()
        {
            var task = InstantiateFirstNodeName(null, "MyMesh");
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator FallsBackToNodeIndex()
        {
            var task = InstantiateFirstNodeName(null, null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("Node-0", task.Result);
        }

        [UnityTest]
        public IEnumerator FallsBackToMeshNameWhenMeshMaskedOut()
        {
            // The mesh-name fallback must apply even when Mesh components are excluded.
            var settings = new InstantiationSettings { Mask = ComponentType.All & ~ComponentType.Mesh };
            var task = InstantiateFirstNodeNameFromJson(SingleMeshGltf(null, "MyMesh"), settings);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator FallsBackToMeshNameWhenInstanced()
        {
            var task = InstantiateFirstNodeNameFromJson(InstancedMeshGltf("MyMesh"), null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator LegacyImplementationStillGetsNamed()
        {
            var task = RecordedFirstNodeName(new LegacyInstantiator(), SingleMeshGltf("MyNode", null));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result);
        }

        [UnityTest]
        public IEnumerator NewOverloadAloneGetsNamed()
        {
            var task = RecordedFirstNodeName(new NamedCreateNodeInstantiator(), SingleMeshGltf("MyNode", null));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result);
        }

        [UnityTest]
        public IEnumerator LegacyAndNewImplementationsAgree()
        {
            var gltfJson = SingleMeshGltf(null, "MyMesh");
            var legacy = RecordedFirstNodeName(new LegacyInstantiator(), gltfJson);
            yield return AsyncWrapper.WaitForTask(legacy);
            var modern = RecordedFirstNodeName(new NamedCreateNodeInstantiator(), gltfJson);
            yield return AsyncWrapper.WaitForTask(modern);
            Assert.AreEqual(legacy.Result, modern.Result);
        }

        [UnityTest]
        public IEnumerator SubclassOverrideReceivesName()
        {
            var task = SubclassedFirstNodeName(SingleMeshGltf("MyNode", null));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result.overriddenName);
            Assert.AreEqual("MyNode", task.Result.nodeName);
        }

        class NameOverridingInstantiator : GameObjectInstantiator
        {
            public NameOverridingInstantiator(IGltfReadable gltf, Transform parent)
                : base(gltf, parent) { }

            public string OverriddenName { get; private set; }

            public override void CreateNode(
                uint nodeIndex,
                uint? parentIndex,
                double3 position,
                double4 rotation,
                double3 scale,
                string name
                )
            {
                OverriddenName = name;
                base.CreateNode(nodeIndex, parentIndex, position, rotation, scale, name);
            }
        }

        abstract class NameRecordingInstantiator
        {
            readonly Dictionary<uint, string> m_Names = new Dictionary<uint, string>();

            public string GetName(uint nodeIndex) => m_Names.TryGetValue(nodeIndex, out var name) ? name : null;

            protected void Record(uint nodeIndex, string name) => m_Names[nodeIndex] = name;

            public void BeginScene(string name, IReadOnlyList<uint> rootNodeIndices) { }

#if UNITY_ANIMATION
            public void AddAnimation(AnimationClip[] animationClips) { }
#endif

            public void AddPrimitive(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                IReadOnlyList<uint> joints = null,
                uint? rootJoint = null,
                IReadOnlyList<float> morphTargetWeights = null,
                int meshNumeration = 0
                )
            { }

            public void AddPrimitiveInstanced(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                uint instanceCount,
                NativeArray<Vector3>? positions,
                NativeArray<Quaternion>? rotations,
                NativeArray<Vector3>? scales,
                int meshNumeration = 0
                )
            { }

            public void AddCamera(uint nodeIndex, uint cameraIndex) { }

            public void AddLightPunctual(uint nodeIndex, uint lightIndex) { }

            public void EndScene(IReadOnlyList<uint> rootNodeIndices) { }
        }

        // Implements the pre-6.20 pair only; the interface's default CreateNode overload has to route to it.
        class LegacyInstantiator : NameRecordingInstantiator, IInstantiator
        {
            public void CreateNode(
                uint nodeIndex,
                uint? parentIndex,
                double3 position,
                double4 rotation,
                double3 scale
                )
            { }

            public void SetNodeName(uint nodeIndex, string name) => Record(nodeIndex, name);
        }

        // Implements the named overload only, as an implementation written against 7.0 would.
        class NamedCreateNodeInstantiator : NameRecordingInstantiator, IInstantiator
        {
            public void CreateNode(
                uint nodeIndex,
                uint? parentIndex,
                double3 position,
                double4 rotation,
                double3 scale,
                string name
                )
                => Record(nodeIndex, name);
        }
    }
}

#endif
