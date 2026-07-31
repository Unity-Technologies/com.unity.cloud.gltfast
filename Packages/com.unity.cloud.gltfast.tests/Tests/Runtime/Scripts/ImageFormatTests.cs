// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Export;
using Unity.Cloud.Gltfast.Schema;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Tests
{
    class ImageFormatTests
    {
        [Test]
        public void FromMimeType()
        {
            Assert.AreEqual(ImageFormat.Jpeg, ImageFormatExtensions.FromMimeType(ImageMimeType.Jpeg));
            Assert.AreEqual(ImageFormat.Png, ImageFormatExtensions.FromMimeType(ImageMimeType.Png));
            Assert.AreEqual(ImageFormat.Ktx, ImageFormatExtensions.FromMimeType(ImageMimeType.Ktx2));
            Assert.AreEqual(ImageFormat.WebP, ImageFormatExtensions.FromMimeType(ImageMimeType.WebP));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(ImageMimeType.Undefined));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(default(EnumOrRawValue<ImageMimeType>)));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(
                new EnumOrRawValue<ImageMimeType>(Encoding.UTF8.GetBytes("image/ktx"))));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(
                new EnumOrRawValue<ImageMimeType>(Encoding.UTF8.GetBytes("image/fantasy-format"))));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(
                new EnumOrRawValue<ImageMimeType>(Encoding.UTF8.GetBytes("application/jpeg"))));
        }
    }
}
