// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if !UNITY_WEBGL || UNITY_EDITOR
// Depicts whether managed scripting threads are available.
#define GLTFAST_THREADS
#endif

#if KTX_IS_RECENT
#define KTX_IS_ENABLED
#elif KTX_IS_INSTALLED
#error You have to update the *KTX for Unity* package in package manager to enable support for KTX textures in *glTFast*.
#endif

#if DRACO_IS_RECENT
#define DRACO_IS_ENABLED
#elif DRACO_IS_INSTALLED
#error You have to update the *Draco for Unity* package in package manager to enable support for decompressing Draco meshes in *glTFast*.
#endif

#if MESHOPT_IS_RECENT
#define MESHOPT_IS_ENABLED
#elif MESHOPT_IS_INSTALLED && !GLTFAST_IGNORE_MESHOPT_OUTDATED_ERROR
#error You have to update the <a href="https://docs.unity3d.com/Packages/com.unity.meshopt.decompress@latest/">meshoptimizer mesh compression for Unity package</a> in package manager to enable support for decoding meshoptimizer compressed buffer views in glTFast. Add "GLTFAST_IGNORE_MESHOPT_OUTDATED_ERROR" to your project's scripting define symbols to temporarily suppress this error.
#endif

// #define MEASURE_TIMINGS

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text;

using Unity.Gltfast.Text.Json;
using GLTFast.Addons;
using GLTFast.Animations;
using GLTFast.Jobs;
using GLTFast.Loading;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
#if MESHOPT_IS_ENABLED
using Meshoptimizer;
#endif
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine;
using Buffer = GLTFast.Schema.Buffer;
using Camera = GLTFast.Schema.Camera;
using Debug = UnityEngine.Debug;
using Material = GLTFast.Schema.Material;
using Mesh = GLTFast.Schema.Mesh;
using Sampler = GLTFast.Schema.Sampler;
using Texture = GLTFast.Schema.Texture;

namespace GLTFast
{
    /// <summary>
    /// Loads a glTF's content, converts it to Unity resources and is able to
    /// feed it to an <see cref="IInstantiator"/> for instantiation.
    /// </summary>
    public class GltfImport : IGltfReadable, IGltfBuffers, IGltfAccessors, IDisposable
    {
        /// <summary>
        /// Default value for a C# Job's innerloopBatchCount parameter.
        /// </summary>
        /// <seealso cref="IJobParallelForExtensions.Schedule&lt;T&gt;(T,int,int,JobHandle)"/>
        internal const int DefaultBatchCount = 512;

        /// <summary>
        /// JSON parse speed in bytes per second
        /// Measurements based on a MacBook Pro Intel(R) Core(TM) i9-9980HK CPU @ 2.40GHz
        /// and reduced by ~ 20%
        /// </summary>
        const int k_JsonParseSpeed =
#if UNITY_EDITOR
            45_000_000;
#else
            80_000_000;
#endif

        /// <summary>Anticipated memory copy speed in bytes per second</summary>
        const uint k_MemCopySpeed =
#if UNITY_EDITOR
            1_500_000_000;
#else
            3_000_000_000;
#endif

        /// <summary>
        /// A buffer size of 81920 bytes (System.IO.Pipeline's default) seems to be a good trade-off between
        /// throughput and managed memory allocation.
        /// </summary>
        const uint k_CopyBufferSize = 81_920;

        const string k_PrimitiveName = "Primitive";

        static readonly HashSet<Extension> k_SupportedExtensions = new HashSet<Extension> {
#if DRACO_IS_ENABLED
            Extension.DracoMeshCompression,
#endif
#if KTX_IS_ENABLED
            Extension.TextureBasisUniversal,
#endif // KTX_IS_ENABLED
#if MESHOPT_IS_ENABLED
            Extension.MeshoptCompression,
#endif
            Extension.MaterialsPbrSpecularGlossiness,
            Extension.MaterialsUnlit,
            Extension.MaterialsVariants,
            Extension.TextureTransform,
            Extension.MeshQuantization,
            Extension.MaterialsTransmission,
            Extension.MeshGPUInstancing,
            Extension.LightsPunctual,
            Extension.MaterialsClearcoat,
        };

        static IDeferAgent s_DefaultDeferAgent;
        static MeshComparer s_MeshComparer = new();

        /// <summary>Logger used by this glTF import instance.</summary>
        public ICodeLogger Logger => m_Context.Logger;

        /// <summary>Defer agent used by this glTF import instance.</summary>
        public IDeferAgent DeferAgent => m_Context.DeferAgent;

        ImportContext m_Context;

        IMaterialGenerator m_MaterialGenerator;

        ImportAddonInstanceCollection m_Addons;

        ImportSettings m_Settings;

        ReadOnlyNativeArray<byte>[] m_Buffers;
        List<IDisposable> m_VolatileDisposables;

        GlbBinChunk[] m_BinChunks;

        Dictionary<int, Task<bool>> m_BufferLoadTasks;
        Dictionary<int, Task<ImageResult>> m_TextureLoadTasks;

        IDisposable[] m_AccessorData;
        AccessorUsage[] m_AccessorUsage;
        JobHandle m_AccessorJobsHandle;

        List<MeshOrder> m_MeshOrders;

        /// <summary>
        /// In glTF a texture is an image with a certain sampler setting applied.
        /// So any `images` member is also in `textures`, but not necessary the
        /// other way around.
        /// </summary>
        Texture2D[] m_Textures;

        /// <summary>Key: texture index; Value: index of original texture to re-use.</summary>
        Dictionary<int, int> m_TextureReuseMap;
        /// <summary>Key: texture index; Value: index of original texture to instantiate off.</summary>
        Dictionary<int, int> m_TextureResampleMap;
        /// <summary>Key: texture index; Value: new image index</summary>
        Dictionary<int, int> m_TextureImageOverrides;
        HashSet<int> m_NonFlippedYTextureIndices;

        /// optional glTF-binary buffer
        /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#binary-buffer
        GlbBinChunk? m_GlbBinChunk;

#if MESHOPT_IS_ENABLED
        Dictionary<int, NativeArray<byte>> m_MeshoptBufferViews;
        NativeArray<int> m_MeshoptReturnValues;
        JobHandle m_MeshoptJobHandle;
#endif

        /// <summary>
        /// Material IDs of materials that require points topology support.
        /// </summary>
        HashSet<int> m_MaterialPointsSupport;
        bool m_DefaultMaterialPointsSupport;

        /// <summary>
        /// The base URI complements relative URIs of external buffers or images.
        /// </summary>
        Uri BaseUri { get; set; }

        /// <summary>Main glTF data structure</summary>
        public Root Root { get; protected set; }

        UnityEngine.Material[] m_Materials;
        List<UnityEngine.Object> m_Resources;

        /// <summary>
        /// Unity's animation system addresses target GameObjects by hierarchical name.
        /// To make sure names are consistent and have no conflicts they are precalculated
        /// and stored in this array.
        /// </summary>
        string[] m_NodeNames;

        List<UnityEngine.Mesh> m_Meshes;
        FlatArray<MeshAssignment> m_MeshAssignments;

        Matrix4x4[][] m_SkinsInverseBindMatrices;

        List<IDataInstanceApplierFactory> m_DataInstanceApplierFactories;

#if UNITY_EDITOR
        /// <summary>
        /// Required for Editor import only to preserve default/fallback materials
        /// </summary>
        public UnityEngine.Material defaultMaterial;
#endif

        /// <summary>
        /// True, when loading has finished and glTF can be instantiated
        /// </summary>
        public bool LoadingDone { get; private set; }

        /// <summary>
        /// True if an error happened during glTF loading
        /// </summary>
        public bool LoadingError { get; private set; }

        /// <summary>
        /// Constructs a GltfImport instance with injectable customization objects.
        /// </summary>
        /// <param name="downloadProvider">Provides file access or download customization</param>
        /// <param name="deferAgent">Provides custom update loop behavior for better frame rate control</param>
        /// <param name="materialGenerator">Provides custom glTF to Unity material conversion</param>
        /// <param name="logger">Custom logger for reporting messages. Defaults to the shared <see cref="GLTFast.Logging.ConsoleLogger.Instance"/> (writes to Unity's Console) when <c>null</c> is passed. Pass <see cref="GLTFast.Logging.NullLogger.Instance"/> (or <c>new NullLogger()</c>) to suppress all output.</param>
        public GltfImport(
            IDownloadProvider downloadProvider = null,
            IDeferAgent deferAgent = null,
            IMaterialGenerator materialGenerator = null,
            ICodeLogger logger = null
            )
        {
            if (deferAgent == null)
            {
                if (s_DefaultDeferAgent == null
                    || (s_DefaultDeferAgent is UnityEngine.Object agent && agent == null) // Cast to Object to enforce Unity Object's null check (is MonoBehavior alive?)
                    )
                {
                    var defaultDeferAgentGameObject = new GameObject("glTF-StableFramerate");
                    // Keep it across scene loads
                    UnityEngine.Object.DontDestroyOnLoad(defaultDeferAgentGameObject);
                    SetDefaultDeferAgent(defaultDeferAgentGameObject.AddComponent<TimeBudgetPerFrameDeferAgent>());
                    // Adding a DefaultDeferAgent component will make it un-register via <see cref="UnsetDefaultDeferAgent"/>
                    defaultDeferAgentGameObject.AddComponent<DefaultDeferAgent>();
                }
                deferAgent = s_DefaultDeferAgent;
            }
            m_MaterialGenerator = materialGenerator ?? MaterialGenerator.GetDefaultMaterialGenerator();

            m_Context = new ImportContext(
                downloadProvider ?? new DefaultDownloadProvider(),
                logger is NullLogger ? null : (logger ?? ConsoleLogger.Instance),
                deferAgent
                );

            ImportAddonRegistry.InjectAllAddons(this);
        }

        /// <summary>
        /// Sets the default <see cref="IDeferAgent"/> for subsequently
        /// generated GltfImport instances.
        /// </summary>
        /// <param name="deferAgent">New default <see cref="IDeferAgent"/></param>
        public static void SetDefaultDeferAgent(IDeferAgent deferAgent)
        {
#if DEBUG
            if (s_DefaultDeferAgent != null && s_DefaultDeferAgent != deferAgent)
            {
                Debug.LogWarning("GltfImport.defaultDeferAgent got overruled! Make sure there is only one default at any time", deferAgent as UnityEngine.Object);
            }
#endif
            s_DefaultDeferAgent = deferAgent;
        }

        /// <summary>
        /// Allows un-registering default <see cref="IDeferAgent"/>.
        /// For example if it's no longer available.
        /// </summary>
        /// <param name="deferAgent"><see cref="IDeferAgent"/> in question</param>
        public static void UnsetDefaultDeferAgent(IDeferAgent deferAgent)
        {
            if (s_DefaultDeferAgent == deferAgent)
            {
                s_DefaultDeferAgent = null;
            }
        }

        /// <summary>
        /// Adds an import add-on instance. To be called before any loading is initiated.
        /// </summary>
        /// <param name="importInstance">The import instance to add.</param>
        /// <typeparam name="T">Type of the import instance</typeparam>
        public void AddImportAddonInstance<T>(T importInstance) where T : ImportAddonInstance
        {
            m_Addons ??= new ImportAddonInstanceCollection();
            m_Addons.Add(importInstance);
        }

        /// <summary>
        /// Queries the import instance of a particular type.
        /// </summary>
        /// <typeparam name="T">Type of the import instance</typeparam>
        /// <returns>The import instance that was previously added. False if there was none.</returns>
        public T GetImportAddonInstance<T>() where T : ImportAddonInstance
        {
            return m_Addons?.Get<T>();
        }

        /// <summary>
        /// Load a glTF file (JSON or binary)
        /// The URL can be a file path (using the "file://" scheme) or a web address.
        /// </summary>
        /// <param name="url">Uniform Resource Locator. Can be a file path (using the "file://" scheme) or a web address.</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> Load(
            string url,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            return await Load(new Uri(url, UriKind.RelativeOrAbsolute), importSettings, cancellationToken);
        }

        /// <summary>
        /// Load a glTF file (JSON or binary)
        /// The URL can be a file path (using the "file://" scheme) or a web address.
        /// </summary>
        /// <param name="url">Uniform Resource Locator. Can be a file path (using the "file://" scheme) or a web address.</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> Load(
            Uri url,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            m_Settings = importSettings ?? new ImportSettings();
            return await LoadFromUri(url, cancellationToken);
        }

        /// <summary>
        /// Loads a glTF from a byte array.
        /// </summary>
        /// <param name="data">Either glTF-Binary data or a UTF-8 encoded glTF JSON</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> Load(
            byte[] data,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
        )
        {
            var managedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(data);
            m_VolatileDisposables ??= new List<IDisposable>();
            m_VolatileDisposables.Add(managedNativeArray);
            return await Load(
                managedNativeArray.Array.AsNativeArrayReadOnly(),
                uri,
                importSettings,
                cancellationToken
                );
        }

        /// <summary>
        /// Loads a glTF from a NativeArray.
        /// </summary>
        /// <param name="data">Either glTF-Binary data or a UTF-8 encoded glTF JSON</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> Load(
            NativeArray<byte>.ReadOnly data,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            if (GltfGlobals.IsGltfBinary(data))
            {
                return await LoadGltfBinaryInternal(data, uri, importSettings, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            // Fallback interpreting data as string
            // TODO: ToArray does another, slow memcpy! Find a better solution.
            var json = Encoding.UTF8.GetString(data.ToArray(), 0, data.Length);
            return await LoadGltfJson(json, uri, importSettings, cancellationToken);
        }

        /// <summary>
        /// Load glTF from a local file path.
        /// </summary>
        /// <param name="localPath">Local path to glTF or glTF-Binary file.</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> LoadFile(
            string localPath,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            await using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            var result = await LoadStream(fs, uri, importSettings, cancellationToken);
            return result;
        }

        /// <summary>
        /// Load glTF from a stream.
        /// </summary>
        /// <param name="stream">Stream of the glTF or glTF-Binary</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> LoadStream(
            Stream stream,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default)
        {
            if (!stream.CanRead)
            {
                Logger?.Error(LogCode.StreamError, "Not readable");
                return false;
            }

            var initialStreamPosition = stream.CanSeek
                ? stream.Position
                : -1L;

            var firstBytes = new byte[4];
            if (!await stream.ReadToArrayAsync(firstBytes, 0, firstBytes.Length, cancellationToken))
            {
                Logger?.Error(LogCode.StreamError, "First bytes");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            if (GltfGlobals.IsGltfBinary(firstBytes))
            {
                // Read the rest of the header
                var glbHeader = new byte[8];
                if (!await stream.ReadToArrayAsync(glbHeader, 0, glbHeader.Length, cancellationToken))
                {
                    Logger?.Error(LogCode.StreamError, "glb header");
                    return false;
                }
                // Length of the entire glTF, including the header
                var length = BitConverter.ToUInt32(glbHeader, 4);
                if (length >= int.MaxValue)
                {
                    // glTF-binary supports up to 2^32 = 4GB, but C# arrays have a 2^31 (2GB) limit.
                    Logger?.Error("glb exceeds 2GB limit.");
                    return false;
                }
                if (length > stream.Length)
                {
                    Logger?.Error(LogCode.UnexpectedEndOfContent);
                    return false;
                }
                using var data = new NativeArray<byte>(
                    (int)length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                using var manager = new NativeMemoryManager<byte>(data);
                var mem = manager.Memory;
                firstBytes.CopyTo(mem);
                mem = mem[firstBytes.Length..];
                glbHeader.CopyTo(mem);
                mem = mem[glbHeader.Length..];

#if GLTFAST_THREADS
                var predictedTime = length / (float)k_MemCopySpeed;
                if (DeferAgent.ShouldDefer(predictedTime))
                {
                    try
                    {
                        await Task.Run(() => CopyStreamToMemory(stream, mem), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    }
                }
                else
#endif // GLTFAST_THREADS
                {
                    await CopyStreamToMemoryAsync(stream, mem);
                }

                var result = await LoadGltfBinaryInternal(data.AsReadOnly(), uri, importSettings, cancellationToken);
                return result;
            }
            var reader = new StreamReader(stream);
            string json;
            if (stream.CanSeek)
            {
                stream.Seek(initialStreamPosition, SeekOrigin.Begin);
                json = await reader.ReadToEndAsync();
            }
            else
            {
                // TODO: String concat likely leads to another copy in memory and bad performance.
                json = Encoding.UTF8.GetString(firstBytes) + await reader.ReadToEndAsync();
            }

            reader.Dispose();

            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            return await LoadGltfJson(json, uri, importSettings, cancellationToken);
        }

        /// <summary>
        /// Load a glTF-binary asset from a byte array.
        /// </summary>
        /// <remarks>Obsolete! Use the generic
        /// <see cref="Load(byte[],Uri,GLTFast.ImportSettings,System.Threading.CancellationToken)"/> instead.</remarks>
        /// <param name="bytes">byte array containing glTF-binary</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        [Obsolete("Use the generic Load instead.")]
        public async Task<bool> LoadGltfBinary(
            byte[] bytes,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
        )
        {
            var managedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(bytes);
            m_VolatileDisposables ??= new List<IDisposable>();
            m_VolatileDisposables.Add(managedNativeArray);
            return await LoadGltfBinaryInternal(
                managedNativeArray.Array.AsNativeArrayReadOnly(),
                uri,
                importSettings,
                cancellationToken
                );
        }

        /// <summary>
        /// Load a glTF JSON from a string
        /// </summary>
        /// <param name="json">glTF JSON</param>
        /// <param name="uri">Base URI for relative paths of external buffers or images</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        public async Task<bool> LoadGltfJson(
            string json,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            var success = false;
            try
            {
                m_Settings = importSettings ?? new ImportSettings();
                BaseUri = UriHelper.GetBaseUri(uri);
                var jsonUTF8 = Encoding.UTF8.GetBytes(json);
                using var jsonNative = new ReadOnlyNativeArrayFromManagedArray<byte>(jsonUTF8);
                success =
                    await LoadGltf(jsonNative.Array.AsNativeArrayReadOnly(), cancellationToken)
                    && await LoadContent(cancellationToken)
                    && await Prepare(cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Logger?.Info(LogCode.OperationCanceled, e.Message);
                await DisposeTextureLoadTasks();
                throw;
            }
            finally
            {
                if (!success)
                {
                    await DisposeTextureLoadTasks();
                }
                DisposeVolatileData();
            }

            LoadingError = !success;
            LoadingDone = true;
            return success;
        }

        /// <inheritdoc cref="InstantiateMainSceneAsync(Transform,CancellationToken)"/>
        [Obsolete("Use InstantiateMainSceneAsync for increased performance and safety. Consult the Upgrade Guide for instructions.")]
        public bool InstantiateMainScene(Transform parent)
        {
            return InstantiateMainSceneAsync(parent).Result;
        }

        /// <inheritdoc cref="InstantiateMainSceneAsync(IInstantiator,CancellationToken)"/>
        [Obsolete("Use InstantiateMainSceneAsync for increased performance and safety. Consult the Upgrade Guide for instructions.")]
        public bool InstantiateMainScene(IInstantiator instantiator)
        {
            return InstantiateMainSceneAsync(instantiator).Result;
        }

        /// <inheritdoc cref="InstantiateSceneAsync(Transform,int,CancellationToken)"/>
        [Obsolete("Use InstantiateSceneAsync for increased performance and safety. Consult the Upgrade Guide for instructions.")]
        public bool InstantiateScene(Transform parent, int sceneIndex = 0)
        {
            return InstantiateSceneAsync(parent, sceneIndex).Result;
        }

        /// <inheritdoc cref="InstantiateSceneAsync(IInstantiator,int,CancellationToken)"/>
        [Obsolete("Use InstantiateSceneAsync for increased performance and safety. Consult the Upgrade Guide for instructions.")]
        public bool InstantiateScene(IInstantiator instantiator, int sceneIndex = 0)
        {
            return InstantiateSceneAsync(instantiator, sceneIndex).Result;
        }

        /// <summary>
        /// Creates an instance of the main scene of the glTF ( <see cref="Root.scene">scene</see> property in the JSON at root level)
        /// If the main scene index is not set, it instantiates nothing (as defined in the glTF 2.0 specification)
        /// </summary>
        /// <param name="parent">Transform that the scene will get parented to</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the main scene was instantiated or was not set. False in case of errors.</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        /// <seealso cref="DefaultSceneIndex"/>
        public async Task<bool> InstantiateMainSceneAsync(
            Transform parent,
            CancellationToken cancellationToken = default
            )
        {
            var instantiator = new GameObjectInstantiator(this, parent);
            var success = await InstantiateMainSceneAsync(instantiator, cancellationToken);
            return success;
        }

        /// <summary>
        /// Creates an instance of the main scene of the glTF ( <see cref="Root.scene">scene</see> property in the JSON at root level)
        /// If the main scene index is not set, it instantiates nothing (as defined in the glTF 2.0 specification)
        /// </summary>
        /// <param name="instantiator">Instantiator implementation; Receives and processes the scene data</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the main scene was instantiated or was not set. False in case of errors.</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        /// <seealso cref="DefaultSceneIndex"/>
        public async Task<bool> InstantiateMainSceneAsync(
            IInstantiator instantiator,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            // According to glTF specification, loading nothing is
            // the correct behavior
            if (!Root.Scene.HasValue)
            {
#if DEBUG
                Debug.LogWarning("glTF has no (main) scene defined. No scene will be instantiated.");
#endif
                return true;
            }
            return await InstantiateSceneAsync(instantiator, Root.Scene.Value, cancellationToken);
        }

        /// <summary>
        /// Creates an instance of the scene specified by the scene index.
        /// </summary>
        /// <param name="parent">Transform that the scene will get parented to</param>
        /// <param name="sceneIndex">Index of the scene to be instantiated</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the scene was instantiated. False in case of errors.</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        /// <seealso cref="SceneCount"/>
        /// <seealso cref="GetSceneName"/>
        public async Task<bool> InstantiateSceneAsync(
            Transform parent,
            int sceneIndex = 0,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            if (sceneIndex < 0 || sceneIndex >= Root.Scenes.Count) return false;
            var instantiator = new GameObjectInstantiator(this, parent);
            var success = await InstantiateSceneAsync(instantiator, sceneIndex, cancellationToken);
            return success;
        }

        /// <summary>
        /// Creates an instance of the scene specified by the scene index.
        /// </summary>
        /// <param name="instantiator">Instantiator implementation; Receives and processes the scene data</param>
        /// <param name="sceneIndex">Index of the scene to be instantiated</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the scene was instantiated. False in case of errors.</returns>
        /// <exception cref="OperationCanceledException">Thrown when cancelled before completion.</exception>
        /// <seealso cref="SceneCount"/>
        /// <seealso cref="GetSceneName"/>
        public async Task<bool> InstantiateSceneAsync(
            IInstantiator instantiator,
            int sceneIndex = 0,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            if (sceneIndex < 0 || sceneIndex >= Root.Scenes.Count) return false;
            try
            {
                await InstantiateSceneInternal(instantiator, sceneIndex, cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Logger?.Info(LogCode.OperationCanceled, e.Message);
                throw;
            }
            return true;
        }

        /// <summary>
        /// Disposes resources that are required for instantiation.
        /// Does not dispose imported resources (meshes, materials, textures, etc.).
        /// </summary>
        internal void DisposeRequiredForInstantiationData()
        {
            if (m_AccessorData != null)
            {
                for (var index = 0; index < m_AccessorData.Length; index++)
                {
                    m_AccessorData[index]?.Dispose();
                    m_AccessorData[index] = null;
                }

                m_AccessorData = null;
            }
        }

        /// <summary>
        /// Frees up memory by disposing all sub assets.
        /// There can be no instantiation or other element access afterwards.
        /// </summary>
        public void Dispose()
        {
            DisposeRequiredForInstantiationData();

            m_Addons?.Dispose();

            m_NodeNames = null;

            DestroyUtils.SafeDestroy(m_Materials);
            m_Materials = null;

            if (m_DataInstanceApplierFactories != null)
            {
                foreach (var factory in m_DataInstanceApplierFactories)
                {
                    factory?.Dispose();
                }
                m_DataInstanceApplierFactories = null;
            }

            DestroyUtils.SafeDestroy(m_Textures);
            m_Textures = null;

            m_MeshAssignments = null;
            DestroyUtils.SafeDestroy(m_Meshes);
            m_Meshes = null;
            DestroyUtils.SafeDestroy(m_Resources);
            m_Resources = null;
        }

        /// <summary>
        /// Number of materials
        /// </summary>
        public int MaterialCount => m_Materials?.Length ?? 0;

        /// <inheritdoc cref="IGltfReadable.ImageCount"/>
        [Obsolete("Use TextureCount and GetTexture instead. This property will be removed in future releases.")]
        public int ImageCount => Root?.Images?.Count ?? 0;

        /// <inheritdoc cref="IGltfReadable.TextureCount"/>
        public int TextureCount => m_Textures?.Length ?? 0;

        /// <summary>
        /// Default scene index
        /// </summary>
        public int? DefaultSceneIndex => Root != null && Root.Scene >= 0 ? Root.Scene : (int?)null;

        /// <summary>
        /// Number of scenes
        /// </summary>
        public int SceneCount => Root?.Scenes?.Count ?? 0;

        /// <summary>
        /// Get a glTF's scene's name by its index
        /// </summary>
        /// <param name="sceneIndex">glTF scene index</param>
        /// <returns>Scene name or null</returns>
        public string GetSceneName(int sceneIndex)
        {
            return Root?.Scenes?[sceneIndex]?.Name;
        }

        /// <inheritdoc />
        public UnityEngine.Material GetMaterial(int index = 0)
        {
            if (m_Materials != null && index >= 0 && index < m_Materials.Length)
            {
                return m_Materials[index];
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<UnityEngine.Material> GetMaterialAsync(int index)
        {
            return await GetMaterialAsync(index, new CancellationToken());
        }

        /// <inheritdoc />
        public Task<UnityEngine.Material> GetMaterialAsync(int index, CancellationToken cancellationToken)
        {
            return Task.FromResult(GetMaterial(index));
        }

        /// <inheritdoc />
        public UnityEngine.Material GetDefaultMaterial()
        {
#if UNITY_EDITOR
            if (defaultMaterial == null)
            {
                m_MaterialGenerator.SetLogger(m_Context.Logger);
                defaultMaterial = m_MaterialGenerator.GetDefaultMaterial(m_DefaultMaterialPointsSupport);
                m_MaterialGenerator.SetLogger(null);
            }
            return defaultMaterial;
#else
            m_MaterialGenerator.SetLogger(m_Context.Logger);
            var material = m_MaterialGenerator.GetDefaultMaterial(m_DefaultMaterialPointsSupport);
            m_MaterialGenerator.SetLogger(null);
            return material;
#endif
        }

        /// <inheritdoc />
        public async Task<UnityEngine.Material> GetDefaultMaterialAsync()
        {
            return await GetDefaultMaterialAsync(new CancellationToken());
        }

        /// <inheritdoc />
        public Task<UnityEngine.Material> GetDefaultMaterialAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(GetDefaultMaterial());
        }

        /// <inheritdoc cref="IGltfReadable.GetImage"/>
        [Obsolete("Use GetTexture instead. This method will be removed in future releases.")]
        public Texture2D GetImage(int index = 0)
        {
            if (Root is { Textures: { Count: > 0 }, Images: { Count: > 0 } }
                && index >= 0 && index < Root.Images.Count)
            {
                for (var textureIndex = 0; textureIndex < Root.Textures.Count; textureIndex++)
                {
                    var texture = Root.Textures[textureIndex];
                    if (index == texture.GetImageIndex())
                    {
                        return GetTexture(textureIndex);
                    }
                }
            }

            return null;
        }

        /// <inheritdoc cref="IGltfReadable.GetTexture"/>
        public Texture2D GetTexture(int index = 0)
        {
            if (m_Textures != null && index >= 0 && index < m_Textures.Length)
            {
                return m_Textures[index];
            }
            return null;
        }

        /// <inheritdoc cref="IGltfReadable.IsTextureYFlipped"/>
        public bool IsTextureYFlipped(int index = 0)
        {
            return m_NonFlippedYTextureIndices == null || !m_NonFlippedYTextureIndices.Contains(index);
        }

#if UNITY_ANIMATION
        /// <summary>
        /// Returns all imported animation clips
        /// </summary>
        /// <returns>All imported animation clips</returns>
        public AnimationClip[] GetAnimationClips()
        {
            if (m_DataInstanceApplierFactories == null)
                return null;
            foreach (var factory in m_DataInstanceApplierFactories)
            {
                if (factory is AnimationModuleDataInstanceApplierFactory animationDataCache)
                {
                    return animationDataCache.Data;
                }
            }
            return null;
        }
#endif

        /// <summary>
        /// Returns all imported meshes
        /// </summary>
        /// <returns>All imported meshes</returns>
        [Obsolete("Use Meshes instead.")]
        public UnityEngine.Mesh[] GetMeshes()
        {
            if (m_Meshes == null || m_Meshes.Count < 1) return Array.Empty<UnityEngine.Mesh>();
            return m_Meshes.ToArray();
        }

        /// <summary>
        /// Allows accessing all imported meshes.
        /// </summary>
        public IReadOnlyCollection<UnityEngine.Mesh> Meshes => m_Meshes;

        /// <summary>
        /// Imported Unity Mesh count. A single glTF mesh is converted into one or more Unity Meshes.
        /// </summary>
        /// <param name="meshIndex">glTF mesh index.</param>
        /// <returns>Number of imported Unity meshes.</returns>
        /// <seealso cref="GetMeshes(int)"/>
        public int GetMeshCount(int meshIndex)
        {
            return m_MeshAssignments.GetLength(meshIndex);
        }

        /// <summary>
        /// Iterates all imported Unity meshes of a glTF mesh.
        /// </summary>
        /// <param name="meshIndex">glTF mesh index.</param>
        /// <returns>Iteration over one or more Unity meshes.</returns>
        /// <seealso cref="GetMeshCount"/>
        public IEnumerable<UnityEngine.Mesh> GetMeshes(int meshIndex)
        {
            foreach (var assignment in m_MeshAssignments.Values(meshIndex))
            {
                yield return assignment.mesh;
            }
        }

        /// <summary>
        /// Gets a specific Unity mesh of a glTF mesh.
        /// A single glTF mesh is converted into one or more Unity Meshes, so <paramref name="meshNumeration" /> is
        /// required to depict which exact one.
        /// </summary>
        /// <param name="meshIndex">glTF mesh index.</param>
        /// <param name="meshNumeration">Per glTF mesh <see cref="MeshResult"/> numeration. A glTF mesh is converted
        /// into one or more MeshResults which are numbered consecutively.</param>
        /// <returns>An imported Unity mesh.</returns>
        public UnityEngine.Mesh GetMesh(int meshIndex, int meshNumeration)
        {
            return m_MeshAssignments.GetValue(meshIndex, meshNumeration).mesh;
        }

        /// <inheritdoc />
        public Camera GetSourceCamera(uint index)
        {
            if (Root?.Cameras != null && index < Root.Cameras.Count)
            {
                return Root.Cameras[(int)index];
            }
            return null;
        }

        /// <inheritdoc />
        public LightPunctual GetSourceLightPunctual(uint index)
        {
            if (Root?.Extensions?.LightsPunctual?.Lights != null && index < Root.Extensions.LightsPunctual.Lights.Count)
            {
                return Root.Extensions.LightsPunctual.Lights[(int)index];
            }
            return null;
        }

        /// <inheritdoc />
        public Scene GetSourceScene(int index = 0)
        {
            if (Root?.Scenes != null && index >= 0 && index < Root.Scenes.Count)
            {
                return Root.Scenes[index];
            }
            return null;
        }

        /// <inheritdoc />
        public Material GetSourceMaterial(int index = 0)
        {
            if (Root?.Materials != null && index >= 0 && index < Root.Materials.Count)
            {
                return Root.Materials[index];
            }
            return null;
        }

        /// <inheritdoc />
        public Mesh GetSourceMesh(int meshIndex)
        {
            if (Root?.Meshes != null && meshIndex >= 0 && meshIndex < Root.Meshes.Count)
            {
                return Root.Meshes[meshIndex];
            }
            return null;
        }

        /// <inheritdoc />
        public MeshPrimitive GetSourceMeshPrimitive(int meshIndex, int primitiveIndex)
        {
            if (Root?.Meshes != null && meshIndex >= 0 && meshIndex < Root.Meshes.Count)
            {
                var mesh = Root.Meshes[meshIndex];
                if (mesh?.Primitives != null && primitiveIndex >= 0 && primitiveIndex < mesh.Primitives.Count)
                {
                    return mesh.Primitives[primitiveIndex];
                }
            }
            return null;
        }

        /// <inheritdoc />
        public IMaterialsVariantsSlot[] GetMaterialsVariantsSlots(int meshIndex, int meshNumeration)
        {
            List<IMaterialsVariantsSlot> materialSlots = null;
            var meshResult = m_MeshAssignments.GetValue(meshIndex, meshNumeration);
            foreach (var primitiveIndex in meshResult.primitives)
            {
                var primitive = GetSourceMeshPrimitive(meshIndex, primitiveIndex);
                if (primitive.Extensions?.MaterialsVariants?.Mappings != null)
                {
                    materialSlots ??= new List<IMaterialsVariantsSlot>();
                    materialSlots.Add(primitive);
                }
            }

            return materialSlots?.ToArray();
        }

        /// <inheritdoc />
        public Node GetSourceNode(int index = 0)
        {
            if (Root?.Nodes != null && index >= 0 && index < Root.Nodes.Count)
            {
                return Root.Nodes[index];
            }
            return null;
        }

        /// <inheritdoc />
        public Texture GetSourceTexture(int index = 0)
        {
            if (Root?.Textures != null && index >= 0 && index < Root.Textures.Count)
            {
                return Root.Textures[index];
            }
            return null;
        }

        /// <inheritdoc />
        public Image GetSourceImage(int index = 0)
        {
            if (Root?.Images != null && index >= 0 && index < Root.Images.Count)
            {
                return Root.Images[index];
            }
            return null;
        }

        /// <inheritdoc />
        public Matrix4x4[] GetBindPoses(int skinId)
        {
            if (m_SkinsInverseBindMatrices == null) return null;
            if (m_SkinsInverseBindMatrices[skinId] != null)
            {
                return m_SkinsInverseBindMatrices[skinId];
            }

            var skin = Root.Skins[skinId];
            var result = new Matrix4x4[skin.Joints.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Matrix4x4.identity;
            }
            m_SkinsInverseBindMatrices[skinId] = result;
            return result;
        }

        /// <inheritdoc />
        [Obsolete("This is going to be removed and replaced with an improved way to access accessors' data in a future release.")]
        public NativeArray<byte>.ReadOnly GetAccessor(int accessorIndex)
        {
            return GetAccessorData(accessorIndex);
        }

        /// <inheritdoc />
        [Obsolete("This is going to be removed and replaced with an improved way to access accessors' data in a future release.")]
        public NativeArray<byte>.ReadOnly GetAccessorData(int accessorIndex)
        {
            if (Root?.Accessors == null || accessorIndex < 0 || accessorIndex >= Root?.Accessors.Count)
            {
                return default;
            }
            var accessor = Root.Accessors[accessorIndex];
            return ((IGltfBuffers)this).GetBufferView(
                accessor.BufferView.Value,
                out _,
                accessor.ByteOffset,
                accessor.ByteSize
                ).AsNativeArrayReadOnly();
        }

        /// <summary>
        /// Loads an image's data, regardless of source type (URI, bufferView, data URI).
        /// </summary>
        /// <param name="image">glTF image</param>
        /// <param name="cancellationToken">Can be used to abort the loading procedure.</param>
        /// <returns>Image's data (needs to be disposed after consumption)</returns>
        async Task<IReadOnlyDisposableData> GetImageDataAsync(Image image, CancellationToken cancellationToken)
        {
            if (image.BufferView.HasValue)
            {
                var bufferView = Root.BufferViews[image.BufferView.Value];
                return new ReadOnlyData(
                    await GetBufferViewAsync(bufferView),
                    null
                    );
            }

            if (image.Uri != null)
            {
                if (image.Uri.IsData || image.Uri.IsFailed)
                {
                    Logger?.Warning(LogCode.EmbedSlow);
                    if (image.Uri.IsFailed || !image.Uri.TryGetData(out var data))
                    {
                        Logger?.Error(LogCode.EmbedImageLoadFailed);
                        return null;
                    }
                    // Non-owning view: the UriValue (tracked by m_VolatileDisposables) owns the
                    // NativeArray. This makes the same image safely consumable by multiple readers.
                    return new ReadOnlyData(data.AsReadOnly(), null);
                }

                var uri = UriHelper.GetUriString(image.Uri.AsString(), BaseUri);
                if (!uri.IsAbsoluteUri && BaseUri == null)
                {
                    Logger?.Error(
                        LogCode.TextureDownloadFailed,
                        $"Couldn't resolve relative URI. Please provide a base URI.",
                        uri.ToString());
                    return null;
                }

                return await ImageImport.LoadDataAsync(m_Context, uri, cancellationToken);
            }

            Logger?.Error(LogCode.MissingImageURL);
            return null;
        }

        /// <inheritdoc />
        public int MaterialsVariantsCount => Root.MaterialsVariantsCount;

        /// <inheritdoc />
        public string GetMaterialsVariantName(int index)
        {
            return Root.GetMaterialsVariantName(index);
        }

        static void CopyStreamToMemory(Stream source, Memory<byte> destination)
        {
            Profiler.BeginSample("CopyStreamToMemory");
            var bytesToWrite = destination.Length;
            var bufferSize = math.min((int)k_CopyBufferSize, bytesToWrite);
            var span = destination.Span;
            int read;
            while (bytesToWrite > 0
                   && (read = source.Read(
                       span[..math.min(bufferSize, bytesToWrite)]
                       )) > 0)
            {
                span = span[read..];
                bytesToWrite -= read;
            }
            Profiler.EndSample();
        }

        async ValueTask CopyStreamToMemoryAsync(Stream source, Memory<byte> destination)
        {
            Profiler.BeginSample("CopyStreamToMemoryAsync");
            var bytesToWrite = destination.Length;
            var bufferSize = math.min((int)k_CopyBufferSize, bytesToWrite);

            var start = 0;
            int read;
            while (bytesToWrite > 0
                   && (read = StreamReadToMemory(math.min(bufferSize, bytesToWrite))) > 0)
            {
                start += read;
                bytesToWrite -= read;
                if (bytesToWrite > 0 && DeferAgent.ShouldDefer(bufferSize / (float)k_MemCopySpeed))
                {
                    Profiler.EndSample();
                    await Task.Yield();
                    Profiler.BeginSample("CopyStreamToMemoryAsync");
                }
            }
            Profiler.EndSample();
            return;

            int StreamReadToMemory(int length)
            {
                return source.Read(destination.Span[start..(start + length)]);
            }
        }

        async Task<bool> LoadFromUri(Uri url, CancellationToken cancellationToken)
        {
            BaseUri = UriHelper.GetBaseUri(url);
            var success = true;
            IDownload download = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequestedWithTracking();

                download = await m_Context.DownloadProvider.Request(url);
                success = download.Success;

                cancellationToken.ThrowIfCancellationRequestedWithTracking();

                if (success)
                {
                    var gltfBinary = download.IsBinary ?? UriHelper.IsGltfBinary(url);

                    m_VolatileDisposables ??= new List<IDisposable>();
                    NativeArray<byte>.ReadOnly data;
                    if (download is INativeDownload nativeDownload)
                    {
                        data = nativeDownload.NativeData;
                    }
                    else
                    {
                        var managedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(download.Data);
                        m_VolatileDisposables.Add(managedNativeArray);
                        data = managedNativeArray.Array.AsNativeArrayReadOnly();
                    }

                    m_VolatileDisposables.Add(download);

                    success = gltfBinary ?? false
                        ? await LoadGltfBinaryBuffer(data, cancellationToken)
                        : await LoadGltf(data, cancellationToken);

                    success = success
                        && await LoadContent(cancellationToken)
                        && await Prepare(cancellationToken);
                }
                else
                {
                    Logger?.Error(LogCode.Download, download.Error, url.ToString());
                }
            }
            catch (OperationCanceledException e)
            {
                Logger?.Info(LogCode.OperationCanceled, e.Message);
                if (m_VolatileDisposables?.Contains(download) is false)
                    download?.Dispose();
                await DisposeTextureLoadTasks();
                throw;
            }
            finally
            {
                if (!success)
                {
                    await DisposeTextureLoadTasks();
                }
                DisposeVolatileData();
            }

            LoadingError = !success;
            LoadingDone = true;
            return success;
        }

        async Task<bool> LoadGltfBinaryInternal(
            NativeArray<byte>.ReadOnly bytes,
            Uri uri, ImportSettings importSettings,
            CancellationToken cancellationToken
            )
        {
            var success = true;
            BaseUri = UriHelper.GetBaseUri(uri);
            try
            {
                m_Settings = importSettings ?? new ImportSettings();
                success = await LoadGltfBinaryBuffer(bytes, cancellationToken);
                success = success
                    && await LoadContent(cancellationToken)
                    && await Prepare(cancellationToken);
            }
            catch (OperationCanceledException e)
            {
                Logger?.Info(LogCode.OperationCanceled, e.Message);
                await DisposeTextureLoadTasks();
                throw;
            }
            finally
            {
                if (!success)
                {
                    await DisposeTextureLoadTasks();
                }
                DisposeVolatileData();
            }

            LoadingError = !success;
            LoadingDone = true;
            return success;
        }

        async Task<bool> LoadContent(CancellationToken cancellationToken)
        {

            var success = await WaitForBufferDownloads(cancellationToken);

            cancellationToken.ThrowIfCancellationRequestedWithTracking();

#if MESHOPT_IS_ENABLED
            if (success)
            {
                MeshoptDecode();
            }
#endif
            return success;
        }

        /// <summary>
        /// De-serializes a UTF-8 encoded glTF JSON string and returns the glTF root object along
        /// with the list of decoded data URI <see cref="UriValue"/>s produced by the converter.
        /// Ownership of those entries transfers to the caller, which must add them to a disposal
        /// container (e.g. <c>m_VolatileDisposables</c>) or dispose them directly.
        /// </summary>
        /// <param name="json">glTF JSON (UTF-8 encoded)</param>
        /// <returns>De-serialized glTF root object paired with the converter's pending list.</returns>
        protected static (Root root, List<UriValue> pending) ParseJson(ReadOnlySpan<byte> json)
        {
            Profiler.BeginSample("ParseJson");
            UriValueConverter.BeginCollect();
            try
            {
                var root = JsonSerializer.Deserialize(json, GltfJsonContext.Default.Root);
                var pending = UriValueConverter.EndCollect();
                return (root, pending);
            }
            catch
            {
                // Failure: dispose any decoded data URIs the converter produced before
                // the deserializer threw, otherwise their NativeArrays leak.
                var pending = UriValueConverter.EndCollect();
                if (pending != null)
                {
                    foreach (var uri in pending)
                    {
                        uri.Dispose();
                    }
                }
                throw;
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        async Task<bool> ParseJsonAndLoadBuffers(
            NativeArray<byte>.ReadOnly json,
            CancellationToken cancellationToken,
            int offset = 0,
            int length = -1
            )
        {
            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            if (length < 0)
            {
                length = json.Length - offset;
            }
            Assert.IsTrue(
                offset >= 0 && length >= 0 && offset + length <= json.Length,
                "Invalid offset and length parameters for JSON parsing."
                );

            var predictedTime = length / (float)k_JsonParseSpeed;
            List<UriValue> pending = null;
#if GLTFAST_THREADS && !MEASURE_TIMINGS
            if (DeferAgent.ShouldDefer(predictedTime))
            {
                // JSON is larger than threshold
                // => parse in a thread
                try
                {
                    (Root, pending) = await Task.Run(
                        () => ParseJson(json.AsReadOnlySpan().Slice(offset, length)),
                        cancellationToken
                        );
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                }
            }
            else
#endif
            {
                // Parse immediately on main thread
                (Root, pending) = ParseJson(json.AsReadOnlySpan().Slice(offset, length));

                // Loading subsequent buffers and images has to start asap.
                // That's why parsing JSON right away is *very* important.
            }

            if (pending != null && pending.Count > 0)
            {
                m_VolatileDisposables ??= new List<IDisposable>(pending.Count);
                foreach (var uri in pending)
                {
                    m_VolatileDisposables.Add(uri);
                }
            }

            if (Root == null)
            {
                Debug.LogError("JsonParsingFailed");
                Logger?.Error(LogCode.JsonParsingFailed);
                return false;
            }

            if (m_Addons?
                    .Any<IPostJsonDeserialization>(addon => !addon.PostJsonDeserialization())
                ?? false)
            {
                return false;
            }

            if (!CheckExtensionSupport())
            {
                return false;
            }

            if (Root.Buffers != null)
            {
                var bufferCount = Root.Buffers.Count;
                if (bufferCount > 0)
                {
                    m_Buffers = new ReadOnlyNativeArray<byte>[bufferCount];
                    m_BinChunks = new GlbBinChunk[bufferCount];
                }

                for (var i = 0; i < bufferCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();

                    var buffer = Root.Buffers[i];
                    if (buffer.Uri != null)
                    {
                        m_BufferLoadTasks ??= new Dictionary<int, Task<bool>>();
                        if (buffer.Uri.IsData || buffer.Uri.IsFailed)
                        {
                            Logger?.Warning(LogCode.EmbedSlow);
                            m_BufferLoadTasks[i] = LoadBufferFromDataUri(i, buffer, cancellationToken);
                        }
                        else
                        {
                            m_BufferLoadTasks[i] = LoadBufferFromUriAsync(
                                i, UriHelper.GetUriString(buffer.Uri.AsString(), BaseUri));
                        }
                    }
                }
            }

            return true;
        }

#pragma warning disable CS1998 // async method lacking 'await' is intentional: this stays a Task to match LoadBufferFromUriAsync.
        async Task<bool> LoadBufferFromDataUri(int bufferIndex, Buffer buffer, CancellationToken cancellationToken)
#pragma warning restore CS1998
        {
            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            if (buffer.Uri.IsFailed)
            {
                Logger?.Error(LogCode.EmbedBufferLoadFailed);
                return false;
            }

            var mimeType = buffer.Uri.MimeType;
            if (!mimeType.StartsWith("application/", StringComparison.Ordinal)
                || !(
                    mimeType.AsSpan(12).SequenceEqual("octet-stream")
                    || mimeType.AsSpan(12).SequenceEqual("gltf-buffer")
                    )
                )
            {
                Logger?.Error(
                    LogCode.BufferDataUriUnexpectedMimeType,
                    bufferIndex.ToString(),
                    mimeType
                    );
                return false;
            }

            if (!buffer.Uri.TryGetData(out var data))
            {
                Logger?.Error(LogCode.EmbedBufferLoadFailed);
                return false;
            }

            if (data.Length < buffer.ByteLength)
            {
                Logger?.Error(
                    LogCode.BufferContentUndersized,
                    bufferIndex.ToString(),
                    buffer.ByteLength.ToString(),
                    data.Length.ToString()
                    );
                return false;
            }

            // The UriValue (already tracked via m_VolatileDisposables from ParseJsonAndLoadBuffers
            // draining the converter's pending list) retains ownership of the NativeArray.
            m_Buffers[bufferIndex] = new ReadOnlyNativeArray<byte>(data);
            if (bufferIndex != 0 || !m_GlbBinChunk.HasValue)
            {
                m_BinChunks[bufferIndex] = new GlbBinChunk(0, (uint)m_Buffers[bufferIndex].Length);
            }
            return true;
        }

        /// <summary>
        /// Validates required and used glTF extensions and reports unsupported ones.
        /// </summary>
        /// <returns>False if a required extension is not supported. True otherwise.</returns>
        bool CheckExtensionSupport()
        {
            if (!CheckExtensionSupport(Root.ExtensionsRequired))
            {
                return false;
            }
            CheckExtensionSupport(Root.ExtensionsUsed, false);
            return true;
        }

        bool CheckExtensionSupport(List<EnumOrRawValue<Extension>> extensions, bool required = true)
        {
            if (extensions == null)
                return true;
            var allExtensionsSupported = true;
            foreach (var ext in extensions)
            {
                if (ext.RawValue != null)
                {
                    var name = System.Text.Encoding.UTF8.GetString(ext.RawValue);
                    var supported = m_Addons != null && m_Addons.AnySupportsGltfExtension(name);
                    if (!supported)
                    {
                        Logger?.Log(
                            required ? LogType.Error : LogType.Warning,
                            LogCode.ExtensionUnsupported,
                            name
                            );
                        allExtensionsSupported = false;
                    }
                }
                else
                {
                    var supported = k_SupportedExtensions.Contains(ext.Value)
                        || (m_Addons != null && m_Addons.AnySupportsGltfExtension(ext.Value.GetName()));
                    if (!supported)
                    {
                        ReportUnsupportedExtension(ext.Value, required);
                        allExtensionsSupported = false;
                    }
                }
            }
            return allExtensionsSupported;
        }

        void ReportUnsupportedExtension(Extension ext, bool required)
        {
#if !DRACO_IS_ENABLED
            if (ext == Extension.DracoMeshCompression)
            {
                Logger?.Log(
                    required ? LogType.Error : LogType.Warning,
                    LogCode.PackageMissing,
                    "Draco for Unity",
                    ExtensionName.DracoMeshCompression
                    );
                return;
            }
#endif
#if !MESHOPT
            if (ext == Extension.MeshoptCompression)
            {
                Logger?.Log(
                    required ? LogType.Error : LogType.Warning,
                    LogCode.PackageMissing,
                    "meshoptimizer decompression for Unity",
                    ExtensionName.MeshoptCompression
                );
                return;
            }
#endif
#if !KTX_IS_ENABLED
            if (ext == Extension.TextureBasisUniversal)
            {
                Logger?.Log(
                    required ? LogType.Error : LogType.Warning,
                    LogCode.PackageMissing,
                    "KTX for Unity",
                    ExtensionName.TextureBasisUniversal
                    );
                return;
            }
#endif
            Logger?.Log(
                required ? LogType.Error : LogType.Warning,
                LogCode.ExtensionUnsupported,
                ext.GetName()
                );
        }

        async Task<bool> LoadGltf(NativeArray<byte>.ReadOnly json, CancellationToken cancellationToken)
        {
            var success = await ParseJsonAndLoadBuffers(json, cancellationToken);
            if (success)
                LoadImages(cancellationToken);
            return success;
        }

        void LoadImages(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequestedWithTracking();

            if (Root.Textures is { Count: > 0 })
            {
                Dictionary<int, ITextureImageLoader> textureImageLoaderMap = null;
                static bool OverridesImage(ITextureImageLoader loader, Texture texture, out int imageIndex)
                {
                    return loader.IsAbleToLoad(texture, out imageIndex);
                }

                m_Addons
                    ?.SubCollection<ITextureImageLoader>()
                    ?.ForEachTryGet<Texture, int>(
                    Root.Textures,
                    OverridesImage,
                    (addon, textureIndex, imageIndex) =>
                    {
                        m_TextureImageOverrides ??= new Dictionary<int, int>();
                        m_TextureImageOverrides[textureIndex] = imageIndex;
                        textureImageLoaderMap ??= new Dictionary<int, ITextureImageLoader>();
                        textureImageLoaderMap[textureIndex] = addon;
                    }
                );

                bool[] textureGamma = null;

                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                {
                    textureGamma = new bool[Root.Textures.Count];

                    void SetTextureGamma(TextureInfo textureInfo)
                    {
                        if (
                            textureInfo?.Index is int index &&
                            index < Root.Textures.Count
                        )
                        {
                            textureGamma[index] = true;
                        }
                    }

                    if (Root.Materials != null)
                    {
                        foreach (var material in Root.Materials)
                        {
                            if (material.PbrMetallicRoughness != null)
                            {
                                SetTextureGamma(material.PbrMetallicRoughness.BaseColorTexture);
                            }
                            SetTextureGamma(material.EmissiveTexture);
                            if (material.Extensions?.PbrSpecularGlossiness != null)
                            {
                                SetTextureGamma(
                                    material.Extensions.PbrSpecularGlossiness.DiffuseTexture);
                                SetTextureGamma(
                                    material.Extensions.PbrSpecularGlossiness.SpecularGlossinessTexture);
                            }
                        }
                    }
                }

#if !UNITY_VISIONOS
                HashSet<int> readableTextureIndices = null;
#endif

                if (Root.Images is { Count: > 0 })
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    Profiler.BeginSample("LoadImages.Prepare");

                    var defaultKey = new SamplerKey(new Sampler());
                    var imageVariants = new Dictionary<SamplerKey, int>[Root.Images.Count];
                    for (var textureIndex = 0; textureIndex < Root.Textures.Count; textureIndex++)
                    {
                        var txt = Root.Textures[textureIndex];

                        // Determine which images need to be readable, because they
                        // are applied using different samplers.
                        SamplerKey key;
                        if (txt.Sampler.HasValue)
                        {
                            var sampler = Root.Samplers[txt.Sampler.Value];
                            key = new SamplerKey(sampler);
                        }
                        else
                        {
                            key = defaultKey;
                        }

                        if (GetImageIndexFromTexture(txt, textureIndex) is int imageIndex && imageIndex < Root.Images.Count)
                        {
                            if (imageVariants[imageIndex] == null)
                            {
                                imageVariants[imageIndex] =
                                    new Dictionary<SamplerKey, int> { [key] = textureIndex };
                            }
                            else
                            {
                                if (imageVariants[imageIndex].TryGetValue(key, out var existingTextureIndex))
                                {
                                    // Same sampler already used for this image, so reuse the texture.
                                    m_TextureReuseMap ??= new Dictionary<int, int>();
                                    m_TextureReuseMap[textureIndex] = existingTextureIndex;
                                }
                                else
                                {
                                    // Get an existing texture for this image to clone from later.
                                    using (var enumerator = imageVariants[imageIndex].Values.GetEnumerator())
                                    {
                                        if (enumerator.MoveNext())
                                        {
                                            existingTextureIndex = enumerator.Current;
                                        }
                                    }
                                    m_TextureResampleMap ??= new Dictionary<int, int>();
                                    m_TextureResampleMap[textureIndex] = existingTextureIndex;
                                    imageVariants[imageIndex][key] = textureIndex;
#if !UNITY_VISIONOS
                                    if (!m_Settings.TexturesReadable)
                                    {
                                        // Mark texture readable
                                        // to allow instantiation/cloning for different sampling later.
                                        readableTextureIndices ??= new HashSet<int>();
                                        readableTextureIndices.Add(existingTextureIndex);
                                    }
#endif
                                }
                            }
                        }
                    }

                    Profiler.EndSample();

                    m_TextureLoadTasks = new Dictionary<int, Task<ImageResult>>();

                }

                for (var textureIndex = 0; textureIndex < Root.Textures.Count; textureIndex++)
                {
                    var texture = Root.Textures[textureIndex];
                    if ((m_TextureReuseMap != null && m_TextureReuseMap.ContainsKey(textureIndex))
                        || (m_TextureResampleMap != null && m_TextureResampleMap.ContainsKey(textureIndex)))
                    {
                        continue;
                    }
                    var forceSampleLinear = textureGamma != null && !textureGamma[textureIndex];

#if UNITY_VISIONOS
                    // PolySpatial visionOS needs to be able to access raw texture data in order to
                    // do the material/texture conversion.
                    const bool readable = true;
#else
                    var readable = m_Settings.TexturesReadable
                        || (readableTextureIndices != null && readableTextureIndices.Contains(textureIndex));
#endif

                    var imageIndexNullable = GetImageIndexFromTexture(texture, textureIndex);
                    if (!imageIndexNullable.HasValue || imageIndexNullable.Value >= Root.Images.Count)
                    {
                        continue;
                    }
                    var imageIndex = imageIndexNullable.Value;
                    var img = Root.Images[imageIndex];

                    if (textureImageLoaderMap != null && textureImageLoaderMap.TryGetValue(textureIndex, out var loader))
                    {
                        m_TextureLoadTasks[textureIndex] = ImageImport.LoadImageAsync(
                                GetImageDataAsync(img, cancellationToken),
                                forceSampleLinear,
                                readable,
                                m_Settings.GenerateMipMaps,
                                cancellationToken,
                                loader,
                                DeferAgent
                            );
                        continue;
                    }

                    if (img.Uri != null && img.Uri.IsString)
                    {
                        // Load from URI
                        // Detect format based on mimeType or URI file extension.
                        var imgFormat = img.MimeType.Value == ImageMimeType.Undefined && img.MimeType.RawValue == null
                            ? UriHelper.GetImageFormatFromUri(img.Uri.AsString())
                            : ImageFormatExtensions.FromMimeType(img.MimeType);

                        if (imgFormat is ImageFormat.Jpeg or ImageFormat.Png)
                        {
                            if (m_Addons?.TryGet(
                                imgFormat,
                                (IDefaultImageFormatLoader defaultLoader, ImageFormat format, out Task<ImageResult> task) =>
                                {
                                    if (defaultLoader.IsAbleToLoad(format))
                                    {
                                        task = ImageImport.LoadImageAsync(
                                            GetImageDataAsync(img, cancellationToken),
                                            forceSampleLinear,
                                            readable,
                                            m_Settings.GenerateMipMaps,
                                            cancellationToken,
                                            defaultLoader,
                                            DeferAgent
                                            );
                                        return true;
                                    }
                                    task = null;
                                    return false;
                                },
                                out var defaultTask) ?? false)
                            {
                                m_TextureLoadTasks[textureIndex] = defaultTask;
                                continue;
                            }

                            // Jpeg and PNG are a special case. If feasible, they're loaded via UnityWebRequestTexture,
                            // which decodes them in a worker thread.
#if UNITY_IMAGECONVERSION
                            var loadFromBytes = ImageConversionImageLoader.LoadImageFromBytes(
                                forceSampleLinear,
                                m_Settings.GenerateMipMaps
                                );
                            if (!loadFromBytes)
                            {
                                var uri = UriHelper.GetUriString(img.Uri.AsString(), BaseUri);
                                m_TextureLoadTasks[textureIndex] = ImageConversionImageLoader.LoadAsync(
                                    m_Context, uri, readable, cancellationToken);
                                continue;
                            }
#else
                            Logger?.Error(LogCode.ImageConversionNotEnabled);
                            continue;
#endif
                        }

                        // Abort for formats known to be not supported to avoid pointless downloads.
                        if (
                            imgFormat == ImageFormat.WebP
#if !KTX_IS_ENABLED
                            || imgFormat == ImageFormat.Ktx
#endif
                            )
                        {
                            Logger?.Error(
                                LogCode.ImageFormatUnsupported,
                                imageIndex.ToString(),
                                imgFormat.ToString()
                                );
                            continue;
                        }
                    }

                    m_TextureLoadTasks[textureIndex] = ImageImport.LoadImageAsync(
                        m_Context,
                        m_Settings,
                        imageIndex,
                        forceSampleLinear,
                        readable,
                        m_Settings.GenerateMipMaps,
                        GetImageDataAsync(img, cancellationToken),
                        m_Addons,
                        cancellationToken
                    );
                }
            }
        }

        async Task<bool> WaitForBufferDownloads(CancellationToken cancellationToken)
        {
            if (m_BufferLoadTasks != null)
            {
                foreach (var loadTaskPair in m_BufferLoadTasks)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    if (!await loadTaskPair.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        async Task<bool> LoadBufferFromUriAsync(int index, Uri uri)
        {
            var request = m_Context.DownloadProvider.Request(uri);
            var download = await request;
            if (download.Success)
            {
                Profiler.BeginSample("GetData");

                m_VolatileDisposables ??= new List<IDisposable>();
                if (download is INativeDownload nativeDownload)
                {
                    var wrapper = new ReadOnlyNativeArrayFromNativeArray<byte>(nativeDownload.NativeData);
                    m_Buffers[index] = wrapper.Array;
                }
                else
                {
                    var wrapper = new ReadOnlyNativeArrayFromManagedArray<byte>(download.Data);
                    m_Buffers[index] = wrapper.Array;
                    m_VolatileDisposables.Add(wrapper);
                }

                m_VolatileDisposables.Add(download);

                Profiler.EndSample();

                if (index != 0 || !m_GlbBinChunk.HasValue)
                {
                    m_BinChunks[index] = new GlbBinChunk(0, (uint)m_Buffers[index].Length);
                }

                return true;
            }

            Logger?.Error(LogCode.BufferLoadFailed, download.Error, index.ToString());
            return false;
        }

        async Task<bool> LoadGltfBinaryBuffer(
            NativeArray<byte>.ReadOnly bytes,
            CancellationToken cancellationToken
            )
        {
            Profiler.BeginSample("LoadGltfBinary.Phase1");

            if (!GltfGlobals.IsGltfBinary(bytes))
            {
                Logger?.Error(LogCode.GltfNotBinary);
                Profiler.EndSample();
                return false;
            }

            var version = bytes.ReadUInt32(4);

            if (version != 2)
            {
                Logger?.Error(LogCode.GltfUnsupportedVersion, version.ToString());
                Profiler.EndSample();
                return false;
            }

            var totalLength = bytes.ReadUInt32(8);
            if (totalLength > bytes.Length)
            {
                Logger?.Error(LogCode.UnexpectedEndOfContent);
                Profiler.EndSample();
                return false;
            }

            int index = 12; // first chunk header

            Profiler.EndSample();

            while (index < totalLength)
            {

                if (index + 8 > totalLength)
                {
                    Logger?.Error(LogCode.ChunkIncomplete);
                    return false;
                }

                var chLength = bytes.ReadUInt32(index);
                index += 4;
                var chType = bytes.ReadUInt32(index);
                index += 4;

                if (index + chLength > totalLength)
                {
                    Logger?.Error(LogCode.ChunkIncomplete);
                    return false;
                }

                if (chType == (uint)ChunkFormat.Binary)
                {
                    Assert.IsFalse(m_GlbBinChunk.HasValue); // There can only be one binary chunk
                    m_GlbBinChunk = new GlbBinChunk(index, chLength);
                }
                else if (chType == (uint)ChunkFormat.Json)
                {
                    Assert.IsNull(Root);

                    var success = await ParseJsonAndLoadBuffers(
                        bytes,
                        cancellationToken,
                        index,
                        (int)chLength
                        );

                    if (!success)
                    {
                        return false;
                    }
                }
                else
                {
                    Logger?.Error(LogCode.ChunkUnknown, chType.ToString());
                    return false;
                }

                index += (int)chLength;
            }

            if (Root == null)
            {
                Logger?.Error(LogCode.ChunkJsonInvalid);
                return false;
            }

            if (m_GlbBinChunk.HasValue && m_BinChunks != null)
            {
                m_BinChunks[0] = m_GlbBinChunk.Value;
                var wrapper = new ReadOnlyNativeArrayFromNativeArray<byte>(bytes);
                m_Buffers[0] = wrapper.Array;
            }
            LoadImages(cancellationToken);
            return true;
        }

        ReadOnlyNativeArray<byte> GetBuffer(int index)
        {
            return m_Buffers[index];
        }

        ReadOnlyNativeArray<byte> IGltfBuffers.GetBufferView(
            int bufferViewIndex,
            out int? byteStride,
            int offset,
            int length
            )
        {
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                byteStride = bufferView.Extensions.ExtMeshoptCompression.ByteStride;
                var entireBuffer = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && length <= 0)
                {
                    return new ReadOnlyNativeArray<byte>(entireBuffer);
                }
                Assert.IsTrue(offset >= 0);
                if (length <= 0)
                {
                    length = entireBuffer.Length - offset;
                }
                Assert.IsTrue(offset + length <= entireBuffer.Length);
                return new ReadOnlyNativeArray<byte>(entireBuffer.GetSubArray(offset, length));
            }
#endif
            byteStride = bufferView.ByteStride;
            return GetBufferView(bufferView, offset, length);
        }


        ReadOnlyNativeArray<T> IGltfBuffers.GetAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset
            )
        {
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                var fullSlice = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && (count <= 0 || count * UnsafeUtility.SizeOf(typeof(T)) == fullSlice.Length))
                {
                    return new ReadOnlyNativeArray<byte>(fullSlice).Reinterpret<T>();
                }
                Assert.IsTrue(offset >= 0);
                Assert.IsTrue(count > 0);
                Assert.IsTrue(offset + count * UnsafeUtility.SizeOf(typeof(T)) <= fullSlice.Length);
                return new ReadOnlyNativeArray<byte>(fullSlice).GetSubArray(offset, count).Reinterpret<T>();
            }
#endif
            return GetAccessorData<T>(bufferView, count, offset);
        }

        ReadOnlyNativeStridedArray<T> IGltfBuffers.GetStridedAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset
        )
        {
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            if (bufferView.Extensions?.ExtMeshoptCompression != null)
            {
                unsafe
                {
                    var fullSlice = m_MeshoptBufferViews[bufferViewIndex];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var safety = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(fullSlice);
#endif
                    return new ReadOnlyNativeStridedArray<T>(
                        fullSlice.GetUnsafeReadOnlyPtr(),
                        fullSlice.Length,
                        offset,
                        count,
                        bufferView.ByteStride ?? 0
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        , ref safety
#endif
                        );
                }
            }
#endif
            return GetStridedAccessorData<T>(bufferView, count, offset);
        }

        ReadOnlyNativeArray<T> GetAccessorData<T>(
            IBufferView bufferView,
            int count,
            int offset = 0
        ) where T : unmanaged
        {
            Assert.IsTrue(offset >= 0);
            var bufferIndex = bufferView.Buffer;
            Assert.IsNotNull(m_Buffers);
            Assert.IsTrue(bufferIndex < m_Buffers.Length);
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);
            var chunk = m_BinChunks[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            return m_Buffers[bufferIndex].GetSubArray(totalOffset, count * UnsafeUtility.SizeOf<T>()).Reinterpret<T>();
        }

        ReadOnlyNativeStridedArray<T> GetStridedAccessorData<T>(
            IBufferView bufferView,
            int count,
            int offset = 0
        ) where T : unmanaged
        {
            Assert.IsTrue(offset >= 0);
            var bufferIndex = bufferView.Buffer;
            Assert.IsNotNull(m_Buffers);
            Assert.IsTrue(bufferIndex < m_Buffers.Length);
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);
            var chunk = m_BinChunks[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            var byteStride = bufferView.ByteStride ?? UnsafeUtility.SizeOf(typeof(T));
            return m_Buffers[bufferIndex].ToStrided<T>(totalOffset, count, byteStride);
        }

        async Task<NativeArray<byte>.ReadOnly> GetBufferViewAsync(
            IBufferView bufferView,
            int offset = 0,
            int length = 0
            )
        {
            var bufferIndex = bufferView.Buffer;
            if (!m_Buffers[bufferIndex].IsCreated)
            {
                var download = m_BufferLoadTasks?[bufferIndex];
                if (download != null)
                {
                    return await download
                        ? GetBufferView(bufferView, offset, length).AsNativeArrayReadOnly()
                        : default;
                }
            }

            return GetBufferView(bufferView, offset, length).AsNativeArrayReadOnly();
        }

        ReadOnlyNativeArray<byte> GetBufferView(
            IBufferView bufferView,
            int offset = 0,
            int length = 0
            )
        {
            Assert.IsTrue(offset >= 0);
            if (length <= 0)
            {
                length = bufferView.ByteLength - offset;
            }
            Assert.IsTrue(offset + length <= bufferView.ByteLength);

            var bufferIndex = bufferView.Buffer;
            Assert.IsNotNull(m_Buffers);
            Assert.IsTrue(bufferIndex < m_Buffers.Length);
            Assert.IsTrue(m_Buffers[bufferIndex].IsCreated);

            var chunk = m_BinChunks[bufferIndex];
            var nativeBuffer = m_Buffers[bufferIndex];
            var totalOffset = chunk.Start + bufferView.ByteOffset + offset;
            Assert.IsTrue(bufferView.ByteOffset + offset <= chunk.Length);
            Assert.IsTrue(totalOffset + length <= nativeBuffer.Length);
            return m_Buffers[bufferIndex].GetSubArray(totalOffset, length);
        }

#if MESHOPT_IS_ENABLED
        void MeshoptDecode()
        {
            if (Root.BufferViews != null)
            {
                List<JobHandle> jobHandlesList = null;
                for (var i = 0; i < Root.BufferViews.Count; i++)
                {
                    var bufferView = Root.BufferViews[i];
                    if (bufferView.Extensions?.ExtMeshoptCompression != null)
                    {
                        var meshopt = bufferView.Extensions.ExtMeshoptCompression;
                        if (jobHandlesList == null)
                        {
                            m_MeshoptBufferViews = new Dictionary<int, NativeArray<byte>>();
                            jobHandlesList = new List<JobHandle>(Root.BufferViews.Count);
                            m_MeshoptReturnValues = new NativeArray<int>(Root.BufferViews.Count, Allocator.TempJob);
                        }

                        if (!meshopt.ByteStride.HasValue)
                        {
                            Logger?.Error(LogCode.TypeUnsupported, "Meshopt", "Missing byteStride");
                            continue;
                        }

                        var byteStride = meshopt.ByteStride.Value;

                        var arr = new NativeArray<byte>(meshopt.Count * byteStride, Allocator.Persistent);

                        var origBufferView = GetBufferView(meshopt);

                        var jobHandle = Decode.DecodeGltfBuffer(
                            m_MeshoptReturnValues.GetSubArray(i, 1),
                            arr,
                            meshopt.Count,
                            byteStride,
                            origBufferView.AsNativeArrayReadOnly(),
                            meshopt.Mode.ToMeshoptimizerMode(),
                            meshopt.Filter.ToMeshoptimizerFilter()
                        );
                        jobHandlesList.Add(jobHandle);
                        m_MeshoptBufferViews[i] = arr;
                    }
                }

                if (jobHandlesList != null)
                {
                    using (var jobHandles = new NativeArray<JobHandle>(jobHandlesList.ToArray(), Allocator.Temp))
                    {
                        m_MeshoptJobHandle = JobHandle.CombineDependencies(jobHandles);
                    }
                }
            }
        }

        async Task<bool> WaitForMeshoptDecode()
        {
            var success = true;
            if (m_MeshoptBufferViews != null)
            {
                while (!m_MeshoptJobHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                m_MeshoptJobHandle.Complete();

                foreach (var returnValue in m_MeshoptReturnValues)
                {
                    success &= returnValue == 0;
                }
                m_MeshoptReturnValues.Dispose();
            }
            return success;
        }

#endif // MESHOPT_IS_ENABLED

        async Task<bool> Prepare(CancellationToken cancellationToken)
        {
            m_Resources = new List<UnityEngine.Object>();

            // RedundantAssignment potentially becomes necessary when MESHOPT_IS_ENABLED is not defined
            // ReSharper disable once RedundantAssignment
            var success = true;

#if MESHOPT_IS_ENABLED
            success = await WaitForMeshoptDecode();
            if (!success) return false;
#endif

            if (Root.Accessors != null)
            {
                success = await LoadAccessorData(cancellationToken);
                await DeferAgent.BreakPoint();

                while (!m_AccessorJobsHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                m_AccessorJobsHandle.Complete();
            }
            if (!success) return success;

            if (m_TextureLoadTasks != null)
            {
                m_Textures = new Texture2D[Root.Textures.Count];
                foreach (var textureLoadTask in m_TextureLoadTasks)
                {
                    var result = await textureLoadTask.Value;
                    if (result.Texture is not null)
                    {
                        if (!result.IsYFlipped)
                        {
                            m_NonFlippedYTextureIndices ??= new HashSet<int>(m_TextureLoadTasks.Count);
                            m_NonFlippedYTextureIndices.Add(textureLoadTask.Key);
                        }

                        var textureIndex = textureLoadTask.Key;
                        result.Texture.name = GetTextureName(textureIndex);
                        result.Texture.anisoLevel = m_Settings.AnisotropicFilterLevel;
                        m_Textures[textureIndex] = result.Texture;
                        m_Resources.Add(result.Texture);
                    }
                }
            }

            if (Root.Textures != null)
            {
                PopulateTextureVariants(cancellationToken);
            }

            if (Root.Materials != null)
            {
                await GenerateMaterials(cancellationToken);
            }
            await DeferAgent.BreakPoint();

            if (m_MeshOrders != null)
            {
                await WaitForAllMeshGenerators(cancellationToken);
                await DeferAgent.BreakPoint();

                await AssignAllAccessorData(cancellationToken);

                success = await CreateAllMeshAssignments(cancellationToken);
            }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
            if (Root.HasAnimation)
            {
                if (m_Settings.NodeNameMethod != NameImportMethod.OriginalUnique)
                {
                    Logger?.Info(LogCode.NamingOverride);
                    m_Settings.NodeNameMethod = NameImportMethod.OriginalUnique;
                }
            }
#endif

            int[] parentIndex = null;

            var skeletonMissing = Root.IsASkeletonMissing();

            if (Root.Nodes != null && Root.Nodes.Count > 0)
            {
                if (m_Settings.NodeNameMethod == NameImportMethod.OriginalUnique)
                {
                    parentIndex = CreateUniqueNames();
                }
                else if (skeletonMissing)
                {
                    parentIndex = GetParentIndices();
                }
                if (skeletonMissing)
                {
                    CalculateSkinSkeletons(parentIndex);
                }
            }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
            if (Root.HasAnimation && m_Settings.AnimationMethod != AnimationMethod.None)
            {
                CreateAnimationClips(parentIndex);
            }
#endif

            DisposeVolatileAccessorData();
            return success;
        }

#if UNITY_ANIMATION || GLTFAST_ANIMATION
        void CreateAnimationClips(int[] parentIndex)
        {
            var processorFactory = m_Addons?.First<IAnimationProcessorFactory>();
            if (processorFactory == null)
            {
#if UNITY_ANIMATION
                processorFactory = new AnimationModuleProcessorFactory(
                    m_Settings.AnimationMethod == AnimationMethod.Legacy);
#else
                // Animation module disabled and no animation addon found, nothing to do here.
                return;
#endif
            }

            using var animationAddons = processorFactory.CreateAnimationProcessor(Root.Animations.Count);
            for (var i = 0; i < Root.Animations.Count; i++)
            {
                var animation = Root.Animations[i];
                var clipName = animation.Name ?? $"Clip_{i}";

                animationAddons.AddClip(i, clipName);

                for (var j = 0; j < animation.Channels.Count; j++)
                {
                    var channel = animation.Channels[j];
                    if (channel.Sampler < 0 || channel.Sampler >= animation.Samplers.Count)
                    {
                        Logger?.Error(LogCode.AnimationChannelSamplerInvalid, j.ToString());
                        continue;
                    }
                    var sampler = animation.Samplers[channel.Sampler];
                    if (sampler == null || sampler.Output < 0 || sampler.Output >= Root.Accessors.Count)
                    {
                        Logger?.Error(LogCode.AnimationChannelSamplerInvalid, j.ToString());
                        continue;
                    }
                    if (channel.Target?.Node is not int targetNode || targetNode < 0 || targetNode >= Root.Nodes.Count)
                    {
                        Logger?.Error(LogCode.AnimationChannelNodeInvalid, j.ToString());
                        continue;
                    }


                    var nodeHierarchyInfo = new NodeHierarchyInfo(m_NodeNames, parentIndex);
                    var times = GetAccessorData<float>(sampler.Input);
                    if (!times.IsCreated)
                    {
                        Logger?.Error(LogCode.AccessorAccessFailed, sampler.Input.ToString());
                        continue;
                    }
                    var interpolation = sampler.Interpolation.Value;

                    switch (channel.Target.Path.Value)
                    {
                        case AnimationPath.Translation:
                        {
                            var values = GetAccessorData<float3>(sampler.Output);
                            if (!values.IsCreated)
                            {
                                Logger?.Error(LogCode.AccessorAccessFailed, sampler.Output.ToString());
                                continue;
                            }
                            animationAddons.AddTranslationCurves(
                                i,
                                targetNode,
                                nodeHierarchyInfo,
                                times,
                                values,
                                interpolation
                            );
                            break;
                        }
                        case AnimationPath.Rotation:
                        {
                            var values = GetAccessorData<quaternion>(sampler.Output);
                            if (!values.IsCreated)
                            {
                                Logger?.Error(LogCode.AccessorAccessFailed, sampler.Output.ToString());
                                continue;
                            }
                            animationAddons.AddRotationCurves(
                                i,
                                targetNode,
                                nodeHierarchyInfo,
                                times,
                                values,
                                interpolation
                            );
                            break;
                        }
                        case AnimationPath.Scale:
                        {
                            var values = GetAccessorData<float3>(sampler.Output);
                            if (!values.IsCreated)
                            {
                                Logger?.Error(LogCode.AccessorAccessFailed, sampler.Output.ToString());
                                continue;
                            }
                            animationAddons.AddScaleCurves(
                                i,
                                targetNode,
                                nodeHierarchyInfo,
                                times,
                                values,
                                interpolation
                            );
                            break;
                        }
                        case AnimationPath.Weights:
                        {
                            var node = Root.Nodes[targetNode];
                            if (!node.Mesh.HasValue || node.Mesh.Value >= Root.Meshes.Count)
                            {
                                break;
                            }
                            var mesh = Root.Meshes[node.Mesh.Value];
                            var values = GetAccessorData<float>(sampler.Output);
                            if (!values.IsCreated)
                            {
                                Logger?.Error(LogCode.AccessorAccessFailed, sampler.Output.ToString());
                                continue;
                            }
                            animationAddons.AddMorphTargetWeightCurves(
                                i,
                                targetNode,
                                0,
                                null,
                                nodeHierarchyInfo,
                                times,
                                values,
                                interpolation,
                                mesh.Extras?.TargetNames
                            );

                            // HACK BEGIN:
                            // Meshes with multiple primitives that are not using identical vertex buffer layouts
                            // are split up into separate Unity Meshes. Because of this, we have to duplicate the
                            // animation curves, so that all primitives are animated.
                            // TODO: Refactor primitive sub-meshing and remove this hack
                            // https://github.com/atteneder/glTFast/issues/153
                            var meshPrefix = string.IsNullOrEmpty(mesh.Name) ? k_PrimitiveName : mesh.Name;
                            var meshCount = m_MeshAssignments.GetLength(node.Mesh.Value);
                            for (var meshNumeration = 1; meshNumeration < meshCount; meshNumeration++)
                            {
                                var meshName = $"{meshPrefix}_{meshNumeration}";
                                animationAddons.AddMorphTargetWeightCurves(
                                    i,
                                    targetNode,
                                    meshNumeration,
                                    meshName,
                                    nodeHierarchyInfo,
                                    times,
                                    values,
                                    interpolation,
                                    mesh.Extras?.TargetNames
                                );
                            }
                            // HACK END
                            break;
                        }
                        case AnimationPath.Pointer:
                            Logger?.Warning(LogCode.AnimationTargetPathUnsupported, channel.Target.Path.ToString());
                            break;
                        case AnimationPath.Undefined:
                        default:
                            Logger?.Error(LogCode.AnimationTargetPathUnsupported, channel.Target.Path.ToString());
                            break;
                    }
                }
            }
            var instantiationFactory = animationAddons.Complete();
            AddDataInstanceApplierFactory(instantiationFactory);
        }

#endif // UNITY_ANIMATION

        void CalculateSkinSkeletons(int[] parentIndex)
        {
            foreach (var skin in Root.Skins)
            {
                if (skin.Skeleton < 0)
                {
                    skin.Skeleton = GetLowestCommonAncestorNode(skin.Joints, parentIndex);
                }
            }
        }

        void DisposeVolatileAccessorData()
        {
            // Dispose all accessor data buffers, except the ones needed for instantiation
            if (m_AccessorData != null)
            {
                for (var index = 0; index < m_AccessorData.Length; index++)
                {
                    if ((m_AccessorUsage[index] & AccessorUsage.RequiredForInstantiation) == 0)
                    {
                        m_AccessorData[index]?.Dispose();
                        m_AccessorData[index] = null;
                    }
                }
            }
        }

        async Task<bool> CreateAllMeshAssignments(CancellationToken cancellationToken)
        {
            foreach (var meshOrder in m_MeshOrders)
            {
                cancellationToken.ThrowIfCancellationRequestedWithTracking();

                var mesh = await meshOrder.generator.CreateMeshResult(cancellationToken);
                if (!ReferenceEquals(mesh, null))
                {
                    foreach (var meshSubset in meshOrder.Recipients)
                    {
                        var uMesh = new MeshAssignment(mesh, meshSubset.primitives);
                        m_MeshAssignments.SetValue(
                            meshSubset.meshIndex,
                            meshSubset.meshNumeration,
                            uMesh
                        );
                    }
                    m_Meshes.Add(mesh);
                }
                else
                {
                    return false;
                }

                meshOrder.Dispose();
                await DeferAgent.BreakPoint();
            }

            m_MeshOrders = null;
            return true;
        }

        async Task WaitForAllMeshGenerators(CancellationToken cancellationToken)
        {
            foreach (var meshOrder in m_MeshOrders)
            {
                if (meshOrder.generator == null) continue;
                while (!meshOrder.generator.IsCompleted)
                {
                    // TODO fix resource disposal when calling DisposeVolatileData() while jobs are queued/running
                    // cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    await Task.Yield();
                }
            }
        }

        async Task GenerateMaterials(CancellationToken cancellationToken)
        {
            m_Materials = new UnityEngine.Material[Root.Materials.Count];
            for (var i = 0; i < m_Materials.Length; i++)
            {
                // TODO fix resource disposal when calling DisposeVolatileData() while jobs are queued/running
                // cancellationToken.ThrowIfCancellationRequestedWithTracking();

                await DeferAgent.BreakPoint(.0001f);
                Profiler.BeginSample("GenerateMaterial");
                m_MaterialGenerator.SetLogger(m_Context.Logger);
                var pointsSupport = GetMaterialPointsSupport(i);
                var material = m_MaterialGenerator.GenerateMaterial(
                    Root.Materials[i],
                    this,
                    pointsSupport
                );
                m_Materials[i] = material;
                m_MaterialGenerator.SetLogger(null);
                Profiler.EndSample();
            }
        }

        void PopulateTextureVariants(CancellationToken cancellationToken)
        {
            if (m_TextureResampleMap != null)
            {
                // TODO fix resource disposal when calling DisposeVolatileData() while jobs are queued/running
                // cancellationToken.ThrowIfCancellationRequestedWithTracking();
                foreach (var (textureIndex, originalTextureIndex) in m_TextureResampleMap)
                {
                    var txt = Root.Textures[textureIndex];
                    var originalTexture = m_Textures[originalTextureIndex];

                    if (originalTexture is null)
                    {
                        continue;
                    }
                    if (originalTexture.isReadable)
                    {
                        Sampler sampler = null;
                        if (Root.Samplers != null && txt.Sampler is int samplerIndex && samplerIndex < Root.Samplers.Count)
                        {
                            sampler = Root.Samplers[samplerIndex];
                        }

                        sampler ??= new Sampler();

                        var texture = UnityEngine.Object.Instantiate(originalTexture);
#if DEBUG
                        texture.name = $"{originalTexture.name}_sampler{txt.Sampler}";
                        Logger?.Warning(LogCode.ImageMultipleSamplers, GetImageIndexFromTexture(txt, textureIndex).ToString());
#endif
                        sampler.Apply(texture, m_Settings.DefaultMinFilterMode, m_Settings.DefaultMagFilterMode);
                        m_Resources.Add(texture);
                        m_Textures[textureIndex] = texture;
                    }
                    else
                    {
                        Logger?.Warning(
                            LogCode.TextureSamplerNotApplied,
                            txt.Sampler >= 0 ? $"#{txt.Sampler}" : "default",
                            textureIndex.ToString(),
                            txt.GetImageIndex().ToString()
                        );
                        m_Textures[textureIndex] = originalTexture;
                    }
                    if (m_NonFlippedYTextureIndices != null && m_NonFlippedYTextureIndices.Contains(originalTextureIndex))
                    {
                        m_NonFlippedYTextureIndices.Add(textureIndex);
                    }
                }
            }

            if (m_TextureReuseMap != null)
            {
                foreach (var (textureIndex, existingTextureIndex) in m_TextureReuseMap)
                {
                    var originalTexture = m_Textures[existingTextureIndex];
                    m_Textures[textureIndex] = originalTexture;
                    if (m_NonFlippedYTextureIndices != null && m_NonFlippedYTextureIndices.Contains(existingTextureIndex))
                    {
                        m_NonFlippedYTextureIndices.Add(textureIndex);
                    }
                }
            }
        }

        int? GetImageIndexFromTexture(Texture txt, int textureIndex)
        {
            if (m_TextureImageOverrides != null && m_TextureImageOverrides.TryGetValue(textureIndex, out var mappedImageIndex))
            {
                return mappedImageIndex;
            }
            return txt.GetImageIndex();
        }

        void SetMaterialPointsSupport(int materialIndex)
        {
            Assert.IsNotNull(Root?.Materials);
            Assert.IsTrue(materialIndex >= 0);
            Assert.IsTrue(materialIndex < Root.Materials.Count);
            if (m_MaterialPointsSupport == null)
            {
                m_MaterialPointsSupport = new HashSet<int>();
            }
            m_MaterialPointsSupport.Add(materialIndex);
        }

        bool GetMaterialPointsSupport(int materialIndex)
        {
            if (m_MaterialPointsSupport != null)
            {
                Assert.IsNotNull(Root?.Materials);
                Assert.IsTrue(materialIndex >= 0);
                Assert.IsTrue(materialIndex < Root.Materials.Count);
                return m_MaterialPointsSupport.Contains(materialIndex);
            }
            return false;
        }

        /// <summary>
        /// glTF nodes have no requirement to be named or have specific names.
        /// Some Unity systems like animation and importers require unique
        /// names for Nodes with the same parent. For each node this method creates
        /// names that are:
        /// - Not empty
        /// - Unique amongst nodes with identical parent node
        /// </summary>
        /// <returns>Array containing each node's parent node index (or -1 for root nodes)</returns>
        int[] CreateUniqueNames()
        {
            m_NodeNames = new string[Root.Nodes.Count];
            var parentIndex = new int[Root.Nodes.Count];

            for (var nodeIndex = 0; nodeIndex < Root.Nodes.Count; nodeIndex++)
            {
                parentIndex[nodeIndex] = -1;
            }

            var childNames = new HashSet<string>();

            for (var nodeIndex = 0; nodeIndex < Root.Nodes.Count; nodeIndex++)
            {
                var node = Root.Nodes[nodeIndex];
                if (node.Children != null)
                {
                    childNames.Clear();
                    foreach (var child in node.Children)
                    {
                        parentIndex[child] = nodeIndex;
                        m_NodeNames[child] = GetUniqueNodeName(Root, child, childNames);
                    }
                }
            }

            for (int sceneId = 0; sceneId < Root.Scenes.Count; sceneId++)
            {
                childNames.Clear();
                var scene = Root.Scenes[sceneId];
                if (scene.Nodes != null)
                {
                    foreach (var nodeIndex in scene.Nodes)
                    {
                        m_NodeNames[nodeIndex] = GetUniqueNodeName(Root, nodeIndex, childNames);
                    }
                }
            }

            return parentIndex;
        }

        static string GetUniqueNodeName(Root gltf, uint index, ICollection<string> excludeNames)
        {
            if (gltf.Nodes == null || index >= gltf.Nodes.Count) return null;
            var name = gltf.Nodes[(int)index].Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                if (gltf.Nodes[(int)index].Mesh is int meshIndex)
                {
                    name = gltf.Meshes[meshIndex].Name;
                }
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Node-{index}";
            }

            if (excludeNames != null)
            {
                if (excludeNames.Contains(name))
                {
                    var i = 0;
                    string extName;
                    do
                    {
                        extName = $"{name}_{i++}";
                    } while (excludeNames.Contains(extName));
                    excludeNames.Add(extName);
                    return extName;
                }
                excludeNames.Add(name);
            }
            return name;
        }

        async Task DisposeTextureLoadTasks()
        {
            if (m_TextureLoadTasks != null)
            {
                foreach (var textureLoadTask in m_TextureLoadTasks)
                {
                    var result = await textureLoadTask.Value;
                    if (result.Texture is not null)
                    {
                        DestroyUtils.SafeDestroy(result.Texture);
                    }
                }

                m_TextureLoadTasks = null;
            }
        }

        /// <summary>
        /// Free up volatile loading resources
        /// </summary>
        void DisposeVolatileData()
        {
            m_Buffers = null;
            m_BinChunks = null;

            if (m_VolatileDisposables != null)
            {
                foreach (var disposable in m_VolatileDisposables)
                {
                    disposable?.Dispose();
                }
                m_VolatileDisposables = null;
            }

            m_BufferLoadTasks = null;

            m_AccessorUsage = null;

            m_TextureLoadTasks = null;
            m_TextureImageOverrides = null;

            m_GlbBinChunk = null;
            m_MaterialPointsSupport = null;

#if MESHOPT_IS_ENABLED
            if (m_MeshoptBufferViews != null)
            {
                foreach (var nativeBuffer in m_MeshoptBufferViews.Values)
                {
                    nativeBuffer.Dispose();
                }
                m_MeshoptBufferViews = null;
            }
            if (m_MeshoptReturnValues.IsCreated)
            {
                m_MeshoptReturnValues.Dispose();
            }
#endif
        }

        void AddDataInstanceApplierFactory(IDataInstanceApplierFactory factory)
        {
            if (factory == null)
                return;
            (m_DataInstanceApplierFactories ??= new List<IDataInstanceApplierFactory>()).Add(factory);
        }

        async Task InstantiateSceneInternal(IInstantiator instantiator, int sceneId, CancellationToken cancellationToken)
        {
            m_Addons?.ForEach(addon => addon.Inject(instantiator));

            // TODO: Temporary solution to maintain IInstantiator.AddAnimation behavior.
            // Refactor this when a better (public and generic) method of instantiation add-on integration comes around.
            List<IPostBeginSceneInstanceApplier> postBeginSceneAppliers = null;

            if (m_DataInstanceApplierFactories != null)
            {
                foreach (var factory in m_DataInstanceApplierFactories)
                {
                    var applier = factory.CreateInstanceApplier(instantiator);
                    if (applier is IPostBeginSceneInstanceApplier postBeginSceneApplier)
                    {
                        (postBeginSceneAppliers ??= new List<IPostBeginSceneInstanceApplier>())
                            .Add(postBeginSceneApplier);
                    }
                }
            }

            async Task IterateNodes(uint nodeIndex, uint? parentIndex, Action<uint, uint?> callback)
            {
                var node = this.Root.Nodes[(int)nodeIndex];
                callback(nodeIndex, parentIndex);
                await DeferAgent.BreakPoint();
                if (node.Children != null)
                {
                    foreach (var child in node.Children)
                    {
                        cancellationToken.ThrowIfCancellationRequestedWithTracking();
                        await IterateNodes(child, nodeIndex, callback);
                    }
                }
            }

            void CreateHierarchy(uint nodeIndex, uint? parentIndex)
            {

                Profiler.BeginSample("CreateHierarchy");
                var node = this.Root.Nodes[(int)nodeIndex];
                node.GetTransform(out var position, out var rotation, out var scale);
                instantiator.CreateNode(nodeIndex, parentIndex, position, rotation, scale);

                var nodeName = m_NodeNames == null ? node.Name : m_NodeNames[nodeIndex];
                if (nodeName == null && node.Mesh.HasValue)
                {
                    // Fallback name for Node is first valid Mesh name
                    foreach (var meshAssignment in m_MeshAssignments.Values(node.Mesh.Value))
                    {
                        var mesh = meshAssignment.mesh;
                        if (!string.IsNullOrEmpty(mesh.name))
                        {
                            nodeName = mesh.name;
                            break;
                        }
                    }
                }

                instantiator.SetNodeName(nodeIndex, nodeName);
                Profiler.EndSample();
            }

            void PopulateHierarchy(uint nodeIndex, uint? parentIndex)
            {

                Profiler.BeginSample("PopulateHierarchy");
                var node = this.Root.Nodes[(int)nodeIndex];

                if (node.Mesh.HasValue)
                {
                    var meshNumeration = 0;
                    foreach (var meshAssignment in m_MeshAssignments.Values(node.Mesh.Value))
                    {
                        cancellationToken.ThrowIfCancellationRequestedWithTracking();

                        var mesh = meshAssignment.mesh;
                        var meshName = string.IsNullOrEmpty(mesh.name) ? null : mesh.name;
                        IReadOnlyList<uint> joints = null;
                        uint? rootJoint = null;

                        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendIndices))
                        {
                            if (node.Skin is int skinIndex)
                            {
                                var skin = Root.Skins[skinIndex];
                                // TODO: see if this can be moved to mesh creation phase / before instantiation
                                mesh.bindposes = GetBindPoses(skinIndex);
                                if (skin.Skeleton is int skeletonIndex)
                                {
                                    rootJoint = (uint)skeletonIndex;
                                }
                                joints = skin.Joints;
                            }
                            else
                            {
                                Logger?.Warning(LogCode.SkinMissing);
                            }
                        }

                        var meshInstancing = node.Extensions?.MeshGpuInstancing;

                        var meshResultName =
                            meshNumeration > 0
                                ? $"{meshName ?? k_PrimitiveName}_{meshNumeration}"
                                : meshName ?? k_PrimitiveName;

                        var meshResult = new MeshResult(
                            node.Mesh.Value,
                            meshAssignment.primitives,
                            GetMaterialIndices(node.Mesh.Value, meshAssignment.primitives),
                            meshAssignment.mesh
                        );

                        if (meshInstancing == null)
                        {
                            instantiator.AddPrimitive(
                                nodeIndex,
                                meshResultName,
                                meshResult,
                                joints,
                                rootJoint,
                                node.Weights ?? Root.Meshes[node.Mesh.Value].Weights,
                                meshNumeration
                            );
                        }
                        else
                        {

                            var hasTranslations = meshInstancing.Attributes.Translation.HasValue;
                            var hasRotations = meshInstancing.Attributes.Rotation.HasValue;
                            var hasScales = meshInstancing.Attributes.Scale.HasValue;

                            NativeArray<Vector3>? positions = null;
                            NativeArray<Quaternion>? rotations = null;
                            NativeArray<Vector3>? scales = null;
                            uint instanceCount = 0;

                            if (hasTranslations)
                            {
                                positions = ((NativeArray<float3>)m_AccessorData[meshInstancing.Attributes.Translation.Value]).Reinterpret<Vector3>();
                                instanceCount = (uint)positions.Value.Length;
                            }

                            if (hasRotations)
                            {
                                rotations = ((NativeArray<quaternion>)m_AccessorData[meshInstancing.Attributes.Rotation.Value]).Reinterpret<Quaternion>();
                                instanceCount = (uint)rotations.Value.Length;
                            }

                            if (hasScales)
                            {
                                scales = ((NativeArray<float3>)m_AccessorData[meshInstancing.Attributes.Scale.Value]).Reinterpret<Vector3>();
                                instanceCount = (uint)scales.Value.Length;
                            }

                            instantiator.AddPrimitiveInstanced(
                                nodeIndex,
                                meshResultName,
                                meshResult,
                                instanceCount,
                                positions,
                                rotations,
                                scales,
                                meshNumeration
                            );
                        }

                        meshNumeration++;
                    }
                }

                if (node.Camera is int cameraIndex
                    && Root.Cameras != null
                    && cameraIndex < Root.Cameras.Count
                    )
                {
                    instantiator.AddCamera(nodeIndex, (uint)cameraIndex);
                }

                if (node.Extensions?.LightsPunctual != null && Root.Extensions?.LightsPunctual?.Lights != null)
                {
                    if (node.Extensions.LightsPunctual.Light is int lightIndex
                        && lightIndex < Root.Extensions.LightsPunctual.Lights.Count)
                    {
                        instantiator.AddLightPunctual(nodeIndex, (uint)lightIndex);
                    }
                }

                Profiler.EndSample();
            }

            var scene = this.Root.Scenes[sceneId];

            instantiator.BeginScene(scene.Name, scene.Nodes);

            postBeginSceneAppliers?.ForEach(applier => applier.PostBeginScene());

            if (scene.Nodes != null)
            {
                foreach (var nodeId in scene.Nodes)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    await IterateNodes(nodeId, null, CreateHierarchy);
                }
                foreach (var nodeId in scene.Nodes)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();
                    await IterateNodes(nodeId, null, PopulateHierarchy);
                }
            }

            instantiator.EndScene(scene.Nodes);
        }

        /// <summary>
        /// Given a set of nodes in a hierarchy, this method finds the
        /// lowest common ancestor node.
        /// </summary>
        /// <param name="nodes">Set of nodes</param>
        /// <param name="parentIndex">Dictionary of nodes' parent indices</param>
        /// <returns>Lowest common ancestor node of all provided nodes. -1 if it was not found</returns>
        static int GetLowestCommonAncestorNode(IEnumerable<uint> nodes, IReadOnlyList<int> parentIndex)
        {

            List<int> chain = null;
            var commonAncestor = -1;

            bool CompareTo(int nodeId)
            {
                var nodeChain = new List<int>();

                var currNodeId = nodeId;

                while (currNodeId >= 0)
                {
                    if (currNodeId == commonAncestor)
                    {
                        return true;
                    }
                    nodeChain.Insert(0, currNodeId);
                    currNodeId = parentIndex[currNodeId];
                }

                if (chain == null)
                {
                    chain = nodeChain;
                }
                else
                {
                    var depth = math.min(chain.Count, nodeChain.Count);
                    for (var i = 0; i < depth; i++)
                    {
                        if (chain[i] != nodeChain[i])
                        {
                            if (i > 0)
                            {
                                chain.RemoveRange(i, chain.Count - i);
                                break;
                            }
                            return false;
                        }
                    }
                }

                commonAncestor = chain[chain.Count - 1];
                return true;
            }

            foreach (var nodeId in nodes)
            {
                if (!CompareTo((int)nodeId))
                {
                    return -1;
                }
            }

            // foreach (var nodeId in nodes) {
            //     if (commonAncestor == nodeId) {
            //         // A joint cannot be the root, so use its parent instead
            //         commonAncestor = parentIndex[commonAncestor];
            //         break;
            //     }
            // }

            return commonAncestor;
        }

        int[] GetParentIndices()
        {
            var parentIndex = new int[Root.Nodes.Count];
            for (var i = 0; i < parentIndex.Length; i++)
            {
                parentIndex[i] = -1;
            }

            for (var i = 0; i < Root.Nodes.Count; i++)
            {
                if (Root.Nodes[i].Children != null)
                {
                    foreach (var child in Root.Nodes[i].Children)
                    {
                        parentIndex[child] = i;
                    }
                }
            }

            return parentIndex;
        }

        int?[] GetMaterialIndices(int meshIndex, IReadOnlyList<int> primitiveIndices)
        {
            var result = new int?[primitiveIndices.Count];
            for (var subMesh = 0; subMesh < primitiveIndices.Count; subMesh++)
            {
                var primitiveIndex = primitiveIndices[subMesh];
                var primitive = GetSourceMeshPrimitive(meshIndex, primitiveIndex);
                result[subMesh] = primitive.Material;
            }

            return result;
        }

        string GetTextureName(int textureIndex)
        {
            var texture = Root.Textures[textureIndex];

            // For compatibility reasons textures are named after their glTF image.
            // TODO: Change name to glTF texture name in the next major release.
            if (GetImageIndexFromTexture(texture, textureIndex) is int imageIndex)
            {
                var image = GetSourceImage(imageIndex);
                if (image != null)
                {
                    return string.IsNullOrEmpty(image.Name) ? $"image_{imageIndex}" : image.Name;
                }
            }

            return string.IsNullOrEmpty(texture.Name) ? $"texture_{textureIndex}" : texture.Name;
        }

        /// <summary>Is called when retrieving data from accessors should be performed/started.</summary>
        public event Action LoadAccessorDataEvent;

        /// <summary>Is called when a mesh and its primitives are assigned to a <see cref="MeshResult"/> and
        /// sub-meshes. Parameters are MeshResult index, mesh index and per sub-mesh primitive index</summary>
        public event Action<int, int, int[]> MeshResultAssigned;

        async Task<bool> LoadAccessorData(CancellationToken cancellationToken)
        {
            // TODO fix resource disposal when calling DisposeVolatileData() while jobs are queued/running

            Profiler.BeginSample("LoadAccessorData.Init");

#if DEBUG
            // Detect and report poor shared accessor usage. Since this adds performance overhead, it's done in debug
            // mode only.
            var perPrimitiveSetIndices = new Dictionary<IReadOnlyList<MeshPrimitive>, int?[]>(
                comparer: new PrimitivesComparer());
#endif

            // Iterate all primitive vertex attributes and remember the accessors usage.
            m_AccessorUsage = new AccessorUsage[Root.Accessors.Count];

            LoadAccessorDataEvent?.Invoke();

            var meshCount = Root.Meshes?.Count ?? 0;
            int[] meshAssignmentIndices = null;
            if (meshCount > 0)
            {
                m_MeshOrders = new List<MeshOrder>();
                meshAssignmentIndices = new int[meshCount + 1];
                meshAssignmentIndices[0] = 0;
            }
            var meshAssignmentCounter = 0;
            var primitiveSets = new Dictionary<IReadOnlyList<MeshPrimitive>, MeshOrder>(s_MeshComparer);
            for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
            {
                var mesh = Root.Meshes[meshIndex];
                // TODO: Optimized path for single primitive meshes!
                var clusteredPrimitives = new Dictionary<VertexBufferDescriptor, PrimitiveSet>();

                for (var primIndex = 0; primIndex < mesh.Primitives.Count; primIndex++)
                {
                    var primitive = mesh.Primitives[primIndex];
                    {
                        var vertexBufferDesc = VertexBufferDescriptor.FromPrimitive(primitive);
                        if (!clusteredPrimitives.ContainsKey(vertexBufferDesc))
                        {
                            clusteredPrimitives[vertexBufferDesc] = new PrimitiveSet();
                        }
                        clusteredPrimitives[vertexBufferDesc].Add(primIndex, primitive);
                    }

                    if (primitive.Indices >= 0)
                    {
                        AccessorUsage usage;
#if DRACO_IS_ENABLED
                        if (primitive.IsDracoCompressed)
                        {
                            usage = AccessorUsage.Ignore;
                        }
                        else
#endif
                        {
                            usage = primitive.Mode == PrimitiveMode.Triangles
                                ? AccessorUsage.IndexFlipped
                                : AccessorUsage.Index;
                        }
                        SetAccessorUsage(primitive.Indices.Value, usage);
                    }

                    if (primitive.Material is int materialIndex)
                    {
                        if (Root.Materials != null && primitive.Mode == PrimitiveMode.Points)
                        {
                            SetMaterialPointsSupport(materialIndex);
                        }
                    }
                    else
                    {
                        m_DefaultMaterialPointsSupport |= primitive.Mode == PrimitiveMode.Points;
                    }
                }

                var meshNumeration = 0;
                foreach (var primitiveCluster in clusteredPrimitives)
                {
                    var primitiveSet = primitiveCluster.Value;
#if DEBUG
                    if (perPrimitiveSetIndices != null && CheckVertexBufferUsage(perPrimitiveSetIndices, primitiveSet))
                    {
                        // Poor accessor sharing has been detected and logged.
                        // Unset perPrimitiveSetIndices to prevent redundant logging.
                        perPrimitiveSetIndices = null;
                    }
#endif
                    int[] primIndexArray;
                    if (primitiveSets.TryGetValue(primitiveSet.Primitives, out var meshOrder))
                    {
                        primitiveSet.BuildAndDispose(out primIndexArray, out _);
                        meshOrder.AddRecipient(new MeshSubset(meshIndex, meshNumeration, primIndexArray));
                    }
                    else
                    {
                        meshOrder = CreateMeshOrder(
                            primitiveSet,
                            mesh,
                            meshIndex,
                            meshNumeration,
                            out primIndexArray,
                            out var primitives
                            );
                        primitiveSets[primitives] = meshOrder;
                        m_MeshOrders.Add(meshOrder);
                    }

                    MeshResultAssigned?.Invoke(
                        meshNumeration,
                        meshIndex,
                        primIndexArray
                    );

                    meshNumeration++;
                }

                meshAssignmentCounter += clusteredPrimitives.Count;
                meshAssignmentIndices[meshIndex + 1] = meshAssignmentCounter;
            }

            if (Root.Skins != null)
            {
                m_SkinsInverseBindMatrices = new Matrix4x4[Root.Skins.Count][];
                foreach (var skin in Root.Skins)
                {
                    if (skin.InverseBindMatrices is int ibmIndex)
                    {
                        SetAccessorUsage(ibmIndex, AccessorUsage.InverseBindMatrix);
                    }
                }
            }

            if (Root.Nodes != null)
            {
                foreach (var node in Root.Nodes)
                {
                    var attr = node.Extensions?.MeshGpuInstancing?.Attributes;
                    if (attr != null)
                    {
                        if (attr.Translation is int t)
                        {
                            SetAccessorUsage(t, AccessorUsage.Translation | AccessorUsage.RequiredForInstantiation);
                        }
                        if (attr.Rotation is int r)
                        {
                            SetAccessorUsage(r, AccessorUsage.Rotation | AccessorUsage.RequiredForInstantiation);
                        }
                        if (attr.Scale is int s)
                        {
                            SetAccessorUsage(s, AccessorUsage.Scale | AccessorUsage.RequiredForInstantiation);
                        }
                    }
                }
            }

            if (meshAssignmentIndices != null)
            {
                m_Meshes = new List<UnityEngine.Mesh>();
                m_MeshAssignments = new FlatArray<MeshAssignment>(meshAssignmentIndices);
            }
            var tmpList = new List<JobHandle>();
            Profiler.EndSample();

            var success = true;

#if UNITY_ANIMATION
            if (Root.HasAnimation)
            {
                foreach (var animation in Root.Animations)
                {
                    foreach (var sampler in animation.Samplers)
                    {
                        SetAccessorUsage(sampler.Input, AccessorUsage.AnimationTimes);
                    }

                    foreach (var channel in animation.Channels)
                    {
                        var accessorIndex = animation.Samplers[channel.Sampler].Output;
                        switch (channel.Target.Path.Value)
                        {
                            case AnimationPath.Translation:
                                SetAccessorUsage(accessorIndex, AccessorUsage.Translation);
                                break;
                            case AnimationPath.Rotation:
                                SetAccessorUsage(accessorIndex, AccessorUsage.Rotation);
                                break;
                            case AnimationPath.Scale:
                                SetAccessorUsage(accessorIndex, AccessorUsage.Scale);
                                break;
                            case AnimationPath.Weights:
                                SetAccessorUsage(accessorIndex, AccessorUsage.Weight);
                                break;
                        }
                    }
                }
            }
#endif

            // Retrieve indices data jobified
            m_AccessorData = new IDisposable[Root.Accessors.Count];

            for (int i = 0; i < m_AccessorData.Length; i++)
            {
                Profiler.BeginSample("LoadAccessorData.IndicesMatrixJob");
                var acc = Root.Accessors[i];
                if (acc.BufferView < 0)
                {
                    // Not actual accessor to data
                    // Common for draco meshes
                    // the accessor only holds meta information
                    continue;
                }
                switch (acc.Type.Value)
                {
                    case AccessorType.Matrix4x4 when m_AccessorUsage[i] == AccessorUsage.InverseBindMatrix:
                    {
                        // TODO: Maybe use Matrix4x4[], since Mesh.bindposes only accepts C# arrays.
                        GetMatricesJob(i, out var matrices, out var jh);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = matrices;
                        break;
                    }
                    case AccessorType.Vector3 when (m_AccessorUsage[i] & AccessorUsage.Translation) != 0:
                    {
                        GetVector3Job(i, out var data, out var jh, true);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
                    case AccessorType.Vector4 when (m_AccessorUsage[i] & AccessorUsage.Rotation) != 0:
                    {
                        GetVector4Job(i, out var data, out var jh);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
                    case AccessorType.Vector3 when (m_AccessorUsage[i] & AccessorUsage.Scale) != 0:
                    {
                        GetVector3Job(i, out var data, out var jh, false);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
#if UNITY_ANIMATION
                    case AccessorType.Scalar when m_AccessorUsage[i] == AccessorUsage.AnimationTimes || m_AccessorUsage[i] == AccessorUsage.Weight:
                    {
                        GetScalarJob(i, out var times, out var jh);
                        if (times.HasValue)
                        {
                            m_AccessorData[i] = times.Value;
                        }
                        if (jh.HasValue)
                        {
                            tmpList.Add(jh.Value);
                        }
                        break;
                    }
#endif
                }
                Profiler.EndSample();
                await DeferAgent.BreakPoint();
            }

            Profiler.BeginSample("LoadAccessorData.Schedule");
            NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(tmpList.ToArray(), Allocator.Persistent);
            m_AccessorJobsHandle = JobHandle.CombineDependencies(jobHandles);
            jobHandles.Dispose();
            JobHandle.ScheduleBatchedJobs();

            Profiler.EndSample();
            return success;
        }

#if DEBUG
        bool CheckVertexBufferUsage(
            Dictionary<IReadOnlyList<MeshPrimitive>, int?[]> perAttributeMeshCollection,
            PrimitiveSingle primitiveSingle
        )
        {
            return CheckVertexBufferUsage(perAttributeMeshCollection, new[] { primitiveSingle.Primitive });
        }

        bool CheckVertexBufferUsage(
            Dictionary<IReadOnlyList<MeshPrimitive>, int?[]> perAttributeMeshCollection,
            PrimitiveSet primitiveSet
        )
        {
            return CheckVertexBufferUsage(perAttributeMeshCollection, primitiveSet.Primitives);
        }

        bool CheckVertexBufferUsage(
            Dictionary<IReadOnlyList<MeshPrimitive>, int?[]> perAttributeMeshCollection,
            IReadOnlyList<MeshPrimitive> primitives
            )
        {
            if (perAttributeMeshCollection.TryGetValue(primitives, out var indicesAccessors))
            {
                Assert.AreEqual(primitives.Count, indicesAccessors.Length);
                var conflict = false;
                for (var index = 0; index < indicesAccessors.Length; index++)
                {
                    if (indicesAccessors[index] != primitives[index].Indices)
                    {
                        conflict = true;
                        break;
                    }
                }

                if (conflict)
                {
                    Logger?.Warning(LogCode.AccessorsShared);
                    return true;
                }
            }
            else
            {
                indicesAccessors = new int?[primitives.Count];
                // Original will be disposed, so make a copy.
                var primitiveArray = new MeshPrimitive[primitives.Count];
                for (var i = 0; i < primitives.Count; i++)
                {
                    indicesAccessors[i] = primitives[i].Indices;
                    primitiveArray[i] = primitives[i];
                }
                perAttributeMeshCollection[primitiveArray] = indicesAccessors;
            }

            return false;
        }
#endif

        MeshOrder CreateMeshOrder(
            IPrimitiveSet primitiveSet,
            Mesh mesh,
            int meshIndex,
            int meshNumeration,
            out int[] primIndexArray,
            out MeshPrimitive[] primitives
            )
        {
            var morphTargetNames = primitiveSet.HasMorphTargets
                ? mesh.Extras?.TargetNames
                : null;

            MeshGeneratorBase generator;
            primitiveSet.BuildAndDispose(out primIndexArray, out primitives, out var subMeshes);
            var meshSubset = new MeshSubset(meshIndex, meshNumeration, primIndexArray);
#if DRACO_IS_ENABLED
            if (primitives[0].IsDracoCompressed)
            {
                generator = new DracoMeshGenerator(
                    primitives, morphTargetNames, mesh.Name, this, this, DeferAgent, Logger);
            }
            else
#endif
            {
                generator = new MeshGenerator(
                    primitives, subMeshes, morphTargetNames, mesh.Name, this, this, DeferAgent, Logger);
            }

            var meshOrder = new MeshOrder(generator);
            meshOrder.AddRecipient(meshSubset);

            return meshOrder;
        }

        void SetAccessorUsage(int index, AccessorUsage newUsage)
        {
#if DEBUG
            if (m_AccessorUsage[index] != AccessorUsage.Unknown && newUsage != m_AccessorUsage[index])
            {
                Logger?.Error(LogCode.AccessorInconsistentUsage, m_AccessorUsage[index].ToString(), newUsage.ToString());
            }
#endif
            m_AccessorUsage[index] = newUsage;
        }

        async Task AssignAllAccessorData(CancellationToken cancellationToken)
        {
            if (Root.Skins != null)
            {
                for (int s = 0; s < Root.Skins.Count; s++)
                {
                    cancellationToken.ThrowIfCancellationRequestedWithTracking();

                    Profiler.BeginSample("AssignAllAccessorData.Skin");
                    var skin = Root.Skins[s];
                    if (skin.InverseBindMatrices is int ibmIndex)
                    {
                        m_SkinsInverseBindMatrices[s] =
                            ((NativeArray<float4x4>)m_AccessorData[ibmIndex])
                            .Reinterpret<Matrix4x4>().ToArray();
                    }
                    Profiler.EndSample();
                    await DeferAgent.BreakPoint();
                }
            }
        }

        void GetMatricesJob(int accessorIndex, out NativeArray<float4x4> matrices, out JobHandle? jobHandle)
        {
            Profiler.BeginSample("GetMatricesJob");
            // index
            var accessor = Root.Accessors[accessorIndex];
            var accessorData = ((IGltfBuffers)this).GetBufferView(
                accessor.BufferView.Value,
                out _,
                accessor.ByteOffset,
                accessor.ByteSize
                );

            Profiler.BeginSample("Alloc");
            matrices = new NativeArray<float4x4>(accessor.Count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.Type.Value, AccessorType.Matrix4x4);
            //Assert.AreEqual(accessor.count * GetLength(accessor.typeEnum) * 4 , (int) chunk.length);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Matrix");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.ComponentType)
            {
                case AccessorDataType.Float:
                    var job32 = new ConvertMatricesJob
                    {
                        input = accessorData.Reinterpret<float4x4>().AsNativeArrayReadOnly(),
                        result = matrices
                    };
                    jobHandle = job32.Schedule(accessor.Count, DefaultBatchCount);
                    break;
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.ComponentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        unsafe void GetVector3Job(int accessorIndex, out NativeArray<float3> vectors, out JobHandle? jobHandle, bool flip)
        {
            Profiler.BeginSample("GetVector3Job");
            var accessor = Root.Accessors[accessorIndex];

            Profiler.BeginSample("Alloc");
            vectors = new NativeArray<float3>(accessor.Count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.Type.Value, AccessorType.Vector3);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Vector3");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.ComponentType)
            {
                case AccessorDataType.Float:
                {
                    if (flip)
                    {
                        var accessorData = ((IGltfBuffers)this).GetStridedAccessorData<float3>(
                            accessor.BufferView.Value,
                            accessor.Count,
                            accessor.ByteOffset
                        );
                        var job = new ConvertVector3FloatToFloatJob
                        {
                            input = accessorData,
                            result = vectors
                        };
                        jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                    }
                    else
                    {
                        var accessorData = ((IGltfBuffers)this).GetAccessorData<float3>(
                            accessor.BufferView.Value,
                            accessor.Count,
                            accessor.ByteOffset
                        );
                        var job = new MemCopyJob
                        {
                            input = (float*)accessorData.GetUnsafeReadOnlyPtr(),
                            bufferSize = accessor.Count * 12,
                            result = (float*)vectors.GetUnsafePtr()
                        };
                        jobHandle = job.Schedule();
                    }
                    break;
                }
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.ComponentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        void GetVector4Job(int accessorIndex, out NativeArray<quaternion> vectors, out JobHandle? jobHandle)
        {
            Profiler.BeginSample("GetVector4Job");
            // index
            var accessor = Root.Accessors[accessorIndex];
            var accessorData = ((IGltfBuffers)this).GetBufferView(
                accessor.BufferView.Value,
                out _,
                accessor.ByteOffset,
                accessor.ByteSize
                );

            Profiler.BeginSample("Alloc");
            vectors = new NativeArray<quaternion>(accessor.Count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.Type.Value, AccessorType.Vector4);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Vector4");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.ComponentType)
            {
                case AccessorDataType.Float:
                {
                    var job = new ConvertRotationsFloatToFloatJob
                    {
                        input = accessorData.Reinterpret<float4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                    break;
                }
                case AccessorDataType.Short:
                {
                    var job = new ConvertRotationsInt16ToFloatJob
                    {
                        input = accessorData.Reinterpret<short4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                    break;
                }
                case AccessorDataType.Byte:
                {
                    var job = new ConvertRotationsInt8ToFloatJob
                    {
                        input = accessorData.Reinterpret<sbyte4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                    break;
                }
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.ComponentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

#if UNITY_ANIMATION
        unsafe void GetScalarJob(int accessorIndex, out NativeArray<float>? scalars, out JobHandle? jobHandle)
        {
            Profiler.BeginSample("GetScalarJob");
            scalars = null;
            jobHandle = null;
            var accessor = Root.Accessors[accessorIndex];
            var accessorData = ((IGltfBuffers)this).GetBufferView(
                accessor.BufferView.Value,
                out _,
                accessor.ByteOffset,
                accessor.ByteSize
                );

            Assert.AreEqual(accessor.Type.Value, AccessorType.Scalar);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "scalars");
            }

            if (accessor.ComponentType == AccessorDataType.Float)
            {
                Profiler.BeginSample("CopyAnimationTimes");
                var bufferTimes = accessorData
                    .Reinterpret<float>()
                    .GetSubArray(0, accessor.Count);
                scalars = new NativeArray<float>(bufferTimes.Length, Allocator.Persistent);
                unsafe
                {
                    var job = new MemCopyJob
                    {
                        bufferSize = bufferTimes.Length * sizeof(float),
                        input = bufferTimes.GetUnsafeReadOnlyPtr(),
                        result = scalars.Value.GetUnsafePtr()
                    };
                    jobHandle = job.Schedule();
                }
                Profiler.EndSample();
            }
            else if (accessor.Normalized)
            {
                Profiler.BeginSample("Alloc");
                scalars = new NativeArray<float>(accessor.Count, Allocator.Persistent);
                Profiler.EndSample();

                switch (accessor.ComponentType)
                {
                    case AccessorDataType.Byte:
                    {
                        var job = new ConvertScalarInt8ToFloatNormalizedJob
                        {
                            input = accessorData.Reinterpret<sbyte>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                        break;
                    }
                    case AccessorDataType.UnsignedByte:
                    {
                        var job = new ConvertScalarUInt8ToFloatNormalizedJob
                        {
                            input = accessorData.Reinterpret<byte>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                        break;
                    }
                    case AccessorDataType.Short:
                    {
                        var job = new ConvertScalarInt16ToFloatNormalizedJob
                        {
                            input = accessorData.Reinterpret<short>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                        break;
                    }
                    case AccessorDataType.UnsignedShort:
                    {
                        var job = new ConvertScalarUInt16ToFloatNormalizedJob
                        {
                            input = accessorData.Reinterpret<ushort>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.Count, DefaultBatchCount);
                        break;
                    }
                    default:
                        Logger?.Error(LogCode.AnimationFormatInvalid, accessor.ComponentType.ToString());
                        break;
                }
            }
            else
            {
                // Non-normalized
                Logger?.Error(LogCode.AnimationFormatInvalid, accessor.ComponentType.ToString());
            }
            Profiler.EndSample();
        }

#endif // UNITY_ANIMATION

        Accessor IGltfBuffers.GetAccessor(int index)
        {
            return index < 0 || Root.Accessors == null || index >= Root.Accessors.Count
                ? null
                : Root.Accessors[index];
        }

        /// <summary>
        /// Get glTF accessor and its raw data
        /// </summary>
        /// <param name="index">glTF accessor index</param>
        /// <param name="accessor">De-serialized glTF accessor</param>
        /// <param name="data">Pointer to accessor's data in memory</param>
        /// <param name="byteStride">Element byte stride</param>
        unsafe void IGltfBuffers.GetAccessorAndData(
            int index, out Accessor accessor, out void* data, out int? byteStride)
        {
            accessor = Root.Accessors[index];
            if (!accessor.BufferView.HasValue || accessor.BufferView.Value >= Root.BufferViews.Count)
            {
                data = null;
                byteStride = 0;
                return;
            }
            var bufferViewIndex = accessor.BufferView.Value;
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT_IS_ENABLED
            var meshopt = bufferView.Extensions?.ExtMeshoptCompression;
            if (meshopt != null)
            {
                byteStride = meshopt.ByteStride;
                data = (byte*)m_MeshoptBufferViews[bufferViewIndex].GetUnsafeReadOnlyPtr() + accessor.ByteOffset;
            }
            else
#endif
            {
                byteStride = bufferView.ByteStride;
                var bufferIndex = bufferView.Buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (accessor.ByteOffset + bufferView.ByteOffset + m_BinChunks[bufferIndex].Start);
            }

            // // Alternative that uses NativeArray/Slice
            // var bufferViewData = GetBufferView(bufferView);
            // data =  (byte*)bufferViewData.GetUnsafeReadOnlyPtr() + accessor.byteOffset;
        }

        /// <summary>
        /// Get sparse indices raw data
        /// </summary>
        /// <param name="sparseIndices">glTF sparse indices accessor</param>
        /// <param name="data">Pointer to accessor's data in memory</param>
        public unsafe void GetAccessorSparseIndices(AccessorSparseIndices sparseIndices, out void* data)
        {
            if (sparseIndices.BufferView < 0 || sparseIndices.BufferView >= Root.BufferViews.Count)
            {
                Logger?.Error(LogCode.AccessorAccessFailed, sparseIndices.BufferView.ToString());
                data = null;
                return;
            }
            var bufferView = Root.BufferViews[sparseIndices.BufferView];
#if MESHOPT_IS_ENABLED
            var meshopt = bufferView.Extensions?.ExtMeshoptCompression;
            if (meshopt != null)
            {
                data = (byte*)m_MeshoptBufferViews[sparseIndices.BufferView].GetUnsafeReadOnlyPtr() + sparseIndices.ByteOffset;
            }
            else
#endif
            {
                var bufferIndex = bufferView.Buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (sparseIndices.ByteOffset + bufferView.ByteOffset + m_BinChunks[bufferIndex].Start);
            }
        }

        /// <summary>
        /// Get sparse value raw data
        /// </summary>
        /// <param name="sparseValues">glTF sparse values accessor</param>
        /// <param name="data">Pointer to accessor's data in memory</param>
        public unsafe void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data)
        {
            if (sparseValues.BufferView < 0 || sparseValues.BufferView >= Root.BufferViews.Count)
            {
                Logger?.Error(LogCode.AccessorAccessFailed, sparseValues.BufferView.ToString());
                data = null;
                return;
            }
            var bufferView = Root.BufferViews[sparseValues.BufferView];
#if MESHOPT_IS_ENABLED
            var meshopt = bufferView.Extensions?.ExtMeshoptCompression;
            if (meshopt != null)
            {
                data = (byte*)m_MeshoptBufferViews[sparseValues.BufferView].GetUnsafeReadOnlyPtr() + sparseValues.ByteOffset;
            }
            else
#endif
            {
                var bufferIndex = bufferView.Buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (sparseValues.ByteOffset + bufferView.ByteOffset + m_BinChunks[bufferIndex].Start);
            }
        }


#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            // Reset static state
            s_DefaultDeferAgent = null;
            s_MeshComparer = new();
        }
#endif // UNITY_EDITOR


        /// <inheritdoc/>
        public NativeArray<T>.ReadOnly GetAccessorData<T>(int accessorIndex) where T : unmanaged
        {
            if (accessorIndex < 0 || Root?.Accessors == null || accessorIndex >= Root.Accessors.Count)
            {
                return default;
            }
            var data = m_AccessorData[accessorIndex] is NativeArray<T>
                ? (NativeArray<T>)m_AccessorData[accessorIndex]
                : default;
            return data.AsReadOnly();
        }
    }
}
