// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;

namespace GLTFast.Tests
{
    class PrimitiveComparerTests
    {
        static Attributes Deserialize(string json)
        {
            return JsonSerializer.Deserialize(json, GltfJsonContext.Default.Attributes);
        }

        [Test]
        public void EqualsBothExtensionsDataNull()
        {
            var x = Deserialize(@"{""POSITION"":0,""NORMAL"":1}");
            var y = Deserialize(@"{""POSITION"":0,""NORMAL"":1}");
            Assert.IsTrue(PrimitiveComparer.Equals(x, y));
            Assert.AreEqual(PrimitiveComparer.GetHashCode(x), PrimitiveComparer.GetHashCode(y));
        }

        [Test]
        public void EqualsExtensionsDataSameScalar()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            var y = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            Assert.IsTrue(PrimitiveComparer.Equals(x, y));
            Assert.AreEqual(PrimitiveComparer.GetHashCode(x), PrimitiveComparer.GetHashCode(y));
        }

        [Test]
        public void EqualsExtensionsDataSameMultipleKeys()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5,""_BATCH_ID"":12}");
            var y = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5,""_BATCH_ID"":12}");
            Assert.IsTrue(PrimitiveComparer.Equals(x, y));
            Assert.AreEqual(PrimitiveComparer.GetHashCode(x), PrimitiveComparer.GetHashCode(y));
        }

        [Test]
        public void EqualsExtensionsDataSameMultipleKeysReorderedJson()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5,""_BATCH_ID"":12}");
            var y = Deserialize(@"{""POSITION"":0,""_BATCH_ID"":12,""_TEMPERATURE"":5}");
            Assert.IsTrue(PrimitiveComparer.Equals(x, y));
            Assert.AreEqual(PrimitiveComparer.GetHashCode(x), PrimitiveComparer.GetHashCode(y));
        }

        [Test]
        public void EqualsExtensionsDataSameObjectValue()
        {
            var x = Deserialize(@"{""POSITION"":0,""_META"":{""a"":1,""b"":""x""}}");
            var y = Deserialize(@"{""POSITION"":0,""_META"":{""a"":1,""b"":""x""}}");
            Assert.IsTrue(PrimitiveComparer.Equals(x, y));
        }

        [Test]
        public void EqualsExtensionsDataDifferingValues()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            var y = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":6}");
            Assert.IsFalse(PrimitiveComparer.Equals(x, y));
        }

        [Test]
        public void EqualsExtensionsDataDifferingKeys()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            var y = Deserialize(@"{""POSITION"":0,""_PRESSURE"":5}");
            Assert.IsFalse(PrimitiveComparer.Equals(x, y));
        }

        [Test]
        public void EqualsExtensionsDataDifferingCounts()
        {
            var x = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            var y = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5,""_BATCH_ID"":12}");
            Assert.IsFalse(PrimitiveComparer.Equals(x, y));
        }

        [Test]
        public void EqualsExtensionsDataDifferingNestedObject()
        {
            var x = Deserialize(@"{""POSITION"":0,""_META"":{""a"":1,""b"":""x""}}");
            var y = Deserialize(@"{""POSITION"":0,""_META"":{""a"":1,""b"":""y""}}");
            Assert.IsFalse(PrimitiveComparer.Equals(x, y));
        }

        [Test]
        public void EqualsOneExtensionsDataNullOtherPopulated()
        {
            var x = Deserialize(@"{""POSITION"":0}");
            var y = Deserialize(@"{""POSITION"":0,""_TEMPERATURE"":5}");
            Assert.IsFalse(PrimitiveComparer.Equals(x, y));
            Assert.IsFalse(PrimitiveComparer.Equals(y, x));
        }
    }
}
