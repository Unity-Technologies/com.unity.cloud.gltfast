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
        public async Task<IDownload> RequestAsync(Uri url)
        {
            var dependency = new GltfAssetDependency
            {
                originalUri = url.OriginalString
            };
            assetDependencies.Add(dependency);
            var req = new SyncFileLoader(url);
            return req;
        }

        public async Task<ITextureDownload> RequestTextureAsync(Uri url, bool nonReadable)
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

    class SyncFileLoader : IDownload
    {
        ReadOnlyNativeArrayFromManagedArray<byte> m_ManagedNativeArray;
        byte[] m_FileBytes;

        public SyncFileLoader(Uri url)
        {
            var path = url.OriginalString;
            if (File.Exists(path))
            {
                m_FileBytes = File.ReadAllBytes(path);
                // TODO: Is there a better way to load a file into a NativeArray, like AsyncReadManager?
                m_ManagedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(m_FileBytes);
                Data = m_ManagedNativeArray.Array.AsNativeArrayReadOnly();
            }
            else
            {
                Error = $"Cannot find resource at path {path}";
            }
        }

        public object Current => null;
        public bool MoveNext() { return false; }
        public void Reset() { }

        public virtual bool Success => m_FileBytes != null;

        public string Error { get; protected set; }

        public NativeArray<byte>.ReadOnly Data { get; private set; }

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
                m_FileBytes = null;
                Data = default;
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
