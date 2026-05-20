// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace GLTFast.Schema
{

    /// <inheritdoc />
    [Serializable]
    public class Root : RootBase<
        Accessor,
        Animation,
        Asset,
        Buffer,
        BufferView,
        Camera,
        RootExtensions,
        Image,
        Material,
        Mesh,
        Node,
        Sampler,
        Scene,
        Skin,
        Texture
    >
    { }

    /// <inheritdoc />
    /// <typeparam name="TAccessor">Accessor type</typeparam>
    /// <typeparam name="TAnimation">Animation type</typeparam>
    /// <typeparam name="TAsset">Asset type</typeparam>
    /// <typeparam name="TBuffer">Buffer type</typeparam>
    /// <typeparam name="TBufferView">BufferView type</typeparam>
    /// <typeparam name="TCamera">Camera type</typeparam>
    /// <typeparam name="TExtensions">Extensions type</typeparam>
    /// <typeparam name="TImage">Image type</typeparam>
    /// <typeparam name="TMaterial">Material type</typeparam>
    /// <typeparam name="TMesh">Mesh type</typeparam>
    /// <typeparam name="TNode">Node type</typeparam>
    /// <typeparam name="TSampler">Sampler type</typeparam>
    /// <typeparam name="TScene">Scene type</typeparam>
    /// <typeparam name="TSkin">Skin type</typeparam>
    /// <typeparam name="TTexture">Texture type</typeparam>
    [Serializable]
    public abstract class RootBase<
        TAccessor,
        TAnimation,
        TAsset,
        TBuffer,
        TBufferView,
        TCamera,
        TExtensions,
        TImage,
        TMaterial,
        TMesh,
        TNode,
        TSampler,
        TScene,
        TSkin,
        TTexture
    > : RootBase
        where TAccessor : AccessorBase
        where TAnimation : AnimationBase
        where TAsset : Asset
        where TBuffer : Buffer
        where TBufferView : BufferViewBase
        where TCamera : CameraBase
        where TExtensions : RootExtensions
        where TImage : Image
        where TMaterial : MaterialBase
        where TMesh : MeshBase
        where TNode : NodeBase
        where TSampler : Sampler
        where TScene : Scene
        where TSkin : Skin
        where TTexture : TextureBase
    {
        /// <inheritdoc cref="Accessors"/>
        public TAccessor[] accessors;

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <inheritdoc cref="Animations"/>
        public TAnimation[] animations;
#endif

        /// <inheritdoc cref="Asset"/>
        public TAsset asset;

        /// <inheritdoc cref="Buffer"/>
        public TBuffer[] buffers;

        /// <inheritdoc cref="BufferView"/>
        public TBufferView[] bufferViews;

        /// <inheritdoc cref="Camera"/>
        public TCamera[] cameras;

        /// <inheritdoc cref="Image"/>
        public TImage[] images;

        /// <inheritdoc cref="Material"/>
        public TMaterial[] materials;

        /// <inheritdoc cref="Node"/>
        public TNode[] nodes;

        /// <inheritdoc cref="Sampler"/>
        public TSampler[] samplers;

        /// <inheritdoc cref="Scene"/>
        public TScene[] scenes;

        /// <inheritdoc cref="Skin"/>
        public TSkin[] skins;

        /// <inheritdoc cref="Texture"/>
        public TTexture[] textures;

        /// <inheritdoc cref="RootExtensions"/>
        public TExtensions extensions;

        /// <inheritdoc cref="Meshes"/>
        public TMesh[] meshes;

        /// <inheritdoc />
        public override IReadOnlyList<AccessorBase> Accessors => accessors;

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <inheritdoc />
        public override IReadOnlyList<AnimationBase> Animations => animations;
#endif

        /// <inheritdoc />
        public override Asset Asset => asset;

        /// <inheritdoc />
        public override IReadOnlyList<Buffer> Buffers => buffers;

        /// <inheritdoc />
        public override IReadOnlyList<BufferViewBase> BufferViews => bufferViews;

        /// <inheritdoc />
        public override IReadOnlyList<CameraBase> Cameras => cameras;

        /// <inheritdoc />
        public override IReadOnlyList<Image> Images => images;

        /// <inheritdoc />
        public override IReadOnlyList<MaterialBase> Materials => materials;

        /// <inheritdoc />
        public override IReadOnlyList<NodeBase> Nodes => nodes;

        /// <inheritdoc />
        public override IReadOnlyList<Sampler> Samplers => samplers;

        /// <inheritdoc />
        public override IReadOnlyList<Scene> Scenes => scenes;

        /// <inheritdoc />
        public override IReadOnlyList<Skin> Skins => skins;

        /// <inheritdoc />
        public override IReadOnlyList<TextureBase> Textures => textures;

        /// <inheritdoc />
        public override RootExtensions Extensions => extensions;

        /// <inheritdoc />
        public override IReadOnlyList<MeshBase> Meshes => meshes;
    }

    /// <summary>
    /// The root object for a glTF asset.
    /// </summary>
    /// <seealso href="https://www.khronos.org/registry/glTF/specs/2.0/glTF-2.0.html#reference-gltf"/>
    [Serializable]
    public abstract class RootBase : IGltfObject
    {
        /// <summary>
        /// Names of glTF extensions used somewhere in this asset.
        /// </summary>
        public string[] extensionsUsed;

        /// <summary>
        /// Names of glTF extensions required to properly load this asset.
        /// </summary>
        public string[] extensionsRequired;

        /// <summary>
        /// An array of accessors. An accessor is a typed view into a bufferView.
        /// </summary>
        public abstract IReadOnlyList<AccessorBase> Accessors { get; }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        /// <summary>
        /// An array of keyframe animations.
        /// </summary>
        public abstract IReadOnlyList<AnimationBase> Animations { get; }
#endif

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        public abstract Asset Asset { get; }

        /// <summary>
        /// An array of buffers. A buffer points to binary geometry, animation, or skins.
        /// </summary>
        public abstract IReadOnlyList<Buffer> Buffers { get; }

        /// <summary>
        /// An array of bufferViews.
        /// A bufferView is a view into a buffer generally representing a subset of the buffer.
        /// </summary>
        public abstract IReadOnlyList<BufferViewBase> BufferViews { get; }

        /// <summary>
        /// An array of cameras. A camera defines a projection matrix.
        /// </summary>
        public abstract IReadOnlyList<CameraBase> Cameras { get; }

        /// <summary>
        /// An array of images. An image defines data used to create a texture.
        /// </summary>
        public abstract IReadOnlyList<Image> Images { get; }

        /// <summary>
        /// An array of materials. A material defines the appearance of a primitive.
        /// </summary>
        public abstract IReadOnlyList<MaterialBase> Materials { get; }

        /// <summary>
        /// An array of meshes. A mesh is a set of primitives to be rendered.
        /// </summary>
        public abstract IReadOnlyList<MeshBase> Meshes { get; }

        /// <summary>
        /// An array of nodes.
        /// </summary>
        public abstract IReadOnlyList<NodeBase> Nodes { get; }

        /// <summary>
        /// An array of samplers. A sampler contains properties for texture filtering and wrapping modes.
        /// </summary>
        public abstract IReadOnlyList<Sampler> Samplers { get; }

        /// <summary>
        /// The index of the default scene.
        /// </summary>
        public int scene = -1;

        /// <summary>
        /// An array of scenes.
        /// </summary>
        public abstract IReadOnlyList<Scene> Scenes { get; }

        /// <summary>
        /// An array of skins. A skin is defined by joints and matrices.
        /// </summary>
        public abstract IReadOnlyList<Skin> Skins { get; }

        /// <summary>
        /// An array of textures.
        /// </summary>
        public abstract IReadOnlyList<TextureBase> Textures { get; }

        /// <inheritdoc cref="RootExtensions"/>
        public abstract RootExtensions Extensions { get; }

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
        /// Has been used to clean up invalid parsing artifacts created by JsonUtility.
        /// </summary>
        [Obsolete("Has become obsolete after the transition from JsonUtility to System.Text.Json.")]
        public virtual void JsonUtilityCleanup() { }

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
