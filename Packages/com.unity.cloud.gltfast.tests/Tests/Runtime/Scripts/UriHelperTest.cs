// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;

namespace GLTFast.Tests
{
    class UriHelperTest
    {
        static Uri[] s_Glb = {
            new Uri("file.glb",UriKind.RelativeOrAbsolute),
            new Uri("file:///dir/sub/file.glb",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.glb",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.glb",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.glb?a=123&b=234",UriKind.RelativeOrAbsolute),
        };
        static Uri[] s_Gltf = {
            new Uri("file.gltf",UriKind.RelativeOrAbsolute),
            new Uri("file:///dir/sub/file.gltf",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.gltf",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.gltf",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/file.gltf?a=123&b=234",UriKind.RelativeOrAbsolute),
        };
        static Uri[] s_Unknown = {
            new Uri("f",UriKind.RelativeOrAbsolute),
            new Uri("file:///dir/sub/f",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/f",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/f",UriKind.RelativeOrAbsolute),
            new Uri("http://www.server.com/dir/sub/f?a=123&b=234)",UriKind.RelativeOrAbsolute),
        };

        [Test]
        public void GetBaseUriTest()
        {

            // relative file URI
            Assert.AreEqual(new Uri("", UriKind.RelativeOrAbsolute), UriHelper.GetBaseUri(new Uri("file.gltf", UriKind.Relative)));

            // HTTP(s) gltf
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file.gltf")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file.gltf")));
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file.gltf?a=123&b=456")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file.gltf?a=123&b=456")));
            // HTTP(s) glb
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file.glb")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file.glb")));
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file.glb?a=123&b=456")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file.glb?a=123&b=456")));
            // HTTP(s) none
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file")));
            Assert.AreEqual(new Uri("http://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("http://www.server.com/dir/sub/file?a=123&b=456")));
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file?a=123&b=456")));

            // file paths
            Assert.AreEqual(new Uri("file:///dir/sub/"), UriHelper.GetBaseUri(new Uri("file:///dir/sub/file.gltf")));
            Assert.AreEqual(new Uri(@"file://c:\dir\sub\"), UriHelper.GetBaseUri(new Uri(@"c:\dir\sub\file.gltf")));

            // special char `+`
            Assert.AreEqual(new Uri("https://www.server.com/dir/sub/"), UriHelper.GetBaseUri(new Uri("https://www.server.com/dir/sub/file+test.gltf")));
            Assert.AreEqual(new Uri("file:///dir/sub/"), UriHelper.GetBaseUri(new Uri("file:///dir/sub/file+test.gltf")));

            // Android paths
            Assert.AreEqual(new Uri("jar:file:///dir/sub/"), UriHelper.GetBaseUri(new Uri("jar:file:///dir/sub/file.gltf")));

            // relative paths
            var uri = new Uri("Assets/Some/Path/asset.glb", UriKind.Relative);
            var sep = Path.DirectorySeparatorChar;
            Assert.AreEqual(new Uri($"Assets{sep}Some{sep}Path", UriKind.Relative), UriHelper.GetBaseUri(uri));
        }

        [Test]
        public void GetBaseUriAndroidTest()
        {
            Assert.AreEqual(
                "jar:file:///data/Unicode❤♻Test/glTF/",
                UriHelper.GetBaseUri(new Uri("jar:file:///data/Unicode❤♻Test/glTF/Unicode❤♻Test.gltf")).OriginalString
                );
        }

        [Test]
        public void GetBaseUriNonWindowsTest()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    Assert.Ignore("Absolute POSIX paths result in System.UriFormatException on Microsoft platforms.");
                    return;
            }
            // file paths
            Assert.AreEqual(new Uri("file:///dir/sub/"), UriHelper.GetBaseUri(new Uri("/dir/sub/file.gltf")));
        }

        [Test]
        public void GetUriStringTest()
        {
            var baseUri = new Uri("http://www.server.com/dir/sub/");

            Assert.AreEqual(
                "file+test.gltf",
                UriHelper.GetUriString("file+test.gltf", null).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/sub/file+test.gltf",
                UriHelper.GetUriString("file+test.gltf", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/sub/sub2/sub3/file+test.gltf",
                UriHelper.GetUriString("sub2/sub3/file+test.gltf", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/sub/file.gltf",
                UriHelper.GetUriString("./file.gltf", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/file.gltf",
                UriHelper.GetUriString("../file.gltf", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/x/file.gltf",
                UriHelper.GetUriString("../x/file.gltf", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/other_folder/texture.png",
                UriHelper.GetUriString("../other_folder/texture.png", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/dir/sub/asset.glb",
                UriHelper.GetUriString("asset.glb", baseUri).ToString()
                );
            Assert.AreEqual(
                "http://www.server.com/other_folder/texture.png",
                UriHelper.GetUriString("../../../../other_folder/texture.png", baseUri).ToString()
            );
            Assert.AreEqual("http://www.server.com/dir/sub/", baseUri.ToString());

            var sep = Path.DirectorySeparatorChar;
            var relBaseUri = new Uri($"Assets{sep}Some{sep}Path", UriKind.Relative);
            Assert.AreEqual(
                $"Assets{sep}Some{sep}Path{sep}asset.glb",
                UriHelper.GetUriString("asset.glb", relBaseUri).ToString()
                );
            Assert.AreEqual(
                $"Assets{sep}Some{sep}other_folder{sep}texture.png",
                UriHelper.GetUriString($"..{sep}other_folder{sep}texture.png", relBaseUri).ToString()
                );
            Assert.AreEqual(
                $"other_folder{sep}texture.png",
                UriHelper.GetUriString($"..{sep}..{sep}..{sep}other_folder{sep}texture.png", relBaseUri).ToString()
                );
            Assert.AreEqual(
                $"other_folder{sep}texture.png",
                UriHelper.GetUriString($"..{sep}..{sep}..{sep}..{sep}other_folder{sep}texture.png", relBaseUri).ToString()
                );
            Assert.AreEqual(
                new Uri($"Assets{sep}Some{sep}Path", UriKind.Relative),
                relBaseUri
                );
        }

        [Test]
        public void GetUriStringAndroidTest()
        {
            // var baseUri = new Uri("jar:file:///🤡");
            // var uri = new Uri(Path.Join( baseUri.OriginalString, "file.bin"));
            // Assert.AreEqual("jar:file:///🤡/file.bin", uri.OriginalString);

            // var uri = UriHelper.GetUriString("file.bin", baseUri);

            var baseUri = new Uri("jar:file:///data/Unicode❤♻Test/glTF");
            Assert.AreEqual(
                "jar:file:///data/Unicode❤♻Test/glTF/Unicode❤♻Test.bin",
                UriHelper.GetUriString("Unicode❤♻Test.bin", baseUri).OriginalString
            );
        }

        [Test]
        public void RemoveDotSegments()
        {
            var sep = Path.DirectorySeparatorChar;
            var s = UriHelper.RemoveDotSegments("file.txt", out var parentLevels);
            Assert.AreEqual("file.txt", s);
            Assert.AreEqual(0, parentLevels);

            s = UriHelper.RemoveDotSegments("../other_folder/file.txt", out parentLevels);
            Assert.AreEqual($"other_folder{sep}file.txt", s);
            Assert.AreEqual(1, parentLevels);

            s = UriHelper.RemoveDotSegments("other_folder/../file.txt", out parentLevels);
            Assert.AreEqual("file.txt", s);
            Assert.AreEqual(0, parentLevels);

            s = UriHelper.RemoveDotSegments("other_folder/./file.txt", out parentLevels);
            Assert.AreEqual($"other_folder{sep}file.txt", s);
            Assert.AreEqual(0, parentLevels);

            s = UriHelper.RemoveDotSegments("other_folder/./../x/../file.txt", out parentLevels);
            Assert.AreEqual("file.txt", s);
            Assert.AreEqual(0, parentLevels);

            s = UriHelper.RemoveDotSegments("../other_folder/../x/../file.txt", out parentLevels);
            Assert.AreEqual("file.txt", s);
            Assert.AreEqual(1, parentLevels);
        }

        [Test]
        public void IsGltfBinaryTest()
        {
            Profiler.BeginSample("IsGltfBinaryProfile");
            for (int i = 0; i < 1000; i++)
            {
                for (int j = 0; j < s_Glb.Length; j++)
                {
                    Profiler.BeginSample("IsGltfBinaryUriTrue");
                    Assert.IsTrue(UriHelper.IsGltfBinary(s_Glb[j]));
                    Profiler.EndSample();
                }
                for (int j = 0; j < s_Gltf.Length; j++)
                {
                    Profiler.BeginSample("IsGltfBinaryUriFalse");
                    Assert.IsFalse(UriHelper.IsGltfBinary(s_Gltf[j]));
                    Profiler.EndSample();
                }
                for (int j = 0; j < s_Unknown.Length; j++)
                {
                    Profiler.BeginSample("IsGltfBinaryUriUnknown");
                    Assert.IsNull(UriHelper.IsGltfBinary(s_Unknown[j]));
                    Profiler.EndSample();
                }
            }
            Profiler.EndSample();
        }

        [Test]
        public void GetImageFormatFromUriTest()
        {
            Assert.AreEqual(ImageFormat.Unknown, UriHelper.GetImageFormatFromUri(null)); // shortest path
            Assert.AreEqual(ImageFormat.Unknown, UriHelper.GetImageFormatFromUri("")); // shortest path
            Assert.AreEqual(ImageFormat.Unknown, UriHelper.GetImageFormatFromUri("f")); // shortest path

            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("f.jpg")); // shortest path
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("file:///Some/Path/file.jpg"));
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("http://server.com/some.Path/file.jpg"));
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("https://server.com/some.Path/file.jpg?key=value.with.dots&otherkey=val&arrval[]=x"));

            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("f.jpeg")); // shortest path
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("file:///Some/Path/file.jpeg"));
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("http://server.com/some.Path/file.jpeg"));
            Assert.AreEqual(ImageFormat.Jpeg, UriHelper.GetImageFormatFromUri("https://server.com/some.Path/file.jpeg?key=value.with.dots&otherkey=val&arrval[]=x"));

            Assert.AreEqual(ImageFormat.PNG, UriHelper.GetImageFormatFromUri("f.png")); // shortest path
            Assert.AreEqual(ImageFormat.PNG, UriHelper.GetImageFormatFromUri("file:///Some/Path/file.png"));
            Assert.AreEqual(ImageFormat.PNG, UriHelper.GetImageFormatFromUri("http://server.com/some.Path/file.png"));
            Assert.AreEqual(ImageFormat.PNG, UriHelper.GetImageFormatFromUri("https://server.com/some.Path/file.png?key=value.with.dots&otherkey=val&arrval[]=x"));

            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("f.ktx")); // shortest path
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("file:///Some/Path/file.ktx"));
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("http://server.com/some.Path/file.ktx"));
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("https://server.com/some.Path/file.ktx?key=value.with.dots&otherkey=val&arrval[]=x"));

            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("f.ktx2")); // shortest path
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("file:///Some/Path/file.ktx2"));
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("http://server.com/some.Path/file.ktx2"));
            Assert.AreEqual(ImageFormat.Ktx, UriHelper.GetImageFormatFromUri("https://server.com/some.Path/file.ktx2?key=value.with.dots&otherkey=val&arrval[]=x"));
        }

        [Test]
        public void CombineTest()
        {
            Assert.AreEqual("base/file", UriHelper.Combine("base", "file"));
            Assert.AreEqual("base/file", UriHelper.Combine("base", "/file"));
            Assert.AreEqual("base/file", UriHelper.Combine("base", "\\file"));

            Assert.AreEqual("base/file", UriHelper.Combine("base/", "file"));
            Assert.AreEqual("base/file", UriHelper.Combine("base/", "/file"));
            Assert.AreEqual("base/file", UriHelper.Combine("base/", "\\file"));

            Assert.AreEqual("base\\file", UriHelper.Combine("base\\", "file"));
            Assert.AreEqual("base\\file", UriHelper.Combine("base\\", "/file"));
            Assert.AreEqual("base\\file", UriHelper.Combine("base\\", "\\file"));

            Assert.AreEqual("/base/file", UriHelper.Combine("/base", "file"));
            Assert.AreEqual("/base/file", UriHelper.Combine("/base", "/file"));
            Assert.AreEqual("/base/file", UriHelper.Combine("/base", "\\file"));

            Assert.AreEqual(@"c:\base\file", UriHelper.Combine("c:\\base", "file"));
            Assert.AreEqual(@"c:\base\file", UriHelper.Combine("c:\\base", "/file"));
            Assert.AreEqual(@"c:\base\file", UriHelper.Combine("c:\\base", "\\file"));

            Assert.AreEqual(@"file://c:\base\file", UriHelper.Combine("file://c:\\base", "file"));
            Assert.AreEqual(@"file://c:\base\file", UriHelper.Combine("file://c:\\base", "/file"));
            Assert.AreEqual(@"file://c:\base\file", UriHelper.Combine("file://c:\\base", "\\file"));

            Assert.AreEqual("file://base/file", UriHelper.Combine("file://base", "file"));
            Assert.AreEqual("file://base/file", UriHelper.Combine("file://base", "/file"));
            Assert.AreEqual("file://base/file", UriHelper.Combine("file://base", "\\file"));
        }
    }
}
