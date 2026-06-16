// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class UriValueConverterTests
    {
        class UriHolder
        {
            [JsonPropertyName("uri")] public UriValue Uri { get; set; }
            [JsonPropertyName("count")] public int Count { get; set; }
        }

        // Single byte (0xAD = 173) base-64 encoded.
        const string k_OneBytePayload = "rQ==";

        // 4 bytes of arbitrary data, base-64.
        const string k_FourBytePayload = "rQbwDQ==";

        [Test]
        public void StringUri()
        {
            using var uri = Deserialize("\"images/foo.png\"");
            Assert.IsTrue(uri.IsString);
            Assert.IsFalse(uri.IsData);
            Assert.IsFalse(uri.IsFailed);
            Assert.AreEqual("images/foo.png", uri.AsString());
            Assert.IsNull(uri.MimeType);
        }

        [Test]
        public void DataUri()
        {
            using var uri = Deserialize($"\"data:application/octet-stream;base64,{k_FourBytePayload}\"");
            Assert.IsTrue(uri.IsData);
            Assert.IsFalse(uri.IsString);
            Assert.IsFalse(uri.IsFailed);
            Assert.AreEqual("application/octet-stream", uri.MimeType);
            Assert.IsTrue(uri.TryGetData(out var data));
            Assert.AreEqual(4, data.Length);
            Assert.AreEqual(0xAD, data[0]);
            Assert.AreEqual(0x06, data[1]);
            Assert.AreEqual(0xF0, data[2]);
            Assert.AreEqual(0x0D, data[3]);
            Assert.Throws<InvalidOperationException>(() => uri.AsString());
        }

        [Test]
        public void TryGetDataIsRepeatable()
        {
            using var uri = Deserialize($"\"data:application/octet-stream;base64,{k_FourBytePayload}\"");
            Assert.IsTrue(uri.TryGetData(out var first));
            Assert.IsTrue(uri.TryGetData(out var second), "TryGetData must succeed across multiple calls.");
            Assert.AreEqual(first.Length, second.Length);
            for (var i = 0; i < first.Length; i++)
            {
                Assert.AreEqual(first[i], second[i]);
            }
        }

        [Test]
        public void DataUriMissingDelimiter()
        {
            using var uri = Deserialize($"\"data:{k_FourBytePayload}\"");
            Assert.IsTrue(uri.IsFailed);
            Assert.IsFalse(uri.IsData);
            Assert.IsFalse(uri.IsString);
        }

        [Test]
        public void DataUriUnknownEncoding()
        {
            using var uri = Deserialize($"\"data:text/plain;base32,{k_FourBytePayload}\"");
            Assert.IsTrue(uri.IsFailed);
        }

        [Test]
        public void DataUriInvalidBase64()
        {
            using var uri = Deserialize("\"data:application/octet-stream;base64,rQbw}Q==\"");
            Assert.IsTrue(uri.IsFailed);
        }

        [Test]
        public void EscapedStringUri()
        {
            // JSON / is '/'.
            using var uri = Deserialize("\"images\\u002Ffoo.png\"");
            Assert.IsTrue(uri.IsString);
            Assert.AreEqual("images/foo.png", uri.AsString());
        }

        [Test]
        public void AsStringThrowsOnDataUri()
        {
            using var uri = Deserialize($"\"data:application/octet-stream;base64,{k_OneBytePayload}\"");
            Assert.Throws<InvalidOperationException>(() => uri.AsString());
        }

        [Test]
        public void DisposeIdempotent()
        {
            var uri = Deserialize($"\"data:application/octet-stream;base64,{k_OneBytePayload}\"");
            uri.Dispose();
            Assert.DoesNotThrow(() => uri.Dispose());
        }

        [Test]
        public void DisposeFreesData()
        {
            var uri = Deserialize($"\"data:application/octet-stream;base64,{k_FourBytePayload}\"");
            Assert.IsTrue(uri.IsData);
            uri.Dispose();
            // After dispose, TryGetData must report no data.
            Assert.IsFalse(uri.TryGetData(out _));
        }

        [Test]
        public void StringUriDisposeIsNoOp()
        {
            var uri = new UriValue("foo");
            Assert.DoesNotThrow(() => uri.Dispose());
            Assert.DoesNotThrow(() => uri.Dispose());
        }

        [Test]
        public void PendingDisposedOnDeserializeException()
        {
            // Property order matters: `uri` is parsed (and decoded) BEFORE `count` throws.
            var json = Encoding.UTF8.GetBytes(
                $"{{\"uri\":\"data:application/octet-stream;base64,{k_FourBytePayload}\",\"count\":\"not_a_number\"}}");

            UriValueConverter.BeginCollect();
            var threw = false;
            try
            {
                JsonSerializer.Deserialize<UriHolder>(json);
            }
            catch (JsonException)
            {
                threw = true;
            }
            var pending = UriValueConverter.EndCollect();

            Assert.IsTrue(threw, "Deserialize should have thrown on the bad `count` value.");
            Assert.IsNotNull(pending);
            Assert.AreEqual(1, pending.Count);
            var leaked = pending[0];
            Assert.IsTrue(leaked.IsData, "Decoded UriValue must still own its data before disposal.");
            leaked.Dispose();
            // After disposal the wrapper releases its data ownership.
            Assert.IsFalse(leaked.TryGetData(out _));
        }

        [Test]
        public void EndCollectReturnsNullWithoutBeginCollect()
        {
            Assert.IsNull(UriValueConverter.EndCollect());
        }

        [Test]
        public void PendingListCollectsAllDataUrisAcrossSchema()
        {
            var json = Encoding.UTF8.GetBytes(
                $"{{\"uri\":\"data:application/octet-stream;base64,{k_FourBytePayload}\",\"count\":7}}");

            UriValueConverter.BeginCollect();
            UriHolder holder = null;
            try
            {
                holder = JsonSerializer.Deserialize<UriHolder>(json);
            }
            finally
            {
                var pending = UriValueConverter.EndCollect();
                Assert.IsNotNull(pending);
                Assert.AreEqual(1, pending.Count);
                Assert.IsTrue(pending[0].IsData);
                Assert.AreSame(holder?.Uri, pending[0],
                    "Pending entry must be the same instance reachable via the deserialized graph.");
                foreach (var u in pending) u.Dispose();
            }
        }

        static UriValue Deserialize(string json)
        {
            var utf8 = Encoding.UTF8.GetBytes(json);
            return JsonSerializer.Deserialize<UriValue>(utf8);
        }
    }
}
