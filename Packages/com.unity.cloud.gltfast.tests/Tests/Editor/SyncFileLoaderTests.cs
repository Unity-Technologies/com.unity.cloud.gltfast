// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    class SyncFileLoaderTests
    {
        static readonly byte[] k_Content = { 0x67, 0x6c, 0x54, 0x46, 0x02, 0x00, 0x00, 0x00 };

        [Test]
        public void MissingFile()
        {
            var path = Path.Combine(Application.temporaryCachePath, "MissingFile.gltf");
            if (File.Exists(path))
                File.Delete(path);

            using var loader = new SyncFileLoader(new Uri(path, UriKind.RelativeOrAbsolute));

            Assert.IsFalse(loader.Success);
            Assert.IsNotNull(loader.Error);
            StringAssert.Contains(path, loader.Error);
            Assert.AreEqual(0, loader.Data.Length);
        }

        [Test]
        public void ReadsFile()
        {
            using var loader = new SyncFileLoader(CreateFileUri(nameof(ReadsFile)));

            Assert.IsTrue(loader.Success);
            Assert.IsNull(loader.Error);
            CollectionAssert.AreEqual(k_Content, loader.Data.ToArray());
            Assert.AreEqual(true, loader.IsBinary);
        }

        [Test]
        public void DataCopiesToManagedArray()
        {
            using var loader = new SyncFileLoader(CreateFileUri(nameof(DataCopiesToManagedArray)));
            CollectionAssert.AreEqual(k_Content, loader.Data);
        }

        [Test]
        public void DisposeReleasesData()
        {
            var loader = new SyncFileLoader(CreateFileUri(nameof(DisposeReleasesData)));
            Assert.IsTrue(loader.Success);

            loader.Dispose();

            Assert.IsFalse(loader.Success);
            Assert.AreEqual(0, loader.Data.Length);
        }

        static Uri CreateFileUri(string name)
        {
            var path = Path.Combine(Application.temporaryCachePath, $"{name}.gltf");
            File.WriteAllBytes(path, k_Content);
            return new Uri(path, UriKind.RelativeOrAbsolute);
        }
    }
}
