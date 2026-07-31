// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Schema;

namespace Unity.Cloud.Gltfast.Tests.JsonParsing
{
    [TestFixture]
    [Category("JsonParsing")]
    class EnumOrRawValueTests
    {
        static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);

        [Test]
        public void Equals_OtherStruct_BothDefault()
        {
            var a = default(EnumOrRawValue<AccessorType>);
            var b = default(EnumOrRawValue<AccessorType>);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_OtherStruct_SameEnumValue()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector3);
            var b = new EnumOrRawValue<AccessorType>(AccessorType.Vector3);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_OtherStruct_DifferentEnumValue()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector3);
            var b = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_OtherStruct_SameRawValueDifferentArrays()
        {
            // Different array instances but the same bytes — must compare by content.
            var a = new EnumOrRawValue<AccessorType>(Utf8("FOO"));
            var b = new EnumOrRawValue<AccessorType>(Utf8("FOO"));
            Assert.AreNotSame(a.RawValue, b.RawValue);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_OtherStruct_DifferentRawValueBytes()
        {
            var a = new EnumOrRawValue<AccessorType>(Utf8("FOO"));
            var b = new EnumOrRawValue<AccessorType>(Utf8("BAR"));
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void Equals_OtherStruct_OneRawValueOneNull()
        {
            var a = new EnumOrRawValue<AccessorType>(Utf8("FOO"));
            var b = new EnumOrRawValue<AccessorType>(AccessorType.Undefined);
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(b.Equals(a));
        }

        [Test]
        public void Equals_Enum_SameValueNoRawValue()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector4);
            Assert.IsTrue(a.Equals(AccessorType.Vector4));
        }

        [Test]
        public void Equals_Enum_DifferentValue()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector4);
            Assert.IsFalse(a.Equals(AccessorType.Scalar));
        }

        [Test]
        public void Equals_Enum_RawValueSetIsNeverEqual()
        {
            // Even if Value happens to match, a non-null RawValue means the
            // wrapper represents an unrecognized string, not the enum value.
            var a = new EnumOrRawValue<AccessorType>(Utf8("SCALAR"));
            Assert.AreEqual(default(AccessorType), a.Value);
            Assert.IsFalse(a.Equals(default(AccessorType)));
        }

        [Test]
        public void Equals_Enum_DefaultStructEqualsDefaultEnum()
        {
            var a = default(EnumOrRawValue<AccessorType>);
            Assert.IsTrue(a.Equals(AccessorType.Undefined));
        }

        [Test]
        public void Equals_Object_EqualWrapper()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Matrix3x3);
            object b = new EnumOrRawValue<AccessorType>(AccessorType.Matrix3x3);
            Assert.IsTrue(a.Equals(b));
        }

        [Test]
        public void Equals_Object_Null()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Matrix3x3);
            Assert.IsFalse(a.Equals(null));
        }

        [Test]
        public void Equals_Object_DifferentType()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Matrix3x3);
            Assert.IsFalse(a.Equals("Matrix3x3"));
            // Boxed bare enum is also not an EnumOrRawValue, so not equal.
            Assert.IsFalse(a.Equals((object)AccessorType.Matrix3x3));
        }

        [Test]
        public void Equals_Object_SameTypeUnequal()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Matrix3x3);
            object b = new EnumOrRawValue<AccessorType>(AccessorType.Matrix4x4);
            Assert.IsFalse(a.Equals(b));
        }

        [Test]
        public void GetHashCode_EqualInstancesShareHash()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector2);
            var b = new EnumOrRawValue<AccessorType>(AccessorType.Vector2);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void GetHashCode_DefaultIsStable()
        {
            var a = default(EnumOrRawValue<AccessorType>);
            var b = default(EnumOrRawValue<AccessorType>);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        [Test]
        public void ToString_EnumValue()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Vector2);
            Assert.AreEqual("Vector2", a.ToString());
        }

        [Test]
        public void ToString_Default()
        {
            var a = default(EnumOrRawValue<AccessorType>);
            Assert.AreEqual(AccessorType.Undefined.ToString(), a.ToString());
        }

        [Test]
        public void ToString_RawValueIsUtf8Decoded()
        {
            var a = new EnumOrRawValue<AccessorType>(Utf8("Foo¹Bar"));
            Assert.AreEqual("Foo¹Bar", a.ToString());
        }

        [Test]
        public void ToString_RawValueTakesPrecedenceOverValue()
        {
            // RawValue should be reported even if Value happens to be non-default.
            // (The Value-only constructor sets RawValue to null, so the only way
            // to construct this state is via default + reflection or future API;
            // here we cover the documented "RawValue when set" branch.)
            var a = new EnumOrRawValue<AccessorType>(Utf8("UNKNOWN"));
            Assert.AreEqual("UNKNOWN", a.ToString());
        }

        [Test]
        public void OperatorEquals_Enum_Match()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
            Assert.IsTrue(a == AccessorType.Scalar);
        }

        [Test]
        public void OperatorEquals_Enum_Mismatch()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
            Assert.IsFalse(a == AccessorType.Vector2);
        }

        [Test]
        public void OperatorEquals_RawValueIsNeverEqualToEnum()
        {
            var a = new EnumOrRawValue<AccessorType>(Utf8("SCALAR"));
            Assert.IsFalse(a == AccessorType.Scalar);
            Assert.IsFalse(a == AccessorType.Undefined);
        }

        [Test]
        public void OperatorNotEquals_Enum_Match()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
            Assert.IsFalse(a != AccessorType.Scalar);
        }

        [Test]
        public void OperatorNotEquals_Enum_Mismatch()
        {
            var a = new EnumOrRawValue<AccessorType>(AccessorType.Scalar);
            Assert.IsTrue(a != AccessorType.Vector2);
        }

        [Test]
        public void ImplicitOperator_FromEnum()
        {
            EnumOrRawValue<AccessorType> a = AccessorType.Matrix2x2;
            Assert.AreEqual(AccessorType.Matrix2x2, a.Value);
            Assert.IsNull(a.RawValue);
        }

        [Test]
        public void ImplicitOperator_FromDefaultEnum()
        {
            EnumOrRawValue<AccessorType> a = default(AccessorType);
            Assert.AreEqual(default(AccessorType), a.Value);
            Assert.IsNull(a.RawValue);
            Assert.IsTrue(a.Equals(default(EnumOrRawValue<AccessorType>)));
        }
    }
}
