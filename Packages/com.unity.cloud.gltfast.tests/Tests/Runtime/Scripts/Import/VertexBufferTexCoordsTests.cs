// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Cloud.Gltfast.Vertex;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Cloud.Gltfast.Tests
{
    class VertexBufferTexCoordsTests
    {
        [Test]
        public void SparseTexCoords()
        {
            // A sparse accessor is rejected before any data is read, so the store needs no memory.
            var root = new Root
            {
                Accessors = new List<Accessor>
                {
                    new()
                    {
                        BufferView = 0,
                        ComponentType = AccessorDataType.Float,
                        Count = 1,
                        Sparse = new AccessorSparse()
                    }
                }
            };
            using var fixture = new BufferStoreFixture(root);

            var v = new VertexBufferTexCoords<VTexCoord1>(1, 1, null);

            var handles = new NativeArray<JobHandle>(1, Allocator.Temp);
            var success = v.ScheduleVertexUVJobs(
                0,
                new[] { 0 },
                handles,
                fixture.Store
                );
            Assert.IsFalse(success);
            v.Dispose();
            handles.Dispose();
        }
    }
}
