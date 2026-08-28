// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Editor.Tests
{
    class NativeFileReaderTests
    {
        static readonly byte[] k_Content = { 0x67, 0x6c, 0x54, 0x46, 0x02, 0x00, 0x00, 0x00 };

        [Test]
        public void ReadsEntireFile()
        {
            var path = CreateFile(nameof(ReadsEntireFile), k_Content);

            Assert.IsTrue(NativeFileReader.TryReadAllBytes(path, out var data, out var error));
            try
            {
                Assert.IsNull(error);
                CollectionAssert.AreEqual(k_Content, data.ToArray());
            }
            finally
            {
                if (data.IsCreated)
                    data.Dispose();
            }
        }

        [Test]
        public void ReadsEmptyFile()
        {
            var path = CreateFile(nameof(ReadsEmptyFile), Array.Empty<byte>());

            Assert.IsTrue(NativeFileReader.TryReadAllBytes(path, out var data, out var error));
            try
            {
                Assert.IsNull(error);
                Assert.AreEqual(0, data.Length);
            }
            finally
            {
                if (data.IsCreated)
                    data.Dispose();
            }
        }

        [Test]
        public void MissingFileFails()
        {
            var path = Path.Combine(Application.temporaryCachePath, "MissingFileFails.bin");
            if (File.Exists(path))
                File.Delete(path);

            Assert.IsFalse(NativeFileReader.TryReadAllBytes(path, out var data, out var error));
            Assert.IsFalse(data.IsCreated);
            Assert.IsNotNull(error);
            StringAssert.Contains(path, error);
        }

        static string CreateFile(string name, byte[] content)
        {
            var path = Path.Combine(Application.temporaryCachePath, $"{name}.bin");
            File.WriteAllBytes(path, content);
            return path;
        }
    }
}
