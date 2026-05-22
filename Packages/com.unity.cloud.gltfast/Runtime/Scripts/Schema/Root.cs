// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif

using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace GLTFast.Schema
{
    /// <summary>
    /// The root object for a glTF asset.
    /// </summary>
    /// <seealso href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#reference-gltf"/>
    [Serializable]
    public class Root : IGltfObject
    {
        /// <summary>
        /// Names of glTF extensions used somewhere in this asset.
        /// </summary>
        public string[] extensionsUsed;

        /// <summary>
        /// Names of glTF extensions required to properly load this asset.
        /// </summary>
        public string[] extensionsRequired;

        /// <inheritdoc cref="Accessors"/>
        public Accessor[] accessors;

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <inheritdoc cref="Animations"/>
        public Animation[] animations;
#endif

        /// <inheritdoc cref="Asset"/>
        public Asset asset;

        /// <inheritdoc cref="Buffer"/>
        public Buffer[] buffers;

        /// <inheritdoc cref="BufferView"/>
        public BufferView[] bufferViews;

        /// <inheritdoc cref="Camera"/>
        public Camera[] cameras;

        /// <inheritdoc cref="Image"/>
        public Image[] images;

        /// <inheritdoc cref="Material"/>
        public Material[] materials;

        /// <inheritdoc cref="Node"/>
        public Node[] nodes;

        /// <inheritdoc cref="Sampler"/>
        public Sampler[] samplers;

        /// <inheritdoc cref="Scene"/>
        public Scene[] scenes;

        /// <inheritdoc cref="Skin"/>
        public Skin[] skins;

        /// <inheritdoc cref="Texture"/>
        public Texture[] textures;

        /// <inheritdoc cref="RootExtensions"/>
        public RootExtensions extensions;

        /// <inheritdoc cref="Meshes"/>
        public Mesh[] meshes;

        /// <summary>
        /// An array of accessors. An accessor is a typed view into a bufferView.
        /// </summary>
        public IReadOnlyList<Accessor> Accessors => accessors;

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        public IReadOnlyList<Animation> Animations => animations;
#endif

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        public Asset Asset => asset;

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        public IReadOnlyList<Buffer> Buffers => buffers;

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        public IReadOnlyList<BufferView> BufferViews => bufferViews;

        /// <summary>
        /// An array of cameras. A camera defines a projection matrix.
        /// </summary>
        public IReadOnlyList<Camera> Cameras => cameras;

        /// <summary>
        /// An array of images. An image defines data used to create a texture.
        /// </summary>
        public IReadOnlyList<Image> Images => images;

        /// <summary>
        /// An array of materials. A material defines the appearance of a primitive.
        /// </summary>
        public IReadOnlyList<Material> Materials => materials;

        /// <summary>
        /// An array of meshes. A mesh is a set of primitives to be rendered.
        /// </summary>
        public IReadOnlyList<Mesh> Meshes => meshes;

        /// <summary>
        /// An array of nodes.
        /// </summary>
        public IReadOnlyList<Node> Nodes => nodes;

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        public IReadOnlyList<Sampler> Samplers => samplers;

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        public int scene = -1;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        public IReadOnlyList<Scene> Scenes => scenes;

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        public IReadOnlyList<Skin> Skins => skins;

        /// <summary>
        /// An array of textures.
        /// </summary>
        public IReadOnlyList<Texture> Textures => textures;

        /// <inheritdoc cref="RootExtensions"/>
        public RootExtensions Extensions => extensions;

        /// <summary>Application-specific data.</summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras"/>
        public UnclassifiedData extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        public bool HasAnimation => Animations != null && Animations.Count > 0;
#endif // UNITY_ANIMATION || GLTFAST_ANIMATION

        /// <summary>
        /// Looks up if a certain accessor points to interleaved data.
        /// </summary>
        /// <param name="accessorIndex">Accessor index</param>
        /// <returns>True if accessor is interleaved, false if its data is
        /// continuous.</returns>
        public bool IsAccessorInterleaved(int accessorIndex)
        {
            var accessor = Accessors[accessorIndex];
            var bufferView = BufferViews[accessor.bufferView];
            if (bufferView.byteStride < 0) return false;
            return bufferView.byteStride > accessor.ElementByteSize;
        }

        /// <summary>
        /// Serialization to JSON
        /// </summary>
        /// <param name="stream">Stream the JSON string is being written to.</param>
        public void GltfSerialize(StreamWriter stream)
        {
            var writer = new JsonWriter(stream);

            if (Asset != null)
            {
                writer.AddProperty("asset");
                Asset.GltfSerialize(writer);
            }
            if (Nodes != null)
            {
                writer.AddArray("nodes");
                foreach (var node in Nodes)
                {
                    node.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (extensionsRequired != null)
            {
                writer.AddArrayProperty("extensionsRequired", extensionsRequired);
            }

            if (extensionsUsed != null)
            {
                writer.AddArrayProperty("extensionsUsed", extensionsUsed);
            }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
            if (Animations != null)
            {
                writer.AddArray("animations");
                foreach (var animation in Animations)
                {
                    animation.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
#endif

            if (Buffers != null)
            {
                writer.AddArray("buffers");
                foreach (var buffer in Buffers)
                {
                    buffer.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (BufferViews != null)
            {
                writer.AddArray("bufferViews");
                foreach (var bufferView in BufferViews)
                {
                    bufferView.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (Accessors != null)
            {
                writer.AddArray("accessors");
                foreach (var accessor in Accessors)
                {
                    accessor.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (Cameras != null)
            {
                writer.AddArray("cameras");
                foreach (var camera in Cameras)
                {
                    camera.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (Images != null)
            {
                writer.AddArray("images");
                foreach (var image in Images)
                {
                    image?.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (Materials != null)
            {
                writer.AddArray("materials");
                foreach (var material in Materials)
                {
                    material.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (Meshes != null)
            {
                writer.AddArray("meshes");
                foreach (var mesh in Meshes)
                {
                    mesh.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (Samplers != null)
            {
                writer.AddArray("samplers");
                foreach (var sampler in Samplers)
                {
                    sampler.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (scene >= 0)
            {
                writer.AddProperty("scene", scene);
            }
            if (Scenes != null)
            {
                writer.AddArray("scenes");
                foreach (var sceneToSerialize in Scenes)
                {
                    sceneToSerialize.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (Skins != null)
            {
                writer.AddArray("skins");
                foreach (var skin in Skins)
                {
                    skin.GltfSerialize(writer);
                }
                writer.CloseArray();
            }
            if (Textures != null)
            {
                writer.AddArray("textures");
                foreach (var texture in Textures)
                {
                    texture.GltfSerialize(writer);
                }
                writer.CloseArray();
            }

            if (Extensions != null)
            {
                writer.AddProperty("extensions");
                Extensions.GltfSerialize(writer);
            }

            writer.Close();
        }

        /// <summary>
        /// Number of materials variants.
        /// </summary>
        /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants"/>
        public int MaterialsVariantsCount => Extensions?.KHR_materials_variants?.variants?.Count ?? 0;

        /// <summary>
        /// Gets the name of a specific materials variant.
        /// </summary>
        /// <param name="index">Materials variant index.</param>
        /// <returns>Name of a materials variant.</returns>
        /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants"/>
        public string GetMaterialsVariantName(int index)
        {
            var variants = Extensions?.KHR_materials_variants?.variants;
            if (variants != null && index >= 0 && index < variants.Count)
            {
                return variants[index].name;
            }

            return null;
        }
    }
}
