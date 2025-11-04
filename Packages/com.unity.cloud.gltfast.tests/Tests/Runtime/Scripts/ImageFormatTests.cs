// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using GLTFast.Export;
using NUnit.Framework;
using UnityEngine;

namespace GLTFast.Tests
{
    class ImageFormatTests
    {
        [Test]
        public void FromMimeType()
        {
            Assert.AreEqual(ImageFormat.Jpeg, ImageFormatExtensions.FromMimeType("image/jpeg"));
            Assert.AreEqual(ImageFormat.PNG, ImageFormatExtensions.FromMimeType("image/png"));
            Assert.AreEqual(ImageFormat.Ktx, ImageFormatExtensions.FromMimeType("image/ktx"));
            Assert.AreEqual(ImageFormat.Ktx, ImageFormatExtensions.FromMimeType("image/ktx2"));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType("image/fantasy-format"));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType("application/jpeg"));
            Assert.AreEqual(ImageFormat.Unknown, ImageFormatExtensions.FromMimeType(null));
        }
    }
}
