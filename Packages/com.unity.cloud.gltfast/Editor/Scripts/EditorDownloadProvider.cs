// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Unity.Cloud.Gltfast.Editor
{

    using Loading;

    class EditorDownloadProvider : IDownloadProvider
    {

        public List<GltfAssetDependency> assetDependencies = new List<GltfAssetDependency>();

#pragma warning disable 1998
        public async Task<IDownload> Request(Uri url)
        {
            var dependency = new GltfAssetDependency
            {
                originalUri = url.OriginalString
            };
            assetDependencies.Add(dependency);
            var req = new SyncFileLoader(url);
            return req;
        }

        public async Task<ITextureDownload> RequestTexture(Uri url, bool nonReadable)
        {
            var dependency = new GltfAssetDependency
            {
                originalUri = url.OriginalString,
                type = GltfAssetDependency.Type.Texture
            };
            assetDependencies.Add(dependency);
            var req = new SyncTextureLoader(url);
            return req;
        }
#pragma warning restore 1998
    }

    class SyncFileLoader : IDownload, INativeDownload
    {
        ReadOnlyNativeArrayFromManagedArray<byte> m_ManagedNativeArray;

        public SyncFileLoader(Uri url)
        {
            var path = url.OriginalString;
            if (File.Exists(path))
            {
                Data = File.ReadAllBytes(path);
                // TODO: Is there a better way to load a file into a NativeArray, like AsyncReadManager?
                m_ManagedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(Data);
                NativeData = m_ManagedNativeArray.Array.AsNativeArrayReadOnly();
            }
            else
            {
                Error = $"Cannot find resource at path {path}";
            }
        }

        public object Current => null;
        public bool MoveNext() { return false; }
        public void Reset() { }

        public virtual bool Success => Data != null;

        public string Error { get; protected set; }
        public byte[] Data { get; private set; }

        public NativeArray<byte>.ReadOnly NativeData { get; private set; }

        public string Text => Data != null ? System.Text.Encoding.UTF8.GetString(Data) : null;

        public bool? IsBinary
        {
            get
            {
                if (Success)
                {
                    return GltfGlobals.IsGltfBinary(NativeData);
                }
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_ManagedNativeArray?.Dispose();
                m_ManagedNativeArray = null;
                Data = null;
                NativeData = default;
            }
        }
    }

    sealed class SyncTextureLoader : SyncFileLoader, ITextureDownload
    {

        public Texture2D Texture { get; private set; }

        public override bool Success => Texture != null;

        public SyncTextureLoader(Uri url)
            : base(url)
        {
            Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(url.OriginalString);
            if (Texture == null)
            {
                Error = $"Couldn't load texture at {url.OriginalString}";
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Texture = null;
        }
    }
}
