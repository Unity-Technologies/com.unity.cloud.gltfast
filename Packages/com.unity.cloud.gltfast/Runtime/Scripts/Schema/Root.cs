// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{
    /// <summary>
    /// The root object for a glTF asset.
    /// </summary>
    /// <seealso href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#reference-gltf"/>
    public class Root : IGltfObject
    {
        /// <summary>
        /// Names of glTF extensions used somewhere in this asset.
        /// </summary>
        [JsonPropertyName("extensionsUsed")]
        public string[] ExtensionsUsed { get; set; }

        /// <summary>
        /// Names of glTF extensions required to properly load this asset.
        /// </summary>
        [JsonPropertyName("extensionsRequired")]
        public string[] ExtensionsRequired { get; set; }

        /// <summary>
        /// An array of accessors. An accessor is a typed view into a bufferView.
        /// </summary>
        [JsonPropertyName("accessors")]
        public List<Accessor> Accessors { get; set; }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        [JsonPropertyName("animations")]
        public List<Animation> Animations { get; set; }
#endif

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        [JsonPropertyName("asset")]
        public Asset Asset { get; set; }

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        [JsonPropertyName("buffers")]
        public Buffer[] Buffers { get; set; }

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        [JsonPropertyName("bufferViews")]
        public List<BufferView> BufferViews { get; set; }

        /// <summary>
        /// An array of cameras. A camera defines a projection matrix.
        /// </summary>
        [JsonPropertyName("cameras")]
        public List<Camera> Cameras { get; set; }

        /// <summary>
        /// An array of images. An image defines data used to create a texture.
        /// </summary>
        [JsonPropertyName("images")]
        public List<Image> Images { get; set; }

        /// <summary>
        /// An array of materials. A material defines the appearance of a primitive.
        /// </summary>
        [JsonPropertyName("materials")]
        public List<Material> Materials { get; set; }

        /// <summary>
        /// An array of meshes. A mesh is a set of primitives to be rendered.
        /// </summary>
        [JsonPropertyName("meshes")]
        public List<Mesh> Meshes { get; set; }

        /// <summary>
        /// An array of nodes.
        /// </summary>
        [JsonPropertyName("nodes")]
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        [JsonPropertyName("samplers")]
        public List<Sampler> Samplers { get; set; }

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        [JsonPropertyName("scene")]
        public int Scene { get; set; } = -1;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        [JsonPropertyName("scenes")]
        public List<Scene> Scenes { get; set; }

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        [JsonPropertyName("skins")]
        public List<Skin> Skins { get; set; }

        /// <summary>
        /// An array of textures.
        /// </summary>
        [JsonPropertyName("textures")]
        public List<Texture> Textures { get; set; }

        /// <inheritdoc cref="RootExtensions"/>
        [JsonPropertyName("extensions")]
        public RootExtensions Extensions { get; set; }

        /// <summary>Application-specific data.</summary>
        /// <seealso href="https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#reference-extras"/>
        [JsonPropertyName("extras")]
        public UnclassifiedData Extras { get; set; }

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        public bool HasAnimation => Animations is { Count: > 0 };
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
            var bufferView = BufferViews[accessor.BufferView];
            if (bufferView.ByteStride < 0) return false;
            return bufferView.ByteStride > accessor.ElementByteSize;
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

            if (ExtensionsRequired != null)
            {
                writer.AddArrayProperty("extensionsRequired", ExtensionsRequired);
            }

            if (ExtensionsUsed != null)
            {
                writer.AddArrayProperty("extensionsUsed", ExtensionsUsed);
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
            if (Scene >= 0)
            {
                writer.AddProperty("scene", Scene);
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
        public int MaterialsVariantsCount => Extensions?.MaterialsVariants?.Variants?.Count ?? 0;

        /// <summary>
        /// Gets the name of a specific materials variant.
        /// </summary>
        /// <param name="index">Materials variant index.</param>
        /// <returns>Name of a materials variant.</returns>
        /// <seealso href="https://github.com/KhronosGroup/glTF/tree/main/extensions/2.0/Khronos/KHR_materials_variants"/>
        public string GetMaterialsVariantName(int index)
        {
            var variants = Extensions?.MaterialsVariants?.Variants;
            if (variants != null && index >= 0 && index < variants.Count)
            {
                return variants[index].Name;
            }

            return null;
        }
    }
}
