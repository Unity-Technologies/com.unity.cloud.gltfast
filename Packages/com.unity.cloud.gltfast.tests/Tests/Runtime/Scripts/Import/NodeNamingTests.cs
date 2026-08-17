// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_ENTITIES_GRAPHICS && UNITY_EDITOR
using Unity.Entities;
using Unity.Transforms;
#endif
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
        public IEnumerator NewOverloadAloneGetsNamed()
        {
            var task = RecordedFirstNodeName(new NamedCreateNodeInstantiator(), SingleMeshGltf("MyNode", null));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result);
        }

        [UnityTest]
        public IEnumerator UnnamedNodeReachesImplementationAsNull()
        {
            var task = RecordedFirstNodeName(new NamedCreateNodeInstantiator(), SingleMeshGltf(null, "MyMesh"));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.IsNull(task.Result);
        }

        [UnityTest]
        public IEnumerator SubclassOverrideReceivesName()
        {
            var task = SubclassedFirstNodeName(SingleMeshGltf("MyNode", null));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result.overriddenName);
            Assert.AreEqual("MyNode", task.Result.nodeName);
        }

        [UnityTest]
        public IEnumerator NodeCreatedReportsPlaceholderForUnnamedNode()
        {
            var task = NodeCreatedName(SingleMeshGltf(null, "MyMesh"));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("Node-0", task.Result.atEvent, "the mesh name is only known once meshes are assigned");
            Assert.AreEqual("MyMesh", task.Result.afterwards);
        }

        static async Task<(string atEvent, string afterwards)> NodeCreatedName(string gltfJson)
        {
            using var gltf = await LoadGltf(gltfJson);

            var parent = new GameObject();
            try
            {
                var instantiator = new GameObjectInstantiator(gltf, parent.transform);
                string atEvent = null;
                instantiator.NodeCreated += (_, gameObject) => atEvent ??= gameObject.name;
                Assert.IsTrue(await gltf.InstantiateMainSceneAsync(instantiator));
                return (atEvent, parent.transform.GetChild(0).name);
            }
            finally
            {
                Object.Destroy(parent);
            }
        }

        [UnityTest]
        public IEnumerator OverriddenFallbackNamesUnnamedNode()
        {
            var task = InstantiateFirstNode(
                SingleMeshGltf(null, "MyMesh"),
                (gltf, parent) => new FallbackOverridingInstantiator(gltf, parent));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("Custom-MyMesh", task.Result.nodeName);
            Assert.AreEqual(1, task.Result.instantiator.CallCount);
        }

        [UnityTest]
        public IEnumerator OverriddenFallbackSkipsNamedNode()
        {
            var task = InstantiateFirstNode(
                SingleMeshGltf("MyNode", "MyMesh"),
                (gltf, parent) => new FallbackOverridingInstantiator(gltf, parent));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result.nodeName);
            Assert.AreEqual(0, task.Result.instantiator.CallCount);
        }

        [UnityTest]
        public IEnumerator OverriddenFallbackRunsOnceWhenInstanced()
        {
            var task = InstantiateFirstNode(
                InstancedMeshGltf("MyMesh"),
                (gltf, parent) => new FallbackOverridingInstantiator(gltf, parent));
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("Custom-MyMesh", task.Result.nodeName);
            Assert.AreEqual(1, task.Result.instantiator.CallCount);
        }

        class FallbackOverridingInstantiator : GameObjectInstantiator
        {
            public FallbackOverridingInstantiator(IGltfReadable gltf, Transform parent)
                : base(gltf, parent) { }

            public int CallCount { get; private set; }

            protected override void SetFallbackNodeName(uint nodeIndex, string meshName)
            {
                CallCount++;
                base.SetFallbackNodeName(nodeIndex, "Custom-" + meshName);
            }
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

            public void AddMesh(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                IReadOnlyList<uint> joints = null,
                uint? rootJoint = null,
                IReadOnlyList<float> morphTargetWeights = null,
                int meshNumeration = 0
                )
            { }

            public void AddMeshInstanced(
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

#if UNITY_ENTITIES_GRAPHICS && UNITY_EDITOR
        static Task<string> InstantiateFirstEntityName(string nodeName, string meshName)
            => InstantiateFirstEntityNameFromJson(SingleMeshGltf(nodeName, meshName), null);

        static async Task<string> InstantiateFirstEntityNameFromJson(
            string gltfJson,
            InstantiationSettings settings
            )
            => (await InstantiateFirstEntity(gltfJson, settings)).nodeName;

        static async Task<(NodeCapturingEntityInstantiator instantiator, string nodeName)> InstantiateFirstEntity(
            string gltfJson,
            InstantiationSettings settings
            )
        {
            using var gltf = await LoadGltf(gltfJson);

            var world = World.DefaultGameObjectInjectionWorld;
            var sceneRoot = EntityUtils.CreateSceneRootEntity(world);
            var entityManager = world.EntityManager;
            try
            {
                var instantiator = new NodeCapturingEntityInstantiator(gltf, sceneRoot, settings);
                Assert.IsTrue(await gltf.InstantiateMainSceneAsync(instantiator));
                return (instantiator, entityManager.GetName(instantiator.FirstNode));
            }
            finally
            {
                DestroyHierarchy(sceneRoot, entityManager);
            }
        }

        // Not EntityUtils.DestroyChildren: that is [BurstCompile]d, and compiling it surfaces any unrelated
        // assembly-resolution failure in the project as a test-failing error log. Also destroys the root.
        static void DestroyHierarchy(Entity root, EntityManager entityManager)
        {
            if (entityManager.HasComponent<Child>(root))
            {
                var children = entityManager.GetBuffer<Child>(root);
                var toDestroy = new NativeArray<Entity>(children.Length, Allocator.Temp);
                for (var i = 0; i < children.Length; i++)
                {
                    toDestroy[i] = children[i].Value;
                }
                entityManager.DestroyEntity(toDestroy);
                toDestroy.Dispose();
            }
            entityManager.DestroyEntity(root);
        }

        [UnityTest]
        public IEnumerator EntityExplicitNodeName()
        {
            var task = InstantiateFirstEntityName("MyNode", "MyMesh");
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result);
        }

        [UnityTest]
        public IEnumerator EntityFallsBackToMeshName()
        {
            var task = InstantiateFirstEntityName(null, "MyMesh");
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator EntityFallsBackToNodeIndex()
        {
            var task = InstantiateFirstEntityName(null, null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("Node-0", task.Result);
        }

        [UnityTest]
        public IEnumerator EntityFallsBackToMeshNameWhenMeshMaskedOut()
        {
            var settings = new InstantiationSettings { Mask = ComponentType.All & ~ComponentType.Mesh };
            var task = InstantiateFirstEntityNameFromJson(SingleMeshGltf(null, "MyMesh"), settings);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator EntityFallsBackToMeshNameWhenInstanced()
        {
            var task = InstantiateFirstEntityNameFromJson(InstancedMeshGltf("MyMesh"), null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result);
        }

        [UnityTest]
        public IEnumerator EntityOverriddenFallbackRunsOnceForUnnamedNode()
        {
            var task = InstantiateFirstEntity(SingleMeshGltf(null, "MyMesh"), null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result.nodeName);
            Assert.AreEqual(1, task.Result.instantiator.FallbackCalls);
        }

        [UnityTest]
        public IEnumerator EntityOverriddenFallbackSkipsNamedNode()
        {
            var task = InstantiateFirstEntity(SingleMeshGltf("MyNode", "MyMesh"), null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyNode", task.Result.nodeName);
            Assert.AreEqual(0, task.Result.instantiator.FallbackCalls);
        }

        [UnityTest]
        public IEnumerator EntityOverriddenFallbackRunsOnceWhenInstanced()
        {
            var task = InstantiateFirstEntity(InstancedMeshGltf("MyMesh"), null);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual("MyMesh", task.Result.nodeName);
            Assert.AreEqual(1, task.Result.instantiator.FallbackCalls);
        }

        class NodeCapturingEntityInstantiator : EntityInstantiator
        {
            bool m_Captured;

            public Entity FirstNode { get; private set; }

            public int FallbackCalls { get; private set; }

            protected override void SetFallbackNodeName(uint nodeIndex, string meshName)
            {
                FallbackCalls++;
                base.SetFallbackNodeName(nodeIndex, meshName);
            }

            public NodeCapturingEntityInstantiator(
                IGltfReadable gltf,
                Entity parent,
                InstantiationSettings settings
                )
                : base(gltf, parent, settings: settings) { }

            public override void CreateNode(
                uint nodeIndex,
                uint? parentIndex,
                double3 position,
                double4 rotation,
                double3 scale,
                string name
                )
            {
                base.CreateNode(nodeIndex, parentIndex, position, rotation, scale, name);
                if (!m_Captured)
                {
                    FirstNode = m_Nodes[nodeIndex];
                    m_Captured = true;
                }
            }
        }
#endif // UNITY_ENTITIES_GRAPHICS && UNITY_EDITOR
    }
}
