// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections;
using System.Threading.Tasks;
using GLTFast.Logging;
using NUnit.Framework;
using Unity.Collections;
#if UNITY_ENTITIES_GRAPHICS
using Unity.Entities;
using Unity.Rendering;
#endif
using UnityEngine;
using UnityEngine.TestTools;

namespace GLTFast.Tests.Import
{
    [Category("Import")]
    class InstantiatorAddMeshTests
    {
        const string k_TriangleGltf =
            @"{""asset"":{""version"":""2.0""},""scene"":0,""scenes"":[{""nodes"":[0]}],""nodes"":[{""mesh"":0}]," +
            @"""meshes"":[{""primitives"":[{""attributes"":{""POSITION"":0}}]}]," +
            @"""accessors"":[{""bufferView"":0,""componentType"":5126,""count"":3,""type"":""VEC3""," +
            @"""min"":[0,0,0],""max"":[1,1,0]}],""bufferViews"":[{""buffer"":0,""byteLength"":36}]," +
            @"""buffers"":[{""uri"":""data:application/gltf-buffer;base64," +
            @"AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAA"",""byteLength"":36}]}";

        const string k_InstancedTriangleGltf =
            @"{""asset"":{""version"":""2.0""},""scene"":0,""scenes"":[{""nodes"":[0]}]," +
            @"""extensionsUsed"":[""EXT_mesh_gpu_instancing""]," +
            @"""nodes"":[{""mesh"":0,""extensions"":{""EXT_mesh_gpu_instancing"":{" +
            @"""attributes"":{""TRANSLATION"":1}}}}]," +
            @"""meshes"":[{""primitives"":[{""attributes"":{""POSITION"":0}}]}]," +
            @"""accessors"":[{""bufferView"":0,""componentType"":5126,""count"":3,""type"":""VEC3""," +
            @"""min"":[0,0,0],""max"":[1,1,0]}," +
            @"{""bufferView"":1,""componentType"":5126,""count"":2,""type"":""VEC3""}]," +
            @"""bufferViews"":[{""buffer"":0,""byteLength"":36}," +
            @"{""buffer"":0,""byteOffset"":36,""byteLength"":24}]," +
            @"""buffers"":[{""uri"":""data:application/gltf-buffer;base64," +
            @"AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAA""," +
            @"""byteLength"":60}]}";

        [UnityTest]
        public IEnumerator ImplementingAddMeshReceivesMesh()
        {
            var instantiator = new AddMeshInstantiator();
            var task = InstantiateInternal(instantiator, k_TriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual(1, instantiator.MeshCount);
            Assert.AreEqual(0, instantiator.PrimitiveCount);
        }

        [UnityTest]
        public IEnumerator ImplementingAddPrimitiveReceivesMesh()
        {
            var instantiator = new AddPrimitiveInstantiator();
            var task = InstantiateInternal(instantiator, k_TriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual(1, instantiator.MeshCount);
        }

        [UnityTest]
        public IEnumerator ImplementingAddMeshInstancedReceivesInstancedMesh()
        {
            var instantiator = new AddMeshInstantiator();
            var task = InstantiateInternal(instantiator, k_InstancedTriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual(0, instantiator.MeshCount);
            Assert.AreEqual(1, instantiator.InstancedMeshCount);
            Assert.AreEqual(2, instantiator.InstanceCount);
            Assert.AreEqual(0, instantiator.InstancedPrimitiveCount);
        }

        [UnityTest]
        public IEnumerator ImplementingAddPrimitiveInstancedReceivesInstancedMesh()
        {
            var instantiator = new AddPrimitiveInstantiator();
            var task = InstantiateInternal(instantiator, k_InstancedTriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.AreEqual(0, instantiator.MeshCount);
            Assert.AreEqual(1, instantiator.InstancedMeshCount);
            Assert.AreEqual(2, instantiator.InstanceCount);
        }

        [UnityTest]
        public IEnumerator GameObjectInstantiatorAddsMesh()
        {
            var root = new GameObject(nameof(GameObjectInstantiatorAddsMesh));
            var task = InstantiateWithGameObjectInstantiator(root, k_TriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            var meshFilter = root.GetComponentInChildren<MeshFilter>(true);
            Assert.IsNotNull(meshFilter);
            Assert.IsNotNull(meshFilter.sharedMesh);
            task.Result.Dispose();
            UnityEngine.Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator GameObjectInstantiatorAddsInstancedMesh()
        {
            var root = new GameObject(nameof(GameObjectInstantiatorAddsInstancedMesh));
            var task = InstantiateWithGameObjectInstantiator(root, k_InstancedTriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.IsNotEmpty(root.GetComponentsInChildren<MeshFilter>(true));
            task.Result.Dispose();
            UnityEngine.Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator GameObjectBoundsInstantiatorCalculatesBounds()
        {
            var root = new GameObject(nameof(GameObjectBoundsInstantiatorCalculatesBounds));
            var import = new GltfImport(
                deferAgent: new UninterruptedDeferAgent(),
                logger: new CollectingLogger());
            var loadTask = import.LoadGltfJson(k_TriangleGltf);
            yield return AsyncWrapper.WaitForTask(loadTask);
            Assert.IsTrue(loadTask.Result);
            var instantiator = new GameObjectBoundsInstantiator(import, root.transform);
            var instantiateTask = import.InstantiateSceneAsync(instantiator);
            yield return AsyncWrapper.WaitForTask(instantiateTask);
            Assert.IsTrue(instantiateTask.Result);
            Assert.IsNotNull(instantiator.CalculateBounds());
            import.Dispose();
            UnityEngine.Object.Destroy(root);
        }

        static async Task<GltfImport> InstantiateWithGameObjectInstantiator(GameObject root, string gltfJson)
        {
            var import = new GltfImport(
                deferAgent: new UninterruptedDeferAgent(),
                logger: new CollectingLogger());
            Assert.IsTrue(await import.LoadGltfJson(gltfJson));
            Assert.IsTrue(await import.InstantiateSceneAsync(new GameObjectInstantiator(import, root.transform)));
            return import;
        }

#if UNITY_ENTITIES_GRAPHICS
        [UnityTest]
        public IEnumerator EntityInstantiatorAddsMesh()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            var sceneRoot = EntityUtils.CreateSceneRootEntity(world);
            var query = world.EntityManager.CreateEntityQuery(typeof(MaterialMeshInfo));
            var before = query.CalculateEntityCount();
            var task = InstantiateWithEntityInstantiator(sceneRoot, k_TriangleGltf);
            yield return AsyncWrapper.WaitForTask(task);
            Assert.Greater(query.CalculateEntityCount(), before);
            var entityManager = world.EntityManager;
            EntityUtils.DestroyChildren(ref sceneRoot, ref entityManager);
            entityManager.DestroyEntity(sceneRoot);
        }

        static async Task InstantiateWithEntityInstantiator(Entity sceneRoot, string gltfJson)
        {
            using var gltf = new GltfImport(
                deferAgent: new UninterruptedDeferAgent(),
                logger: new CollectingLogger());
            Assert.IsTrue(await gltf.LoadGltfJson(gltfJson));
            Assert.IsTrue(await gltf.InstantiateSceneAsync(new EntityInstantiator(gltf, sceneRoot)));
        }
#endif

        static async Task InstantiateInternal(IInstantiator instantiator, string gltfJson)
        {
            using var import = new GltfImport(
                deferAgent: new UninterruptedDeferAgent(),
                logger: new CollectingLogger());
            Assert.IsTrue(await import.LoadGltfJson(gltfJson));
            Assert.IsTrue(await import.InstantiateSceneAsync(instantiator));
        }

        // Must not declare IInstantiator: a class's interface map is fixed where the interface is declared, so
        // deriving classes' mesh members would not be reached through an IInstantiator reference.
        abstract class StubInstantiator
        {
            public void BeginScene(string name, uint[] rootNodeIndices) { }

            public void CreateNode(
                uint nodeIndex,
                uint? parentIndex,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale
            )
            { }

            public void SetNodeName(uint nodeIndex, string name) { }

            public void AddCamera(uint nodeIndex, uint cameraIndex) { }

            public void AddLightPunctual(uint nodeIndex, uint lightIndex) { }

            public void EndScene(uint[] rootNodeIndices) { }

#if UNITY_ANIMATION
            public void AddAnimation(AnimationClip[] animationClips) { }
#endif
        }

        class AddMeshInstantiator : StubInstantiator, IInstantiator
        {
            public int MeshCount { get; private set; }
            public int InstancedMeshCount { get; private set; }
            public uint InstanceCount { get; private set; }
            public int PrimitiveCount { get; private set; }
            public int InstancedPrimitiveCount { get; private set; }

            public void AddMesh(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                uint[] joints = null,
                uint? rootJoint = null,
                float[] morphTargetWeights = null,
                int meshNumeration = 0
            ) => MeshCount++;

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
            {
                InstancedMeshCount++;
                InstanceCount = instanceCount;
            }

            [Obsolete("Use AddMesh instead.")]
            public void AddPrimitive(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                uint[] joints = null,
                uint? rootJoint = null,
                float[] morphTargetWeights = null,
                int meshNumeration = 0
            ) => PrimitiveCount++;

            [Obsolete("Use AddMeshInstanced instead.")]
            public void AddPrimitiveInstanced(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                uint instanceCount,
                NativeArray<Vector3>? positions,
                NativeArray<Quaternion>? rotations,
                NativeArray<Vector3>? scales,
                int meshNumeration = 0
            ) => InstancedPrimitiveCount++;
        }

        class AddPrimitiveInstantiator : StubInstantiator, IInstantiator
        {
            public int MeshCount { get; private set; }
            public int InstancedMeshCount { get; private set; }
            public uint InstanceCount { get; private set; }

            public void AddPrimitive(
                uint nodeIndex,
                string meshName,
                MeshResult meshResult,
                uint[] joints = null,
                uint? rootJoint = null,
                float[] morphTargetWeights = null,
                int meshNumeration = 0
            ) => MeshCount++;

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
            {
                InstancedMeshCount++;
                InstanceCount = instanceCount;
            }
        }
    }
}
