// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if !UNITY_WEBGL || UNITY_EDITOR
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

// #define MEASURE_TIMINGS

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Text;
using GLTFast.Addons;
using GLTFast.Jobs;
#if KTX_IS_ENABLED
using KtxUnity;
#endif
#if MESHOPT
using Meshoptimizer;
#endif
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GLTFast
{

    using Loading;
    using Logging;
    using Materials;
    using Schema;

    /// <summary>
    /// Loads a glTF's content, converts it to Unity resources and is able to
    /// feed it to an <see cref="IInstantiator"/> for instantiation.
    /// Uses the efficient and fast JsonUtility/<see cref="GltfJsonUtilityParser"/> for JSON parsing.
    /// </summary>
    public class GltfImport : GltfImportBase<Root>
    {
        static GltfJsonUtilityParser s_Parser;

        /// <inheritdoc cref="GltfImportBase(IDownloadProvider,IDeferAgent,IMaterialGenerator,ICodeLogger)"/>
        public GltfImport(
            IDownloadProvider downloadProvider = null,
            IDeferAgent deferAgent = null,
            IMaterialGenerator materialGenerator = null,
            ICodeLogger logger = null
        ) : base(downloadProvider, deferAgent, materialGenerator, logger) { }

        /// <inheritdoc />
        protected override RootBase ParseJson(string json)
        {
            s_Parser ??= new GltfJsonUtilityParser();
            return s_Parser.ParseJson(json);
        }
    }

    /// <inheritdoc cref="GltfImportBase"/>
    /// <typeparam name="TRoot">Root schema class to use for de-serialization.</typeparam>
    public abstract class GltfImportBase<TRoot> : GltfImportBase, IGltfReadable<TRoot>
        where TRoot : RootBase
    {
        /// <inheritdoc cref="GltfImportBase(IDownloadProvider,IDeferAgent,IMaterialGenerator,ICodeLogger)"/>
        public GltfImportBase(
            IDownloadProvider downloadProvider = null,
            IDeferAgent deferAgent = null,
            IMaterialGenerator materialGenerator = null,
            ICodeLogger logger = null
        ) : base(downloadProvider, deferAgent, materialGenerator, logger) { }

        TRoot m_Root;

        /// <inheritdoc />
        protected override RootBase Root
        {
            get => m_Root;
            set => m_Root = (TRoot)value;
        }

        /// <inheritdoc />
        public TRoot GetSourceRoot()
        {
            return m_Root;
        }
    }

    /// <summary>
    /// Loads a glTF's content, converts it to Unity resources and is able to
    /// feed it to an <see cref="IInstantiator"/> for instantiation.
    /// </summary>
    public abstract class GltfImportBase : IGltfReadable, IGltfBuffers, IDisposable
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
        /// <summary>
        /// Base 64 string to byte array decode speed in bytes per second
        /// Measurements based on a MacBook Pro Intel(R) Core(TM) i9-9980HK CPU @ 2.40GHz
        /// and reduced by ~ 20%
        /// </summary>
        const int k_Base64DecodeSpeed =
#if UNITY_EDITOR
            60_000_000;
#else
            150_000_000;
#endif

        const string k_PrimitiveName = "Primitive";

        static readonly HashSet<string> k_SupportedExtensions = new HashSet<string> {
#if DRACO_IS_ENABLED
            ExtensionName.DracoMeshCompression,
#endif
#if KTX_IS_ENABLED
            ExtensionName.TextureBasisUniversal,
#endif // KTX_IS_ENABLED
#if MESHOPT
            ExtensionName.MeshoptCompression,
#endif
            ExtensionName.MaterialsPbrSpecularGlossiness,
            ExtensionName.MaterialsUnlit,
            ExtensionName.MaterialsVariants,
            ExtensionName.TextureTransform,
            ExtensionName.MeshQuantization,
            ExtensionName.MaterialsTransmission,
            ExtensionName.MeshGPUInstancing,
            ExtensionName.LightsPunctual,
            ExtensionName.MaterialsClearcoat,
        };

        static IDeferAgent s_DefaultDeferAgent;
        static MeshComparer s_MeshComparer = new MeshComparer();

        /// <summary>Logger used by this glTF import instance.</summary>
        public ICodeLogger Logger { get; }

        /// <summary>Defer agent used by this glTF import instance.</summary>
        public IDeferAgent DeferAgent { get; }

        IDownloadProvider m_DownloadProvider;
        IMaterialGenerator m_MaterialGenerator;

        Dictionary<Type, ImportAddonInstance> m_ImportInstances;

        ImportSettings m_Settings;

        ReadOnlyNativeArray<byte>[] m_Buffers;
        List<IDisposable> m_VolatileDisposables;

        GlbBinChunk[] m_BinChunks;

        Dictionary<int, Task<IDownload>> m_DownloadTasks;

#if UNITY_IMAGECONVERSION
        Dictionary<int, TextureDownloadBase> m_TextureDownloadTasks;
#if !UNITY_6000_0_OR_NEWER
        List<ImageCreateContext> m_ImageCreateContexts;
#endif // !UNITY_6000_0_OR_NEWER
#endif // UNITY_IMAGECONVERSION
#if KTX_IS_ENABLED
        Dictionary<int,Task<IDownload>> m_KtxDownloadTasks;
        List<KtxLoadContextBase> m_KtxLoadContextsBuffer;
#endif // KTX_IS_ENABLED

        IDisposable[] m_AccessorData;
        AccessorUsage[] m_AccessorUsage;
        JobHandle m_AccessorJobsHandle;

        List<MeshOrder> m_MeshOrders;

        /// <summary>
        /// Loaded glTF images (Raw texture without sampler settings)
        /// <seealso cref="m_Textures"/>
        /// </summary>
        Texture2D[] m_Images;

        /// <summary>
        /// In glTF a texture is an image with a certain sampler setting applied.
        /// So any `images` member is also in `textures`, but not necessary the
        /// other way around.
        /// /// <seealso cref="m_Images"/>
        /// </summary>
        Texture2D[] m_Textures;

#if KTX_IS_ENABLED
        HashSet<int> m_NonFlippedYTextureIndices;
#endif
        ImageFormat[] m_ImageFormats;
#if !UNITY_VISIONOS
        bool[] m_ImageReadable;
#endif
        bool[] m_ImageGamma;

        /// optional glTF-binary buffer
        /// https://github.com/KhronosGroup/glTF/tree/master/specification/2.0#binary-buffer
        GlbBinChunk? m_GlbBinChunk;

#if MESHOPT
        Dictionary<int, NativeArray<byte>> m_MeshoptBufferViews;
        NativeArray<int> m_MeshoptReturnValues;
        JobHandle m_MeshoptJobHandle;
#endif

        /// <summary>
        /// Material IDs of materials that require points topology support.
        /// </summary>
        HashSet<int> m_MaterialPointsSupport;
        bool m_DefaultMaterialPointsSupport;

        /// <summary>Main glTF data structure</summary>
        protected abstract RootBase Root { get; set; }
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
#if UNITY_ANIMATION
        AnimationClip[] m_AnimationClips;
#endif

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
        /// <param name="logger">Provides custom message logging</param>
        public GltfImportBase(
            IDownloadProvider downloadProvider = null,
            IDeferAgent deferAgent = null,
            IMaterialGenerator materialGenerator = null,
            ICodeLogger logger = null
            )
        {
            m_DownloadProvider = downloadProvider ?? new DefaultDownloadProvider();

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
                DeferAgent = s_DefaultDeferAgent;
            }
            else
            {
                DeferAgent = deferAgent;
            }
            m_MaterialGenerator = materialGenerator ?? MaterialGenerator.GetDefaultMaterialGenerator();

            Logger = logger;

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
            if (s_DefaultDeferAgent!=null && s_DefaultDeferAgent != deferAgent) {
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
            if (m_ImportInstances == null)
            {
                m_ImportInstances = new Dictionary<Type, ImportAddonInstance>();
            }
            m_ImportInstances[typeof(T)] = importInstance;
        }

        /// <summary>
        /// Queries the import instance of a particular type.
        /// </summary>
        /// <typeparam name="T">Type of the import instance</typeparam>
        /// <returns>The import instance that was previously added. False if there was none.</returns>
        public T GetImportAddonInstance<T>() where T : ImportAddonInstance
        {
            if (m_ImportInstances == null)
                return null;

            if (m_ImportInstances.TryGetValue(typeof(T), out var addonInstance))
            {
                return (T)addonInstance;
            }

            return null;
        }

        /// <summary>
        /// Load a glTF file (JSON or binary)
        /// The URL can be a file path (using the "file://" scheme) or a web address.
        /// </summary>
        /// <param name="url">Uniform Resource Locator. Can be a file path (using the "file://" scheme) or a web address.</param>
        /// <param name="importSettings">Import Settings (<see cref="ImportSettings"/> for details)</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if loading was mainly successful and no critical error occurred, false otherwise</returns>
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
        public async Task<bool> LoadFile(
            string localPath,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
#if NET_STANDARD_2_1
            await using
#endif
            var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read);
            var result = await LoadStream(fs, uri, importSettings, cancellationToken);
#if !NET_STANDARD_2_1
            fs.Dispose();
#endif
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

            if (cancellationToken.IsCancellationRequested) return false;

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
                using var data = new NativeArray<byte>((int)length, Allocator.Persistent);
                var dataStream = data.ToUnmanagedMemoryStream();
                await dataStream.WriteAsync(firstBytes, cancellationToken);
                await dataStream.WriteAsync(glbHeader, cancellationToken);
                await stream.CopyToAsync(dataStream, (int)(length - dataStream.Position), cancellationToken);
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

            return !cancellationToken.IsCancellationRequested
                && await LoadGltfJson(json, uri, importSettings, cancellationToken);
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
        [Obsolete("Use the generic Load instead.")]
        public async Task<bool> LoadGltfBinary(
            byte[] bytes,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
        )
        {
            var managedNativeArray = new ManagedNativeArray<byte, byte>(bytes);
            m_VolatileDisposables ??= new List<IDisposable>();
            m_VolatileDisposables.Add(managedNativeArray);
            return await LoadGltfBinaryInternal(
                managedNativeArray.nativeArray.AsReadOnly(),
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
        public async Task<bool> LoadGltfJson(
            string json,
            Uri uri = null,
            ImportSettings importSettings = null,
            CancellationToken cancellationToken = default
            )
        {
            m_Settings = importSettings ?? new ImportSettings();
            var success =
                await LoadGltf(json, uri)
                && await LoadContent()
                && await Prepare();
            DisposeVolatileData();
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
        /// Creates an instance of the main scene of the glTF ( <see cref="RootBase.scene">scene</see> property in the JSON at root level)
        /// If the main scene index is not set, it instantiates nothing (as defined in the glTF 2.0 specification)
        /// </summary>
        /// <param name="parent">Transform that the scene will get parented to</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the main scene was instantiated or was not set. False in case of errors.</returns>
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
        /// Creates an instance of the main scene of the glTF ( <see cref="RootBase.scene">scene</see> property in the JSON at root level)
        /// If the main scene index is not set, it instantiates nothing (as defined in the glTF 2.0 specification)
        /// </summary>
        /// <param name="instantiator">Instantiator implementation; Receives and processes the scene data</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the main scene was instantiated or was not set. False in case of errors.</returns>
        /// <seealso cref="DefaultSceneIndex"/>
        public async Task<bool> InstantiateMainSceneAsync(
            IInstantiator instantiator,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            // According to glTF specification, loading nothing is
            // the correct behavior
            if (Root.scene < 0)
            {
#if DEBUG
                Debug.LogWarning("glTF has no (main) scene defined. No scene will be instantiated.");
#endif
                return true;
            }
            return await InstantiateSceneAsync(instantiator, Root.scene, cancellationToken);
        }

        /// <summary>
        /// Creates an instance of the scene specified by the scene index.
        /// </summary>
        /// <param name="parent">Transform that the scene will get parented to</param>
        /// <param name="sceneIndex">Index of the scene to be instantiated</param>
        /// <param name="cancellationToken">Token to submit cancellation requests. The default value is None.</param>
        /// <returns>True if the scene was instantiated. False in case of errors.</returns>
        /// <seealso cref="SceneCount"/>
        /// <seealso cref="GetSceneName"/>
        public async Task<bool> InstantiateSceneAsync(
            Transform parent,
            int sceneIndex = 0,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            if (sceneIndex < 0 || sceneIndex > Root.Scenes.Count) return false;
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
        /// <seealso cref="SceneCount"/>
        /// <seealso cref="GetSceneName"/>
        public async Task<bool> InstantiateSceneAsync(
            IInstantiator instantiator,
            int sceneIndex = 0,
            CancellationToken cancellationToken = default
            )
        {
            if (!LoadingDone || LoadingError) return false;
            if (sceneIndex < 0 || sceneIndex > Root.Scenes.Count) return false;
            await InstantiateSceneInternal(instantiator, sceneIndex);
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

            if (m_ImportInstances != null)
            {
                foreach (var importInstance in m_ImportInstances)
                {
                    importInstance.Value.Dispose();
                }
                m_ImportInstances = null;
            }

            m_NodeNames = null;

            void DisposeArray(IEnumerable<UnityEngine.Object> objects)
            {
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        SafeDestroy(obj);
                    }
                }
            }

            DisposeArray(m_Materials);
            m_Materials = null;

#if UNITY_ANIMATION
            DisposeArray(m_AnimationClips);
            m_AnimationClips = null;
#endif

            DisposeArray(m_Textures);
            m_Textures = null;

            m_MeshAssignments = null;
            DisposeArray(m_Meshes);
            m_Meshes = null;
            DisposeArray(m_Resources);
            m_Resources = null;
        }

        /// <summary>
        /// Number of materials
        /// </summary>
        public int MaterialCount => m_Materials?.Length ?? 0;

        /// <summary>
        /// Number of images
        /// </summary>
        public int ImageCount => m_Images?.Length ?? 0;

        /// <summary>
        /// Number of textures
        /// </summary>
        public int TextureCount => m_Textures?.Length ?? 0;

        /// <summary>
        /// Default scene index
        /// </summary>
        public int? DefaultSceneIndex => Root != null && Root.scene >= 0 ? Root.scene : (int?)null;

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
            return Root?.Scenes?[sceneIndex]?.name;
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
            if (defaultMaterial == null) {
                m_MaterialGenerator.SetLogger(Logger);
                defaultMaterial = m_MaterialGenerator.GetDefaultMaterial(m_DefaultMaterialPointsSupport);
                m_MaterialGenerator.SetLogger(null);
            }
            return defaultMaterial;
#else
            m_MaterialGenerator.SetLogger(Logger);
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

        /// <summary>
        /// Returns a texture by its glTF image index
        /// </summary>
        /// <param name="index">glTF image index</param>
        /// <returns>Corresponding Unity texture</returns>
        public Texture2D GetImage(int index = 0)
        {
            if (m_Images != null && index >= 0 && index < m_Images.Length)
            {
                return m_Images[index];
            }
            return null;
        }

        /// <summary>
        /// Returns a texture by its glTF texture index
        /// </summary>
        /// <param name="index">glTF texture index</param>
        /// <returns>Corresponding Unity texture</returns>
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
#if KTX_IS_ENABLED
            return (m_NonFlippedYTextureIndices == null || !m_NonFlippedYTextureIndices.Contains(index)) && GetSourceTexture(index).IsKtx;
#else
            return false;
#endif
        }

#if UNITY_ANIMATION
        /// <summary>
        /// Returns all imported animation clips
        /// </summary>
        /// <returns>All imported animation clips</returns>
        public AnimationClip[] GetAnimationClips() {
            return m_AnimationClips;
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
        /// A single glTF mesh is converted into one or more Unity Meshes, so <see cref="meshNumeration" /> is
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
        public CameraBase GetSourceCamera(uint index)
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
            if (Root?.Extensions?.KHR_lights_punctual.lights != null && index < Root.Extensions.KHR_lights_punctual.lights.Length)
            {
                return Root.Extensions.KHR_lights_punctual.lights[index];
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
        public MaterialBase GetSourceMaterial(int index = 0)
        {
            if (Root?.Materials != null && index >= 0 && index < Root.Materials.Count)
            {
                return Root.Materials[index];
            }
            return null;
        }

        /// <inheritdoc />
        public MeshBase GetSourceMesh(int meshIndex)
        {
            if (Root?.Meshes != null && meshIndex >= 0 && meshIndex < Root.Meshes.Count)
            {
                return Root.Meshes[meshIndex];
            }
            return null;
        }

        /// <inheritdoc />
        public MeshPrimitiveBase GetSourceMeshPrimitive(int meshIndex, int primitiveIndex)
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
                if (primitive.Extensions?.KHR_materials_variants?.mappings != null)
                {
                    materialSlots ??= new List<IMaterialsVariantsSlot>();
                    materialSlots.Add(primitive);
                }
            }

            return materialSlots?.ToArray();
        }

        /// <inheritdoc />
        public NodeBase GetSourceNode(int index = 0)
        {
            if (Root?.Nodes != null && index >= 0 && index < Root.Nodes.Count)
            {
                return Root.Nodes[index];
            }
            return null;
        }

        /// <inheritdoc />
        public TextureBase GetSourceTexture(int index = 0)
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
            var result = new Matrix4x4[skin.joints.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = Matrix4x4.identity;
            }
            m_SkinsInverseBindMatrices[skinId] = result;
            return result;
        }

        /// <inheritdoc />
        [Obsolete("This is going to be removed and replaced with an improved way to access accessors' data in a future release.")]
        public NativeSlice<byte> GetAccessor(int accessorIndex)
        {
            return GetAccessorData(accessorIndex);
        }

        /// <inheritdoc />
        [Obsolete("This is going to be removed and replaced with an improved way to access accessors' data in a future release.")]
        public NativeSlice<byte> GetAccessorData(int accessorIndex)
        {
            if (Root?.Accessors == null || accessorIndex < 0 || accessorIndex >= Root?.Accessors.Count)
            {
                return new NativeSlice<byte>();
            }
            var accessor = Root.Accessors[accessorIndex];
            return ((IGltfBuffers)this).GetBufferView(
                accessor.bufferView,
                out _,
                accessor.byteOffset,
                accessor.ByteSize
                ).ToSlice();
        }

        /// <inheritdoc />
        public int MaterialsVariantsCount => Root.MaterialsVariantsCount;

        /// <inheritdoc />
        public string GetMaterialsVariantName(int index)
        {
            return Root.GetMaterialsVariantName(index);
        }

        async Task<bool> LoadFromUri(Uri url, CancellationToken cancellationToken)
        {

            var download = await m_DownloadProvider.Request(url);
            var success = download.Success;

            if (cancellationToken.IsCancellationRequested)
            {
                return true;
            }

            if (success)
            {

                var gltfBinary = download.IsBinary ?? UriHelper.IsGltfBinary(url);

                if (gltfBinary ?? false)
                {
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
                    success = await LoadGltfBinaryBuffer(data, url);
                }
                else
                {
                    var text = download.Text;
                    download.Dispose();
                    success = await LoadGltf(text, url);
                }
                success = success
                    && await LoadContent()
                    && await Prepare();
            }
            else
            {
                Logger?.Error(LogCode.Download, download.Error, url.ToString());
            }

            DisposeVolatileData();
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
            m_Settings = importSettings ?? new ImportSettings();
            var success = await LoadGltfBinaryBuffer(bytes, uri);
            success = success
                && await LoadContent()
                && await Prepare();
            DisposeVolatileData();
            LoadingError = !success;
            LoadingDone = true;
            return success;
        }

        async Task<bool> LoadContent()
        {

            var success = await WaitForBufferDownloads();

#if MESHOPT
            if (success) {
                MeshoptDecode();
            }
#endif

#if UNITY_IMAGECONVERSION
            if (m_TextureDownloadTasks != null)
            {
                if(success)
                    await WaitForTextureDownloads();
                m_TextureDownloadTasks.Clear();
            }
#endif // UNITY_IMAGECONVERSION
#if KTX_IS_ENABLED
            if (m_KtxDownloadTasks != null) {
                if(success)
                    await WaitForKtxDownloads();
                m_KtxDownloadTasks.Clear();
            }
#endif // KTX_IS_ENABLED

            return success;
        }

        /// <summary>
        /// De-serializes a glTF JSON string and returns the glTF root schema object.
        /// </summary>
        /// <param name="json">glTF JSON</param>
        /// <returns>De-serialized glTF root object.</returns>
        protected abstract RootBase ParseJson(string json);

        async Task<bool> ParseJsonAndLoadBuffers(string json, Uri baseUri)
        {
            var predictedTime = json.Length / (float)k_JsonParseSpeed;
#if GLTFAST_THREADS && !MEASURE_TIMINGS
            if (DeferAgent.ShouldDefer(predictedTime))
            {
                // JSON is larger than threshold
                // => parse in a thread
                Root = await Task.Run(() => ParseJson(json));
            }
            else
#endif
            {
                // Parse immediately on main thread
                Root = ParseJson(json);

                // Loading subsequent buffers and images has to start asap.
                // That's why parsing JSON right away is *very* important.
            }

            if (Root == null)
            {
                Debug.LogError("JsonParsingFailed");
                Logger?.Error(LogCode.JsonParsingFailed);
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
                    var buffer = Root.Buffers[i];
                    if (!string.IsNullOrEmpty(buffer.uri))
                    {
                        if (buffer.uri.StartsWith("data:"))
                        {
                            if (!await LoadBufferFromDataUri(i, buffer))
                                return false;
                        }
                        else
                        {
                            LoadBuffer(i, UriHelper.GetUriString(buffer.uri, baseUri));
                        }
                    }
                }
            }

            return true;
        }

        async Task<bool> LoadBufferFromDataUri(int bufferIndex, Buffer buffer)
        {
            if (!TryGetBufferDataUriDescriptor(
                    bufferIndex, buffer.byteLength, buffer.uri, out var startIndex, out var byteLength))
            {
                return false;
            }

            var data = await DecodeDataUriAsync(
                buffer.uri,
                startIndex,
                byteLength,
                true // usually there's just one buffer and it's time-critical
            );
            if (!data.IsCreated)
            {
                Logger?.Error(LogCode.EmbedBufferLoadFailed);
                return false;
            }
            m_VolatileDisposables ??= new List<IDisposable>();
            m_VolatileDisposables.Add(data);
            m_Buffers[bufferIndex] = new ReadOnlyNativeArray<byte>(data);
            return true;
        }

        /// <summary>
        /// Validates required and used glTF extensions and reports unsupported ones.
        /// </summary>
        /// <returns>False if a required extension is not supported. True otherwise.</returns>
        bool CheckExtensionSupport()
        {
            if (!CheckExtensionSupport(Root.extensionsRequired))
            {
                return false;
            }
            CheckExtensionSupport(Root.extensionsUsed, false);
            return true;
        }

        bool CheckExtensionSupport(IEnumerable<string> extensions, bool required = true)
        {
            if (extensions == null)
                return true;
            var allExtensionsSupported = true;
            foreach (var ext in extensions)
            {
                var supported = k_SupportedExtensions.Contains(ext);
                if (!supported && m_ImportInstances != null)
                {
                    foreach (var extension in m_ImportInstances)
                    {
                        if (extension.Value.SupportsGltfExtension(ext))
                        {
                            supported = true;
                            break;
                        }
                    }
                }
                if (!supported)
                {
#if !DRACO_IS_ENABLED
                    if (ext == ExtensionName.DracoMeshCompression)
                    {
                        Logger?.Log(
                            required ? LogType.Error : LogType.Warning,
                            LogCode.PackageMissing,
                            "Draco for Unity",
                            ext
                            );
                    }
                    else
#endif
#if !MESHOPT
                    if (ext == ExtensionName.MeshoptCompression)
                    {
                        Logger?.Log(
                            required ? LogType.Error : LogType.Warning,
                            LogCode.PackageMissing,
                            "meshoptimizer decompression for Unity",
                            ext
                        );
                    }
                    else
#endif
#if !KTX_IS_ENABLED
                    if (ext == ExtensionName.TextureBasisUniversal)
                    {
                        Logger?.Log(
                            required ? LogType.Error : LogType.Warning,
                            LogCode.PackageMissing,
                            "KTX for Unity",
                            ext
                            );
                    }
                    else
#endif
                    if (required)
                    {
                        Logger?.Error(LogCode.ExtensionUnsupported, ext);
                    }
                    else
                    {
                        Logger?.Warning(LogCode.ExtensionUnsupported, ext);
                    }

                    allExtensionsSupported = false;
                }
            }
            return allExtensionsSupported;
        }

        async Task<bool> LoadGltf(string json, Uri url)
        {
            var baseUri = UriHelper.GetBaseUri(url);
            var success = await ParseJsonAndLoadBuffers(json, baseUri);
            if (success)
                await LoadImages(baseUri);
            return success;
        }

        async Task LoadImages(Uri baseUri)
        {
            if (Root.Textures != null && Root.Images != null)
            {
                Profiler.BeginSample("LoadImages.Prepare");

                m_Images = new Texture2D[Root.Images.Count];
                m_ImageFormats = new ImageFormat[Root.Images.Count];

                if (QualitySettings.activeColorSpace == ColorSpace.Linear)
                {

                    m_ImageGamma = new bool[Root.Images.Count];

                    void SetImageGamma(TextureInfoBase txtInfo)
                    {
                        if (
                            txtInfo != null &&
                            txtInfo.index >= 0 &&
                            txtInfo.index < Root.Textures.Count
                        )
                        {
                            var imageIndex = Root.Textures[txtInfo.index].GetImageIndex();
                            m_ImageGamma[imageIndex] = true;
                        }
                    }

                    if (Root.Materials != null)
                    {
                        for (int i = 0; i < Root.Materials.Count; i++)
                        {
                            var mat = Root.Materials[i];
                            if (mat.PbrMetallicRoughness != null)
                            {
                                SetImageGamma(mat.PbrMetallicRoughness.BaseColorTexture);
                            }
                            SetImageGamma(mat.EmissiveTexture);
                            if (mat.Extensions?.KHR_materials_pbrSpecularGlossiness != null)
                            {
                                SetImageGamma(mat.Extensions.KHR_materials_pbrSpecularGlossiness.diffuseTexture);
                                SetImageGamma(mat.Extensions.KHR_materials_pbrSpecularGlossiness.specularGlossinessTexture);
                            }
                        }
                    }
                }

#if KTX_IS_ENABLED
                // Derive image type from texture extension
                for (int i = 0; i < Root.Textures.Count; i++) {
                    var texture = Root.Textures[i];
                    if(texture.IsKtx) {
                        var imgIndex = texture.GetImageIndex();
                        m_ImageFormats[imgIndex] = ImageFormat.Ktx;
                    }
                }
#endif // KTX_IS_ENABLED

                // Determine which images need to be readable, because they
                // are applied using different samplers.
                var imageVariants = new HashSet<int>[m_Images.Length];
                foreach (var txt in Root.Textures)
                {
                    var imageIndex = txt.GetImageIndex();
                    if (imageIndex < 0 || imageIndex >= Root.Images.Count) continue;
                    if (imageVariants[imageIndex] == null)
                    {
                        imageVariants[imageIndex] = new HashSet<int>();
                    }
                    imageVariants[imageIndex].Add(txt.sampler);
                }

#if !UNITY_VISIONOS
                if (!m_Settings.TexturesReadable)
                {
                    m_ImageReadable = new bool[m_Images.Length];
                    for (var i = 0; i < m_Images.Length; i++)
                    {
                        m_ImageReadable[i] = imageVariants[i] != null && imageVariants[i].Count > 1;
                    }
                }
#endif
                Profiler.EndSample();

                List<Task<bool>> imageTasks = null;

                for (int imageIndex = 0; imageIndex < Root.Images.Count; imageIndex++)
                {
                    var img = Root.Images[imageIndex];

                    if (!string.IsNullOrEmpty(img.uri) && img.uri.StartsWith("data:"))
                    {
                        var imageTask = LoadImageFromDataUri(imageIndex, img);
                        imageTasks ??= new List<Task<bool>>();
                        imageTasks.Add(imageTask);
                    }
                    else
                    {
                        ImageFormat imgFormat;
                        if (m_ImageFormats[imageIndex] == ImageFormat.Unknown)
                        {
                            imgFormat = string.IsNullOrEmpty(img.mimeType)
                                ? UriHelper.GetImageFormatFromUri(img.uri)
                                : ImageFormatExtensions.FromMimeType(img.mimeType);
                            m_ImageFormats[imageIndex] = imgFormat;
                        }
                        else
                        {
                            imgFormat = m_ImageFormats[imageIndex];
                        }

                        if (imgFormat != ImageFormat.Unknown)
                        {
                            if (img.bufferView < 0)
                            {
                                // Not Inside buffer
                                if (!string.IsNullOrEmpty(img.uri))
                                {
                                    LoadImage(
                                        imageIndex,
                                        UriHelper.GetUriString(img.uri, baseUri),
                                        !LoadImageReadable(imageIndex),
                                        imgFormat == ImageFormat.Ktx
                                        );
                                }
                                else
                                {
                                    Logger?.Error(LogCode.MissingImageURL);
                                }
                            }
                        }
                        else
                        {
                            Logger?.Error(LogCode.ImageFormatUnknown, imageIndex.ToString(), img.uri);
                        }
                    }
                }

                if (imageTasks != null)
                {
                    await Task.WhenAll(imageTasks);
                }
            }
        }

        // TODO: If no suitable image loader is found, this method won't use the await operator, thus causing a warning.
        //       For now we'll ignore that warning. In the future, we'll encapsulate this in a better way.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        async Task<bool> LoadImageFromDataUri(int imageIndex, Image img)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (!TryGetImageDataUriDescriptor(img.uri, out var imageFormat, out var startIndex, out var byteLength))
            {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
                return false;
            }
            m_ImageFormats[imageIndex] = imageFormat;

            switch (imageFormat)
            {
                case ImageFormat.Unknown:
                    Logger?.Error(LogCode.EmbedImageLoadFailed);
                    return false;
                case ImageFormat.Jpeg:
                case ImageFormat.PNG:
#if UNITY_IMAGECONVERSION
                {
                    var texture = await LoadImageJpegOrPngFromDataUri(imageIndex, img, startIndex, byteLength);
                    if (texture is not null)
                    {
                        m_Images[imageIndex] = texture;
                        return true;
                    }
                    return false;
                }
#else
                    Logger?.Warning(LogCode.ImageConversionNotEnabled);
                    return true;
#endif
                case ImageFormat.Ktx:
#if KTX_IS_ENABLED
                {
                    var texture = await LoadImageKtxFromDataUri(imageIndex, img, startIndex, byteLength);
                    if (texture is not null)
                    {
                        m_Images[imageIndex] = texture;
                        return true;
                    }

                    return false;
                }
#endif
                default:
                    Logger?.Error(LogCode.EmbedImageUnsupportedType, m_ImageFormats[imageIndex].ToString());
                    return false;
            }
        }

#if UNITY_IMAGECONVERSION
        async Task<Texture2D> LoadImageJpegOrPngFromDataUri(int imageIndex, Image img, int startIndex, int byteLength)
        {
#if UNITY_6000_0_OR_NEWER
            var data = await DecodeDataUriAsync(img.uri, startIndex, byteLength);
            if (!data.IsCreated)
            {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
                return null;
            }
#else
            var data = await DecodeDataUriToManagedArrayAsync(img.uri, startIndex, byteLength);
            if (data == null)
            {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
                return null;
            }
#endif
            await DeferAgent.BreakPoint();
            Profiler.BeginSample("LoadImageJpegOrPngFromDataUri");
            // TODO: Investigate alternative: native texture creation in worker thread
            var forceSampleLinear = m_ImageGamma != null && !m_ImageGamma[imageIndex];
            var texture = CreateEmptyTexture(img, imageIndex, forceSampleLinear);
            texture.LoadImage(
#if UNITY_6000_0_OR_NEWER
                data.AsReadOnlySpan(),
#else
                data,
#endif
                !LoadImageReadable(imageIndex)
            );
            Profiler.EndSample();
            return texture;
        }
#endif // UNITY_IMAGECONVERSION

#if KTX_IS_ENABLED
        async Task<Texture2D> LoadImageKtxFromDataUri(int imageIndex, Image img, int startIndex, int byteLength)
        {
            var data = await DecodeDataUriAsync(img.uri, startIndex, byteLength);
            if (!data.IsCreated)
            {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
                return null;
            }
            await DeferAgent.BreakPoint();
            var texture = await LoadImageKtx(imageIndex, img, data.AsReadOnly());
            data.Dispose();
            return texture;
        }

        async Task<Texture2D> LoadImageKtx(
            int imageIndex,
            Image img,
            NativeArray<byte>.ReadOnly data)
        {
            Profiler.BeginSample("LoadImageKtx");

            var forceSampleLinear = m_ImageGamma != null && !m_ImageGamma[imageIndex];
            var readable = LoadImageReadable(imageIndex);

            Texture2D texture = null;

            var ktxTexture = new KtxTexture();
            var errorCode = ktxTexture.Open(data);
            if (errorCode != ErrorCode.Success) {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
                return null;
            }
            var result = await ktxTexture.LoadTexture2D(forceSampleLinear, readable);
            ktxTexture.Dispose();
            if (result.errorCode == ErrorCode.Success) {
                texture = result.texture;
                texture.name = GetImageName(img, imageIndex);
            }
            else {
                Logger?.Error(LogCode.EmbedImageLoadFailed);
            }

            Profiler.EndSample();
            return texture;
        }
#endif

        async Task<bool> WaitForBufferDownloads()
        {
            if (m_DownloadTasks != null)
            {
                foreach (var downloadPair in m_DownloadTasks)
                {
                    var download = await downloadPair.Value;
                    if (download.Success)
                    {
                        Profiler.BeginSample("GetData");

                        m_VolatileDisposables ??= new List<IDisposable>();
                        if (download is INativeDownload nativeDownload)
                        {
                            var wrapper = new ReadOnlyNativeArrayFromNativeArray<byte>(nativeDownload.NativeData);
                            m_Buffers[downloadPair.Key] = wrapper.Array;
                        }
                        else
                        {
                            var wrapper = new ReadOnlyNativeArrayFromManagedArray<byte>(download.Data);
                            m_Buffers[downloadPair.Key] = wrapper.Array;
                            m_VolatileDisposables.Add(wrapper);
                        }

                        m_VolatileDisposables.Add(download);

                        Profiler.EndSample();
                    }
                    else
                    {
                        Logger?.Error(LogCode.BufferLoadFailed, download.Error, downloadPair.Key.ToString());
                        return false;
                    }
                }
            }

            if (m_Buffers != null)
            {
                Profiler.BeginSample("CreateGlbBinChunks");
                for (int i = 0; i < m_Buffers.Length; i++)
                {
                    if (i == 0 && m_GlbBinChunk.HasValue)
                    {
                        // Already assigned in LoadGltfBinary
                        continue;
                    }
                    var b = m_Buffers[i];
                    if (b.IsCreated)
                    {
                        m_BinChunks[i] = new GlbBinChunk(0, (uint)b.Length);
                    }
                }
                Profiler.EndSample();
            }
            return true;
        }

#if UNITY_IMAGECONVERSION
        async Task<bool> WaitForTextureDownloads()
        {
            foreach (var dl in m_TextureDownloadTasks)
            {
                await dl.Value.Load();
                var www = dl.Value.Download;

                if (www == null)
                {
                    Logger?.Error(LogCode.TextureDownloadFailed, "?", dl.Key.ToString());
                    return false;
                }

                if (www.Success)
                {
                    var imageIndex = dl.Key;
                    Texture2D txt;
                    // TODO: Loading Jpeg/PNG textures like this creates major frame stalls. Main thread is waiting
                    // on Render thread, which is occupied by Gfx.UploadTextureData for 19 ms for a 2k by 2k texture
                    if (LoadImageFromBytes(imageIndex))
                    {
                        Profiler.BeginSample("Texture2D.LoadImage");
                        var forceSampleLinear = m_ImageGamma!=null && !m_ImageGamma[imageIndex];
                        txt = CreateEmptyTexture(Root.Images[imageIndex], imageIndex, forceSampleLinear);
                        var markNonReadable = !LoadImageReadable(imageIndex);
#if UNITY_6000_0_OR_NEWER
                        if(www is INativeDownload nativeDownload)
                        {
                            txt.LoadImage(
                                nativeDownload.NativeData.AsReadOnlySpan(),
                                markNonReadable
                            );
                        }
                        else
#endif
                        {
                            txt.LoadImage(
                                www.Data,
                                markNonReadable
                                );
                        }
                        Profiler.EndSample();
                    }
                    else
                    {
                        Assert.IsTrue(www is ITextureDownload);
                        Profiler.BeginSample("ITextureDownload.Texture");
                        txt = ((ITextureDownload)www).Texture;
                        Profiler.EndSample();
                        txt.name = GetImageName(Root.Images[imageIndex], imageIndex);
                    }
                    www.Dispose();
                    m_Images[imageIndex] = txt;
                    await DeferAgent.BreakPoint();
                }
                else
                {
                    Logger?.Error(LogCode.TextureDownloadFailed, www.Error, dl.Key.ToString());
                    www.Dispose();
                    return false;
                }
            }
            return true;
        }
#endif // UNITY_IMAGECONVERSION

#if KTX_IS_ENABLED
        async Task<bool> WaitForKtxDownloads() {
            var tasks = new Task<bool>[m_KtxDownloadTasks.Count];
            var i = 0;
            foreach( var dl in m_KtxDownloadTasks ) {
                tasks[i] = ProcessKtxDownload(dl.Key, dl.Value);
                i++;
            }
            await Task.WhenAll(tasks);
            foreach (var task in tasks) {
                if (!task.Result) return false;
            }
            return true;
        }

        async Task<bool> ProcessKtxDownload(int imageIndex, Task<IDownload> downloadTask) {
            var www = await downloadTask;
            if(www.Success) {
                NativeArray<byte>.ReadOnly data;
                if (www is INativeDownload nativeDownload)
                {
                    data = nativeDownload.NativeData;
                }
                else
                {
                    var managedNativeArray = new ReadOnlyNativeArrayFromManagedArray<byte>(www.Data);
                    m_VolatileDisposables ??= new List<IDisposable>();
                    m_VolatileDisposables.Add(managedNativeArray);
                    data = managedNativeArray.Array.AsNativeArrayReadOnly();
                }
                var ktxContext = new KtxLoadContext(imageIndex,data);
                var forceSampleLinear = m_ImageGamma!=null && !m_ImageGamma[imageIndex];
                var readable = LoadImageReadable(imageIndex);
                var result = await ktxContext.LoadTexture2D(forceSampleLinear, readable);
                if (result.errorCode == ErrorCode.Success) {
                    m_Images[imageIndex] = result.texture;
                    if (!result.orientation.IsYFlipped())
                    {
                        m_NonFlippedYTextureIndices ??= new HashSet<int>();
                        m_NonFlippedYTextureIndices.Add(imageIndex);
                    }
                    www.Dispose();
                    return true;
                }
            } else {
                Logger?.Error(LogCode.TextureDownloadFailed,www.Error,imageIndex.ToString());
            }
            www.Dispose();
            return false;
        }
#endif // KTX_IS_ENABLED

        void LoadBuffer(int index, Uri url)
        {
            Profiler.BeginSample("LoadBuffer");
            if (m_DownloadTasks == null)
            {
                m_DownloadTasks = new Dictionary<int, Task<IDownload>>();
            }
            m_DownloadTasks.Add(index, m_DownloadProvider.Request(url));
            Profiler.EndSample();
        }

        async ValueTask<NativeArray<byte>> DecodeDataUriAsync(
            string dataUri,
            int startIndex,
            int byteLength,
            bool timeCritical = false
            )
        {
            var predictedTime = dataUri.Length / (float)k_Base64DecodeSpeed;
#if MEASURE_TIMINGS
            var stopWatch = new Stopwatch();
            stopWatch.Start();
#elif GLTFAST_THREADS
            if (!timeCritical || DeferAgent.ShouldDefer(predictedTime))
            {
                return await Task.Run(() => DecodeDataUri(dataUri, startIndex, byteLength));
            }
#endif
            await DeferAgent.BreakPoint(predictedTime);
            var result = DecodeDataUri(dataUri, startIndex, byteLength);
#if MEASURE_TIMINGS
            stopWatch.Stop();
            var elapsedSeconds = stopWatch.ElapsedMilliseconds / 1000f;
            var relativeDiff = (elapsedSeconds-predictedTime) / predictedTime;
            if (Mathf.Abs(relativeDiff) > .2f) {
                Debug.LogWarning($"Base 64 unexpected duration! diff: {relativeDiff:0.00}% predicted: {predictedTime} sec actual: {elapsedSeconds} sec");
            }
            var throughput = dataUri.Length / elapsedSeconds;
            Debug.Log($"Base 64 throughput: {throughput} bytes/sec ({dataUri.Length} bytes in {elapsedSeconds} seconds)");
#endif
            return result;
        }

#if !UNITY_6000_0_OR_NEWER
        async ValueTask<byte[]> DecodeDataUriToManagedArrayAsync(
            string dataUri,
            int startIndex,
            int byteLength,
            bool timeCritical = false)
        {
            var predictedTime = dataUri.Length / (float)k_Base64DecodeSpeed;
#if GLTFAST_THREADS
            if (!timeCritical || DeferAgent.ShouldDefer(predictedTime))
            {
                return await Task.Run(() => DecodeDataUriToManagedArray(dataUri, startIndex, byteLength));
            }
#endif
            await DeferAgent.BreakPoint(predictedTime);
            var result = DecodeDataUriToManagedArray(dataUri, startIndex, byteLength);
            return result;
        }
#endif // !UNITY_6000_0_OR_NEWER

        bool TryGetDataUriDescriptor(string dataUri, out ReadOnlySpan<char> mimeType, out int startIndex, out int byteLength)
        {
            Logger?.Warning(LogCode.EmbedSlow);
            var mediaTypeEnd = dataUri.IndexOf(';', 5, Math.Min(dataUri.Length - 5, 1000));
            if (mediaTypeEnd < 0)
            {
                Profiler.EndSample();
                mimeType = null;
                startIndex = 0;
                byteLength = -1;
                return false;
            }
            mimeType = dataUri.AsSpan(5, mediaTypeEnd - 5);
            if (!dataUri.AsSpan(mediaTypeEnd + 1, 7).SequenceEqual("base64,"))
            {
                Profiler.EndSample();
                startIndex = 0;
                byteLength = -1;
                return false;
            }
            var padding = 0;
            if (dataUri.Length > 0 && dataUri[^1] == '=')
            {
                padding = dataUri.Length > 1 && dataUri[^2] == '=' ? 2 : 1;
            }

            startIndex = mediaTypeEnd + 8;
            byteLength = ((dataUri.Length - startIndex) * 3 + 3) / 4 - padding;
            return true;
        }

        bool TryGetBufferDataUriDescriptor(
            int bufferIndex,
            uint expectedLength,
            string dataUri,
            out int startIndex,
            out int byteLength
            )
        {
            if (TryGetDataUriDescriptor(dataUri, out var mimeType, out startIndex, out byteLength))
            {
                if (!mimeType.StartsWith("application/")
                    || !(
                        mimeType[12..].SequenceEqual("octet-stream")
                        || mimeType[12..].SequenceEqual("gltf-buffer")
                        )
                    )
                {
                    Logger?.Error(
                        LogCode.BufferDataUriUnexpectedMimeType,
                        bufferIndex.ToString(),
                        mimeType.ToString()
                        );
                    return false;
                }

                if (byteLength < expectedLength)
                {
                    Logger?.Error(
                        LogCode.BufferContentUndersized,
                        bufferIndex.ToString(),
                        expectedLength.ToString(),
                        byteLength.ToString()
                    );
                    return false;
                }

                return true;
            }

            Logger?.Error(LogCode.EmbedBufferLoadFailed);
            return false;
        }

        bool TryGetImageDataUriDescriptor(
            string dataUri,
            out ImageFormat imageFormat,
            out int startIndex,
            out int byteLength
            )
        {
            if (TryGetDataUriDescriptor(dataUri, out var mimeType, out startIndex, out byteLength))
            {
                imageFormat = ImageFormatExtensions.FromMimeType(mimeType);
                return true;
            }

            imageFormat = ImageFormat.Unknown;
            return false;
        }

        static NativeArray<byte> DecodeDataUri(string dataUri, int startIndex, int dataLength)
        {
            Profiler.BeginSample("DecodeDataUri");
            var data = new NativeArray<byte>(dataLength, Allocator.Persistent);
            if (!Convert.TryFromBase64Chars(dataUri.AsSpan(startIndex), data.AsSpan(), out var bytesWritten)
               || bytesWritten != dataLength)
            {
                // Invalidate buffer to signal decoding failed.
                data.Dispose();
            }
            Profiler.EndSample();
            return data;
        }

#if !UNITY_6000_0_OR_NEWER
        static byte[] DecodeDataUriToManagedArray(string dataUri, int startIndex, int dataLength)
        {
            Profiler.BeginSample("DecodeDataUriToManagedArray");
            var data = new byte[dataLength];
            if (!Convert.TryFromBase64Chars(dataUri.AsSpan(startIndex), data.AsSpan(), out var bytesWritten)
                || bytesWritten != dataLength)
            {
                // Invalidate buffer to signal decoding failed.
                data = null;
            }
            Profiler.EndSample();
            return data;
        }
#endif // !UNITY_6000_0_OR_NEWER

        void LoadImage(int imageIndex, Uri url, bool nonReadable, bool isKtx)
        {

            Profiler.BeginSample("LoadTexture");

            if (isKtx)
            {
#if KTX_IS_ENABLED
                var downloadTask = m_DownloadProvider.Request(url);
                if(m_KtxDownloadTasks==null) {
                    m_KtxDownloadTasks = new Dictionary<int, Task<IDownload>>();
                }
                m_KtxDownloadTasks.Add(imageIndex, downloadTask);
#else
                Logger?.Error(LogCode.PackageMissing, "KTX for Unity", ExtensionName.TextureBasisUniversal);
                Profiler.EndSample();
                return;
#endif // KTX_IS_ENABLED
            }
            else
            {
#if UNITY_IMAGECONVERSION
                var downloadTask = LoadImageFromBytes(imageIndex)
                    ? (TextureDownloadBase) new TextureDownload<IDownload>(m_DownloadProvider.Request(url))
                    : new TextureDownload<ITextureDownload>(m_DownloadProvider.RequestTexture(url,nonReadable));
                if(m_TextureDownloadTasks==null) {
                    m_TextureDownloadTasks = new Dictionary<int, TextureDownloadBase>();
                }
                m_TextureDownloadTasks.Add(imageIndex, downloadTask);
#else
                Logger?.Warning(LogCode.ImageConversionNotEnabled);
#endif
            }
            Profiler.EndSample();
        }

        /// <summary>
        /// UnityWebRequestTexture always loads Jpegs/PNGs in sRGB color space
        /// without mipmaps. This method figures if this is not desired and the
        /// texture data needs to be loaded from raw bytes.
        /// </summary>
        /// <param name="imageIndex">glTF image index</param>
        /// <returns>True if image texture had to be loaded manually from bytes, false otherwise.</returns>
        bool LoadImageFromBytes(int imageIndex)
        {

#if UNITY_EDITOR
            if (IsEditorImport) {
                // Use the original texture at Editor (asset database) import
                return false;
            }
#endif
#if UNITY_WEBREQUEST_TEXTURE
            var forceSampleLinear = m_ImageGamma != null && !m_ImageGamma[imageIndex];
            return forceSampleLinear || m_Settings.GenerateMipMaps;
#else
            Logger?.Warning(LogCode.UnityWebRequestTextureNotEnabled);
            return true;
#endif
        }

        async Task<bool> LoadGltfBinaryBuffer(NativeArray<byte>.ReadOnly bytes, Uri uri = null)
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

            int index = 12; // first chunk header

            var baseUri = UriHelper.GetBaseUri(uri);

            Profiler.EndSample();

            while (index < bytes.Length)
            {

                if (index + 8 > bytes.Length)
                {
                    Logger?.Error(LogCode.ChunkIncomplete);
                    return false;
                }

                var chLength = bytes.ReadUInt32(index);
                index += 4;
                var chType = bytes.ReadUInt32(index);
                index += 4;

                if (index + chLength > bytes.Length)
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

                    Profiler.BeginSample("GetJSON");
                    var bytesStream = bytes.ToUnmanagedMemoryStream((uint)index, chLength);
                    var reader = new StreamReader(bytesStream);
                    var json = await reader.ReadToEndAsync();
                    Profiler.EndSample();

                    var success = await ParseJsonAndLoadBuffers(json, baseUri);

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
            await LoadImages(baseUri);
            return true;
        }

        ReadOnlyNativeArray<byte> GetBuffer(int index)
        {
            return m_Buffers[index];
        }

        ReadOnlyNativeArray<byte> IGltfBuffers.GetBufferView(int bufferViewIndex, out int byteStride, int offset, int length)
        {
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT
            if (bufferView.Extensions?.EXT_meshopt_compression != null) {
                byteStride = bufferView.Extensions.EXT_meshopt_compression.byteStride;
                var entireBuffer = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && length <= 0) {
                    return new ReadOnlyNativeArray<byte>(entireBuffer);
                }
                Assert.IsTrue(offset >= 0);
                if (length <= 0) {
                    length = entireBuffer.Length - offset;
                }
                Assert.IsTrue(offset+length <= entireBuffer.Length);
                return new ReadOnlyNativeArray<byte>(entireBuffer.GetSubArray(offset,length));
            }
#endif
            byteStride = bufferView.byteStride;
            return GetBufferView(bufferView, offset, length);
        }


        ReadOnlyNativeArray<T> IGltfBuffers.GetAccessorData<T>(
            int bufferViewIndex,
            int count,
            int offset
            )
        {
            var bufferView = Root.BufferViews[bufferViewIndex];
#if MESHOPT
            if (bufferView.Extensions?.EXT_meshopt_compression != null) {
                var fullSlice = m_MeshoptBufferViews[bufferViewIndex];
                if (offset == 0 && (count <= 0 || count * UnsafeUtility.SizeOf(typeof(T)) == fullSlice.Length)) {
                    return new ReadOnlyNativeArray<byte>(fullSlice).Reinterpret<T>();
                }
                Assert.IsTrue(offset >= 0);
                Assert.IsTrue(count > 0);
                Assert.IsTrue(offset + count * UnsafeUtility.SizeOf(typeof(T)) <= fullSlice.Length);
                return new ReadOnlyNativeArray<byte>(fullSlice).GetSubArray(offset,count).Reinterpret<T>();
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
#if MESHOPT
            if (bufferView.Extensions?.EXT_meshopt_compression != null) {
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
                        bufferView.byteStride
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        ,ref safety
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
            var byteStride = bufferView.ByteStride > 0 ? bufferView.ByteStride : UnsafeUtility.SizeOf(typeof(T));
            return m_Buffers[bufferIndex].ToStrided<T>(totalOffset, count, byteStride);
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

#if MESHOPT
        void MeshoptDecode() {
            if(Root.BufferViews!=null) {
                List<JobHandle> jobHandlesList = null;
                for (var i = 0; i < Root.BufferViews.Count; i++) {
                    var bufferView = Root.BufferViews[i];
                    if (bufferView.Extensions?.EXT_meshopt_compression != null) {
                        var meshopt = bufferView.Extensions.EXT_meshopt_compression;
                        if (jobHandlesList == null) {
                            m_MeshoptBufferViews = new Dictionary<int, NativeArray<byte>>();
                            jobHandlesList = new List<JobHandle>(Root.BufferViews.Count);
                            m_MeshoptReturnValues = new NativeArray<int>(Root.BufferViews.Count, Allocator.TempJob);
                        }

                        var arr = new NativeArray<byte>(meshopt.count * meshopt.byteStride, Allocator.Persistent);

                        var origBufferView = GetBufferView(meshopt);

                        var jobHandle = Decode.DecodeGltfBuffer(
                            new NativeSlice<int>(m_MeshoptReturnValues,i,1),
                            arr,
                            meshopt.count,
                            meshopt.byteStride,
                            origBufferView.ToSlice(),
                            meshopt.GetMode(),
                            meshopt.GetFilter()
                        );
                        jobHandlesList.Add(jobHandle);
                        m_MeshoptBufferViews[i] = arr;
                    }
                }

                if (jobHandlesList != null) {
                    using (var jobHandles = new NativeArray<JobHandle>(jobHandlesList.ToArray(), Allocator.Temp)) {
                        m_MeshoptJobHandle = JobHandle.CombineDependencies(jobHandles);
                    }
                }
            }
        }

        async Task<bool> WaitForMeshoptDecode() {
            var success = true;
            if (m_MeshoptBufferViews != null) {
                while (!m_MeshoptJobHandle.IsCompleted) {
                    await Task.Yield();
                }
                m_MeshoptJobHandle.Complete();

                foreach (var returnValue in m_MeshoptReturnValues) {
                    success &= returnValue == 0;
                }
                m_MeshoptReturnValues.Dispose();
            }
            return success;
        }

#endif // MESHOPT

        async Task<bool> Prepare()
        {
            m_Resources = new List<UnityEngine.Object>();

            if (Root.Images != null && Root.Textures != null && Root.Materials != null)
            {
                if (m_Images == null)
                {
                    m_Images = new Texture2D[Root.Images.Count];
                }
                else
                {
                    Assert.AreEqual(m_Images.Length, Root.Images.Count);
                }
                await CreateTexturesFromBuffers(Root.Images, Root.BufferViews);
            }
            await DeferAgent.BreakPoint();

            // RedundantAssignment potentially becomes necessary when MESHOPT is not available
            // ReSharper disable once RedundantAssignment
            var success = true;

#if MESHOPT
            success = await WaitForMeshoptDecode();
            if (!success) return false;
#endif

            if (Root.Accessors != null)
            {
                success = await LoadAccessorData();
                await DeferAgent.BreakPoint();

                while (!m_AccessorJobsHandle.IsCompleted)
                {
                    await Task.Yield();
                }
                m_AccessorJobsHandle.Complete();
            }
            if (!success) return success;

#if KTX_IS_ENABLED
            if(m_KtxLoadContextsBuffer!=null) {
                await ProcessKtxLoadContexts();
            }
#endif // KTX_IS_ENABLED

#if UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER
            if (m_ImageCreateContexts != null)
            {
                await WaitForImageCreateContexts();
            }
#endif // UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER

            if (m_Images != null && Root.Textures != null)
            {
                PopulateTexturesAndImageVariants();
            }

            if (Root.Materials != null)
            {
                await GenerateMaterials();
            }
            await DeferAgent.BreakPoint();

            if (m_MeshOrders != null)
            {
                await WaitForAllMeshGenerators();
                await DeferAgent.BreakPoint();

                await AssignAllAccessorData();

                success = await CreateAllMeshAssignments();
            }

#if UNITY_ANIMATION
            if (Root.HasAnimation) {
                if (m_Settings.NodeNameMethod != NameImportMethod.OriginalUnique) {
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

#if UNITY_ANIMATION
            if (Root.HasAnimation && m_Settings.AnimationMethod != AnimationMethod.None)
            {
                CreateAnimationClips(parentIndex);
            }
#endif

            DisposeVolatileAccessorData();
            return success;
        }

#if UNITY_ANIMATION
        void CreateAnimationClips(int[] parentIndex)
        {
            m_AnimationClips = new AnimationClip[Root.Animations.Count];
            for (var i = 0; i < Root.Animations.Count; i++) {
                var animation = Root.Animations[i];
                m_AnimationClips[i] = new AnimationClip
                {
                    name = animation.name ?? $"Clip_{i}",

                    // Legacy Animation requirement
                    legacy = m_Settings.AnimationMethod == AnimationMethod.Legacy,
                    wrapMode = WrapMode.Loop
                };

                for (var j = 0; j < animation.Channels.Count; j++) {
                    var channel = animation.Channels[j];
                    if (channel.sampler < 0 || channel.sampler >= animation.Samplers.Count) {
                        Logger?.Error(LogCode.AnimationChannelSamplerInvalid, j.ToString());
                        continue;
                    }
                    var sampler = animation.Samplers[channel.sampler];
                    if (sampler == null || sampler.output < 0 || sampler.output >= Root.Accessors.Count)
                    {
                        Logger?.Error(LogCode.AnimationChannelSamplerInvalid, j.ToString());
                        continue;
                    }
                    if (channel.Target.node < 0 || channel.Target.node >= Root.Nodes.Count) {
                        Logger?.Error(LogCode.AnimationChannelNodeInvalid, j.ToString());
                        continue;
                    }

                    var path = AnimationUtils.CreateAnimationPath(channel.Target.node,m_NodeNames,parentIndex);
                    var times = (NativeArray<float>) m_AccessorData[sampler.input];
                    var outputData = m_AccessorData[sampler.output];
                    var interpolationType = sampler.GetInterpolationType();

                    switch (channel.Target.GetPath()) {
                        case AnimationChannelBase.Path.Translation: {
                            var values = CastOrCreateTypedBuffer<float3>(outputData, times.Length, interpolationType);
                            AnimationUtils.AddTranslationCurves(m_AnimationClips[i], path, times, values, interpolationType);
                            break;
                        }
                        case AnimationChannelBase.Path.Rotation: {
                            var values = CastOrCreateTypedBuffer<quaternion>(outputData, times.Length, interpolationType);
                            AnimationUtils.AddRotationCurves(m_AnimationClips[i], path, times, values, interpolationType);
                            break;
                        }
                        case AnimationChannelBase.Path.Scale: {
                            var values = CastOrCreateTypedBuffer<float3>(outputData, times.Length, interpolationType);
                            AnimationUtils.AddScaleCurves(m_AnimationClips[i], path, times, values, interpolationType);
                            break;
                        }
                        case AnimationChannelBase.Path.Weights: {
                            var values = CastOrCreateTypedBuffer<float>(outputData, times.Length, interpolationType);
                            var node = Root.Nodes[channel.Target.node];
                            if (node.mesh < 0 || node.mesh >= Root.Meshes.Count) {
                                break;
                            }
                            var mesh = Root.Meshes[node.mesh];
                            AnimationUtils.AddMorphTargetWeightCurves(
                                m_AnimationClips[i],
                                path,
                                times,
                                values,
                                interpolationType,
                                mesh.Extras?.targetNames
                            );

                            // HACK BEGIN:
                            // Since meshes with multiple primitives that are not using
                            // identical vertex buffers are split up into separate Unity
                            // Meshes. Because of this, we have to duplicate the animation
                            // curves, so that all primitives are animated.
                            // TODO: Refactor primitive sub-meshing and remove this hack
                            // https://github.com/atteneder/glTFast/issues/153
                            var meshName = string.IsNullOrEmpty(mesh.name) ? k_PrimitiveName : mesh.name;
                            var meshCount = m_MeshAssignments.GetLength(node.mesh);
                            for (var k = 1; k < meshCount; k++) {
                                var primitiveName = $"{meshName}_{k}";
                                AnimationUtils.AddMorphTargetWeightCurves(
                                    m_AnimationClips[i],
                                    $"{path}/{primitiveName}",
                                    times,
                                    values,
                                    interpolationType,
                                    mesh.Extras?.targetNames
                                );
                            }
                            // HACK END
                            break;
                        }
                        case AnimationChannelBase.Path.Pointer:
                            Logger?.Warning(LogCode.AnimationTargetPathUnsupported,channel.Target.GetPath().ToString());
                            break;
                        case AnimationChannelBase.Path.Unknown:
                        case AnimationChannelBase.Path.Invalid:
                        default:
                            Logger?.Error(LogCode.AnimationTargetPathUnsupported,channel.Target.GetPath().ToString());
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Casts <paramref name="input"/> to the given type, or if unavailable allocates a temp buffer filled with 0-value data.
        /// </summary>
        /// <param name="input">Will be filled with 0-value data if unavailable.</param>
        /// <param name="expectedLength">The expected length of the temp buffer.</param>
        /// <param name="interpolationType">The <see cref="InterpolationType"/> of the expected data which might change
        /// the resulting length of the output if the input was unavailable.</param>
        /// <typeparam name="T">The expected type of the buffer.</typeparam>
        /// <returns>A <see cref="NativeArray{T}"/>.</returns>
        static NativeArray<T> CastOrCreateTypedBuffer<T>(IDisposable input, int expectedLength, InterpolationType interpolationType) where T : unmanaged
        {
            if (input is null)
            {
                // InterpolationType.CubicSpline has 3 values per key (in-tangent, out-tangent and value).
                var unknownOutputLength = expectedLength * (interpolationType == InterpolationType.CubicSpline ? 3 : 1);
                return new NativeArray<T>(unknownOutputLength, Allocator.Temp);
            }

            Assert.IsTrue(input is NativeArray<T>);
            return (NativeArray<T>)input;
        }

#endif // UNITY_ANIMATION

        void CalculateSkinSkeletons(int[] parentIndex)
        {
            foreach (var skin in Root.Skins)
            {
                if (skin.skeleton < 0)
                {
                    skin.skeleton = GetLowestCommonAncestorNode(skin.joints, parentIndex);
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

        async Task<bool> CreateAllMeshAssignments()
        {
            foreach (var meshOrder in m_MeshOrders)
            {
                var mesh = await meshOrder.generator.CreateMeshResult();
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

        async Task WaitForAllMeshGenerators()
        {
            foreach (var meshOrder in m_MeshOrders)
            {
                if (meshOrder.generator == null) continue;
                while (!meshOrder.generator.IsCompleted)
                {
                    await Task.Yield();
                }
            }
        }

        async Task GenerateMaterials()
        {
            m_Materials = new UnityEngine.Material[Root.Materials.Count];
            for (var i = 0; i < m_Materials.Length; i++)
            {
                await DeferAgent.BreakPoint(.0001f);
                Profiler.BeginSample("GenerateMaterial");
                m_MaterialGenerator.SetLogger(Logger);
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

        void PopulateTexturesAndImageVariants()
        {
            var defaultKey = new SamplerKey(new Sampler());
            m_Textures = new Texture2D[Root.Textures.Count];
            var imageVariants = new Dictionary<SamplerKey, Texture2D>[m_Images.Length];
            for (var textureIndex = 0; textureIndex < Root.Textures.Count; textureIndex++)
            {
                var txt = Root.Textures[textureIndex];
                SamplerKey key;
                Sampler sampler = null;
                if (txt.sampler >= 0)
                {
                    sampler = Root.Samplers[txt.sampler];
                    key = new SamplerKey(sampler);
                }
                else
                {
                    key = defaultKey;
                }

                var imageIndex = txt.GetImageIndex();
                if (imageIndex < 0 || imageIndex >= Root.Images.Count) continue;
                var img = m_Images[imageIndex];
                if (imageVariants[imageIndex] == null)
                {
                    sampler?.Apply(img, m_Settings.DefaultMinFilterMode, m_Settings.DefaultMagFilterMode);
                    imageVariants[imageIndex] = new Dictionary<SamplerKey, Texture2D>
                    {
                        [key] = img
                    };
                    m_Textures[textureIndex] = img;
                }
                else
                {
                    if (imageVariants[imageIndex].TryGetValue(key, out var imgVariant))
                    {
                        m_Textures[textureIndex] = imgVariant;
                    }
                    else
                    {
                        Texture2D newImg;
                        if (img.isReadable)
                        {

                            newImg = UnityEngine.Object.Instantiate(img);
                            m_Resources.Add(newImg);
#if DEBUG
                            newImg.name = $"{img.name}_sampler{txt.sampler}";
                            Logger?.Warning(LogCode.ImageMultipleSamplers,imageIndex.ToString());
#endif
                            sampler?.Apply(newImg, m_Settings.DefaultMinFilterMode, m_Settings.DefaultMagFilterMode);
                        }
                        else
                        {
                            Logger?.Warning(
                                LogCode.TextureSamplerNotApplied,
                                txt.sampler >= 0 ? $"#{txt.sampler}" : "default",
                                textureIndex.ToString(),
                                imageIndex.ToString()
                                );
                            newImg = img;
                        }
                        imageVariants[imageIndex][key] = newImg;
                        m_Textures[textureIndex] = newImg;
                    }
                }
            }
        }

#if UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER
        async Task WaitForImageCreateContexts()
        {
            var imageCreateContextsLeft = true;
            while (imageCreateContextsLeft)
            {
                var loadedAny = false;
                for (var i = m_ImageCreateContexts.Count - 1; i >= 0; i--)
                {
                    var jh = m_ImageCreateContexts[i];
                    if (jh.jobHandle.IsCompleted)
                    {
                        jh.jobHandle.Complete();
                        Profiler.BeginSample("Texture2D.LoadImage");
                        m_Images[jh.imageIndex].LoadImage(
                            jh.buffer,
                            !LoadImageReadable(jh.imageIndex)
                        );
                        Profiler.EndSample();
                        jh.gcHandle.Free();
                        m_ImageCreateContexts.RemoveAt(i);
                        loadedAny = true;
                        await DeferAgent.BreakPoint();
                    }
                }
                imageCreateContextsLeft = m_ImageCreateContexts.Count > 0;
                if (!loadedAny && imageCreateContextsLeft)
                {
                    await Task.Yield();
                }
            }
            m_ImageCreateContexts = null;
        }
#endif // UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER

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
                if (node.children != null)
                {
                    childNames.Clear();
                    foreach (var child in node.children)
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
                if (scene.nodes != null)
                {
                    foreach (var nodeIndex in scene.nodes)
                    {
                        m_NodeNames[nodeIndex] = GetUniqueNodeName(Root, nodeIndex, childNames);
                    }
                }
            }

            return parentIndex;
        }

        static string GetUniqueNodeName(RootBase gltf, uint index, ICollection<string> excludeNames)
        {
            if (gltf.Nodes == null || index >= gltf.Nodes.Count) return null;
            var name = gltf.Nodes[(int)index].name;
            if (string.IsNullOrWhiteSpace(name))
            {
                var meshIndex = gltf.Nodes[(int)index].mesh;
                if (meshIndex >= 0)
                {
                    name = gltf.Meshes[meshIndex].name;
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
                    disposable.Dispose();
                }
                m_VolatileDisposables = null;
            }

            if (m_DownloadTasks != null)
            {
                foreach (var download in m_DownloadTasks.Values)
                {
                    download?.Dispose();
                }
                m_DownloadTasks = null;
            }

            m_AccessorUsage = null;

#if UNITY_IMAGECONVERSION
            m_TextureDownloadTasks = null;
#if !UNITY_6000_0_OR_NEWER
            m_ImageCreateContexts = null;
#endif
#endif

            m_Images = null;
            m_ImageFormats = null;
#if !UNITY_VISIONOS
            m_ImageReadable = null;
#endif
            m_ImageGamma = null;
            m_GlbBinChunk = null;
            m_MaterialPointsSupport = null;

#if MESHOPT
            if(m_MeshoptBufferViews!=null) {
                foreach (var nativeBuffer in m_MeshoptBufferViews.Values) {
                    nativeBuffer.Dispose();
                }
                m_MeshoptBufferViews = null;
            }
            if (m_MeshoptReturnValues.IsCreated) {
                m_MeshoptReturnValues.Dispose();
            }
#endif
        }

        async Task InstantiateSceneInternal(IInstantiator instantiator, int sceneId)
        {
            if (m_ImportInstances != null)
            {
                foreach (var extension in m_ImportInstances)
                {
                    extension.Value.Inject(instantiator);
                }
            }

            async Task IterateNodes(uint nodeIndex, uint? parentIndex, Action<uint, uint?> callback)
            {
                var node = this.Root.Nodes[(int)nodeIndex];
                callback(nodeIndex, parentIndex);
                await DeferAgent.BreakPoint();
                if (node.children != null)
                {
                    foreach (var child in node.children)
                    {
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

                var nodeName = m_NodeNames == null ? node.name : m_NodeNames[nodeIndex];
                if (nodeName == null && node.mesh >= 0)
                {
                    // Fallback name for Node is first valid Mesh name
                    foreach (var meshAssignment in m_MeshAssignments.Values(node.mesh))
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

                if (node.mesh >= 0)
                {
                    var meshNumeration = 0;
                    foreach (var meshAssignment in m_MeshAssignments.Values(node.mesh))
                    {
                        var mesh = meshAssignment.mesh;
                        var meshName = string.IsNullOrEmpty(mesh.name) ? null : mesh.name;
                        uint[] joints = null;
                        uint? rootJoint = null;

                        if (mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.BlendIndices))
                        {
                            if (node.skin >= 0)
                            {
                                var skin = Root.Skins[node.skin];
                                // TODO: see if this can be moved to mesh creation phase / before instantiation
                                mesh.bindposes = GetBindPoses(node.skin);
                                if (skin.skeleton >= 0)
                                {
                                    rootJoint = (uint)skin.skeleton;
                                }
                                joints = skin.joints;
                            }
                            else
                            {
                                Logger?.Warning(LogCode.SkinMissing);
                            }
                        }

                        var meshInstancing = node.Extensions?.EXT_mesh_gpu_instancing;

                        var meshResultName =
                            meshNumeration > 0
                                ? $"{meshName ?? k_PrimitiveName}_{meshNumeration}"
                                : meshName ?? k_PrimitiveName;

                        var meshResult = new MeshResult(
                            node.mesh,
                            meshAssignment.primitives,
                            GetMaterialIndices(node.mesh, meshAssignment.primitives),
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
                                node.weights ?? Root.Meshes[node.mesh].weights,
                                meshNumeration
                            );
                        }
                        else
                        {

                            var hasTranslations = meshInstancing.attributes.TRANSLATION > -1;
                            var hasRotations = meshInstancing.attributes.ROTATION > -1;
                            var hasScales = meshInstancing.attributes.SCALE > -1;

                            NativeArray<Vector3>? positions = null;
                            NativeArray<Quaternion>? rotations = null;
                            NativeArray<Vector3>? scales = null;
                            uint instanceCount = 0;

                            if (hasTranslations)
                            {
                                positions = ((NativeArray<float3>)m_AccessorData[meshInstancing.attributes.TRANSLATION]).Reinterpret<Vector3>();
                                instanceCount = (uint)positions.Value.Length;
                            }

                            if (hasRotations)
                            {
                                rotations = ((NativeArray<quaternion>)m_AccessorData[meshInstancing.attributes.ROTATION]).Reinterpret<Quaternion>();
                                instanceCount = (uint)rotations.Value.Length;
                            }

                            if (hasScales)
                            {
                                scales = ((NativeArray<float3>)m_AccessorData[meshInstancing.attributes.SCALE]).Reinterpret<Vector3>();
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

                if (node.camera >= 0
                    && Root.Cameras != null
                    && node.camera < Root.Cameras.Count
                    )
                {
                    instantiator.AddCamera(nodeIndex, (uint)node.camera);
                }

                if (node.Extensions?.KHR_lights_punctual != null && Root.Extensions?.KHR_lights_punctual?.lights != null)
                {
                    var lightIndex = node.Extensions.KHR_lights_punctual.light;
                    if (lightIndex < Root.Extensions.KHR_lights_punctual.lights.Length)
                    {
                        instantiator.AddLightPunctual(nodeIndex, (uint)lightIndex);
                    }
                }

                Profiler.EndSample();
            }

            var scene = this.Root.Scenes[sceneId];

            instantiator.BeginScene(scene.name, scene.nodes);
#if UNITY_ANIMATION
            instantiator.AddAnimation(m_AnimationClips);
#endif

            if (scene.nodes != null)
            {
                foreach (var nodeId in scene.nodes)
                {
                    await IterateNodes(nodeId, null, CreateHierarchy);
                }
                foreach (var nodeId in scene.nodes)
                {
                    await IterateNodes(nodeId, null, PopulateHierarchy);
                }
            }

            instantiator.EndScene(scene.nodes);
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
                if (Root.Nodes[i].children != null)
                {
                    foreach (var child in Root.Nodes[i].children)
                    {
                        parentIndex[child] = i;
                    }
                }
            }

            return parentIndex;
        }

        int[] GetMaterialIndices(int meshIndex, IReadOnlyList<int> primitiveIndices)
        {
            var result = new int[primitiveIndices.Count];
            for (var subMesh = 0; subMesh < primitiveIndices.Count; subMesh++)
            {
                var primitiveIndex = primitiveIndices[subMesh];
                var primitive = GetSourceMeshPrimitive(meshIndex, primitiveIndex);
                result[subMesh] = primitive.material;
            }

            return result;
        }

        async Task CreateTexturesFromBuffers(
            IReadOnlyList<Image> srcImages,
            IReadOnlyList<BufferViewBase> bufferViews
        )
        {
#if UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER
            m_ImageCreateContexts = new List<ImageCreateContext>();
#endif
            for (int i = 0; i < m_Images.Length; i++)
            {
                Profiler.BeginSample("CreateTexturesFromBuffers.ImageFormat");
                if (m_Images[i] != null)
                {
                    m_Resources.Add(m_Images[i]);
                }
                var img = srcImages[i];
                ImageFormat imgFormat = m_ImageFormats[i];
                if (imgFormat == ImageFormat.Unknown)
                {
                    imgFormat = string.IsNullOrEmpty(img.mimeType)
                        // Image is missing mime type
                        // try to determine type by file extension
                        ? UriHelper.GetImageFormatFromUri(img.uri)
                        : ImageFormatExtensions.FromMimeType(img.mimeType);
                }
                Profiler.EndSample();

                if (imgFormat != ImageFormat.Unknown && img.bufferView >= 0)
                {
                    if (imgFormat == ImageFormat.Ktx)
                    {
#if KTX_IS_ENABLED
                        Profiler.BeginSample("CreateTexturesFromBuffers.KtxLoadNativeContext");
                        if(m_KtxLoadContextsBuffer==null) {
                            m_KtxLoadContextsBuffer = new List<KtxLoadContextBase>();
                        }
                        var ktxContext = new KtxLoadContext(
                            i, ((IGltfBuffers)this).GetBufferView(img.bufferView, out _).AsNativeArrayReadOnly());
                        m_KtxLoadContextsBuffer.Add(ktxContext);
                        Profiler.EndSample();
                        await DeferAgent.BreakPoint();
#else
                        Logger?.Error(LogCode.PackageMissing, "KTX for Unity", ExtensionName.TextureBasisUniversal);
#endif // KTX_IS_ENABLED
                    }
                    else
                    {
#if UNITY_IMAGECONVERSION

                        var forceSampleLinear = m_ImageGamma != null && !m_ImageGamma[i];
                        var txt = CreateEmptyTexture(img, i, forceSampleLinear);
#if UNITY_6000_0_OR_NEWER
                        Profiler.BeginSample("Texture2D.LoadImage");
                        var data = ((IGltfBuffers)this).GetBufferView(img.bufferView, out _);
                        txt.LoadImage(data.AsNativeArrayReadOnly().AsReadOnlySpan(), !LoadImageReadable(i));
                        Profiler.EndSample();
                        await DeferAgent.BreakPoint();
#else // UNITY_6000_0_OR_NEWER
                        Profiler.BeginSample("CreateTexturesFromBuffers.ExtractBuffer");
                        var bufferView = bufferViews[img.bufferView];
                        var buffer = GetBuffer(bufferView.buffer);
                        var chunk = m_BinChunks[bufferView.buffer];

                        var icc = new ImageCreateContext();
                        icc.imageIndex = i;
                        icc.buffer = new byte[bufferView.byteLength];
                        icc.gcHandle = GCHandle.Alloc(icc.buffer, GCHandleType.Pinned);

                        var job = CreateMemCopyJob(bufferView, buffer, chunk, icc);
                        icc.jobHandle = job.Schedule();

                        m_ImageCreateContexts.Add(icc);
                        Profiler.EndSample();
#endif // UNITY_6000_0_OR_NEWER
                        m_Images[i] = txt;
                        m_Resources.Add(txt);
#else // UNITY_IMAGECONVERSION
                        Logger?.Warning(LogCode.ImageConversionNotEnabled);
#endif // UNITY_IMAGECONVERSION
                    }
                }
            }
#if !(UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER) && !KTX_IS_ENABLED
            await DeferAgent.BreakPoint();
#endif
        }

#if UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER
        static unsafe MemCopyJob CreateMemCopyJob(
            BufferViewBase bufferView,
            ReadOnlyNativeArray<byte> nativeArray,
            GlbBinChunk chunk,
            ImageCreateContext icc
            )
        {
            var job = new MemCopyJob
            {
                bufferSize = bufferView.byteLength,
                input = (byte*)nativeArray.GetUnsafeReadOnlyPtr() + (bufferView.byteOffset + chunk.Start)
            };
            fixed (void* dst = &(icc.buffer[0]))
            {
                job.result = dst;
            }

            return job;
        }
#endif // UNITY_IMAGECONVERSION && !UNITY_6000_0_OR_NEWER

        Texture2D CreateEmptyTexture(Image img, int index, bool forceSampleLinear)
        {
            var textureCreationFlags = TextureCreationFlags.DontUploadUponCreate | TextureCreationFlags.DontInitializePixels;
            if (m_Settings.GenerateMipMaps)
            {
                textureCreationFlags |= TextureCreationFlags.MipChain;
            }
            var txt = new Texture2D(
                4, 4,
                forceSampleLinear
                    ? GraphicsFormat.R8G8B8A8_UNorm
                    : GraphicsFormat.R8G8B8A8_SRGB,
                textureCreationFlags
            )
            {
                anisoLevel = m_Settings.AnisotropicFilterLevel,
                name = GetImageName(img, index)
            };
            return txt;
        }

        static string GetImageName(Image img, int index)
        {
            return string.IsNullOrEmpty(img.name) ? $"image_{index}" : img.name;
        }

        bool LoadImageReadable(int imageIndex)
        {
#if UNITY_VISIONOS
            // PolySpatial visionOS needs to be able to access raw texture data in order to
            // do the material/texture conversion.
            return true;
#else
            return m_Settings.TexturesReadable || m_ImageReadable[imageIndex];
#endif
        }

        static void SafeDestroy(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            else
#endif
            {
                UnityEngine.Object.Destroy(obj);
            }
        }

        /// <summary>Is called when retrieving data from accessors should be performed/started.</summary>
        public event Action LoadAccessorDataEvent;

        /// <summary>Is called when a mesh and its primitives are assigned to a <see cref="MeshResult"/> and
        /// sub-meshes. Parameters are MeshResult index, mesh index and per sub-mesh primitive index</summary>
        public event Action<int, int, int[]> MeshResultAssigned;

        async Task<bool> LoadAccessorData()
        {
            Profiler.BeginSample("LoadAccessorData.Init");

#if DEBUG
            // Detect and report poor shared accessor usage. Since this adds performance overhead, it's done in debug
            // mode only.
            var perPrimitiveSetIndices = new Dictionary<IReadOnlyList<MeshPrimitiveBase>,int[]>(
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
            var primitiveSets = new Dictionary<IReadOnlyList<MeshPrimitiveBase>, MeshOrder>(s_MeshComparer);
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

                    if (primitive.indices >= 0)
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
                            usage = primitive.mode == DrawMode.Triangles
                                ? AccessorUsage.IndexFlipped
                                : AccessorUsage.Index;
                        }
                        SetAccessorUsage(primitive.indices, usage);
                    }

                    if (primitive.material >= 0)
                    {
                        if (Root.Materials != null && primitive.mode == DrawMode.Points)
                        {
                            SetMaterialPointsSupport(primitive.material);
                        }
                    }
                    else
                    {
                        m_DefaultMaterialPointsSupport |= primitive.mode == DrawMode.Points;
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
                    if (skin.inverseBindMatrices >= 0)
                    {
                        SetAccessorUsage(skin.inverseBindMatrices, AccessorUsage.InverseBindMatrix);
                    }
                }
            }

            if (Root.Nodes != null)
            {
                foreach (var node in Root.Nodes)
                {
                    var attr = node.Extensions?.EXT_mesh_gpu_instancing?.attributes;
                    if (attr != null)
                    {
                        if (attr.TRANSLATION >= 0)
                        {
                            SetAccessorUsage(attr.TRANSLATION, AccessorUsage.Translation | AccessorUsage.RequiredForInstantiation);
                        }
                        if (attr.ROTATION >= 0)
                        {
                            SetAccessorUsage(attr.ROTATION, AccessorUsage.Rotation | AccessorUsage.RequiredForInstantiation);
                        }
                        if (attr.SCALE >= 0)
                        {
                            SetAccessorUsage(attr.SCALE, AccessorUsage.Scale | AccessorUsage.RequiredForInstantiation);
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

            if (!success)
            {
                return false;
            }

#if UNITY_ANIMATION
            if (Root.HasAnimation) {
                for (int i = 0; i < Root.Animations.Count; i++) {
                    var animation = Root.Animations[i];
                    foreach (var sampler in animation.Samplers) {
                        SetAccessorUsage(sampler.input,AccessorUsage.AnimationTimes);
                    }

                    foreach (var channel in animation.Channels) {
                        var accessorIndex = animation.Samplers[channel.sampler].output;
                        switch (channel.Target.GetPath()) {
                            case AnimationChannel.Path.Translation:
                                SetAccessorUsage(accessorIndex,AccessorUsage.Translation);
                                break;
                            case AnimationChannel.Path.Rotation:
                                SetAccessorUsage(accessorIndex,AccessorUsage.Rotation);
                                break;
                            case AnimationChannel.Path.Scale:
                                SetAccessorUsage(accessorIndex,AccessorUsage.Scale);
                                break;
                            case AnimationChannel.Path.Weights:
                                SetAccessorUsage(accessorIndex,AccessorUsage.Weight);
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
                if (acc.bufferView < 0)
                {
                    // Not actual accessor to data
                    // Common for draco meshes
                    // the accessor only holds meta information
                    continue;
                }
                switch (acc.GetAttributeType())
                {
                    case GltfAccessorAttributeType.MAT4 when m_AccessorUsage[i] == AccessorUsage.InverseBindMatrix:
                    {
                        // TODO: Maybe use Matrix4x4[], since Mesh.bindposes only accepts C# arrays.
                        GetMatricesJob(i, out var matrices, out var jh);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = matrices;
                        break;
                    }
                    case GltfAccessorAttributeType.VEC3 when (m_AccessorUsage[i] & AccessorUsage.Translation) != 0:
                    {
                        GetVector3Job(i, out var data, out var jh, true);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
                    case GltfAccessorAttributeType.VEC4 when (m_AccessorUsage[i] & AccessorUsage.Rotation) != 0:
                    {
                        GetVector4Job(i, out var data, out var jh);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
                    case GltfAccessorAttributeType.VEC3 when (m_AccessorUsage[i] & AccessorUsage.Scale) != 0:
                    {
                        GetVector3Job(i, out var data, out var jh, false);
                        tmpList.Add(jh.Value);
                        m_AccessorData[i] = data;
                        break;
                    }
#if UNITY_ANIMATION
                    case GltfAccessorAttributeType.SCALAR when m_AccessorUsage[i]==AccessorUsage.AnimationTimes || m_AccessorUsage[i]==AccessorUsage.Weight:
                    {
                        GetScalarJob(i, out var times, out var jh);
                        if (times.HasValue) {
                            m_AccessorData[i] = times.Value;
                        }
                        if (jh.HasValue) {
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
            Dictionary<IReadOnlyList<MeshPrimitiveBase>, int[]> perAttributeMeshCollection,
            PrimitiveSingle primitiveSingle
        )
        {
            return CheckVertexBufferUsage(perAttributeMeshCollection, new []{primitiveSingle.Primitive});
        }

        bool CheckVertexBufferUsage(
            Dictionary<IReadOnlyList<MeshPrimitiveBase>, int[]> perAttributeMeshCollection,
            PrimitiveSet primitiveSet
        )
        {
            return CheckVertexBufferUsage(perAttributeMeshCollection, primitiveSet.Primitives);
        }

        bool CheckVertexBufferUsage(
            Dictionary<IReadOnlyList<MeshPrimitiveBase>, int[]> perAttributeMeshCollection,
            IReadOnlyList<MeshPrimitiveBase> primitives
            )
        {
            if(perAttributeMeshCollection.TryGetValue(primitives, out var indicesAccessors))
            {
                Assert.AreEqual(primitives.Count, indicesAccessors.Length);
                var conflict = false;
                for (var index = 0; index < indicesAccessors.Length; index++)
                {
                    if (indicesAccessors[index] != primitives[index].indices)
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
                indicesAccessors = new int[primitives.Count];
                // Original will be disposed, so make a copy.
                var primitiveArray = new MeshPrimitiveBase[primitives.Count];
                for (var i = 0; i < primitives.Count; i++)
                {
                    indicesAccessors[i] = primitives[i].indices;
                    primitiveArray[i] = primitives[i];
                }
                perAttributeMeshCollection[primitiveArray] = indicesAccessors;
            }

            return false;
        }
#endif

        MeshOrder CreateMeshOrder(
            IPrimitiveSet primitiveSet,
            MeshBase mesh,
            int meshIndex,
            int meshNumeration,
            out int[] primIndexArray,
            out MeshPrimitiveBase[] primitives
            )
        {
            var morphTargetNames = primitiveSet.HasMorphTargets
                ? mesh.Extras?.targetNames
                : null;

            MeshGeneratorBase generator;
            primitiveSet.BuildAndDispose(out primIndexArray, out primitives, out var subMeshes);
            var meshSubset = new MeshSubset(meshIndex, meshNumeration, primIndexArray);
#if DRACO_IS_ENABLED
            if (primitives[0].IsDracoCompressed)
            {
                generator = new DracoMeshGenerator(primitives, morphTargetNames, mesh.name, this);
            }
            else
#endif
            {
                generator = new MeshGenerator(primitives, subMeshes, morphTargetNames, mesh.name, this);
            }

            var meshOrder = new MeshOrder(generator);
            meshOrder.AddRecipient(meshSubset);

            return meshOrder;
        }

        void SetAccessorUsage(int index, AccessorUsage newUsage)
        {
#if DEBUG
            if(m_AccessorUsage[index]!=AccessorUsage.Unknown && newUsage!=m_AccessorUsage[index]) {
                Logger?.Error(LogCode.AccessorInconsistentUsage, m_AccessorUsage[index].ToString(), newUsage.ToString());
            }
#endif
            m_AccessorUsage[index] = newUsage;
        }

        async Task AssignAllAccessorData()
        {
            if (Root.Skins != null)
            {
                for (int s = 0; s < Root.Skins.Count; s++)
                {
                    Profiler.BeginSample("AssignAllAccessorData.Skin");
                    var skin = Root.Skins[s];
                    if (skin.inverseBindMatrices >= 0)
                    {
                        m_SkinsInverseBindMatrices[s] =
                            ((NativeArray<float4x4>)m_AccessorData[skin.inverseBindMatrices])
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
                accessor.bufferView,
                out _,
                accessor.byteOffset,
                accessor.ByteSize
                );

            Profiler.BeginSample("Alloc");
            matrices = new NativeArray<float4x4>(accessor.count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.MAT4);
            //Assert.AreEqual(accessor.count * GetLength(accessor.typeEnum) * 4 , (int) chunk.length);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Matrix");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.componentType)
            {
                case GltfComponentType.Float:
                    var job32 = new ConvertMatricesJob
                    {
                        input = accessorData.Reinterpret<float4x4>().AsNativeArrayReadOnly(),
                        result = matrices
                    };
                    jobHandle = job32.Schedule(accessor.count, DefaultBatchCount);
                    break;
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
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
            vectors = new NativeArray<float3>(accessor.count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.VEC3);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Vector3");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.componentType)
            {
                case GltfComponentType.Float:
                {
                    if (flip)
                    {
                        var accessorData = ((IGltfBuffers)this).GetStridedAccessorData<float3>(
                            accessor.bufferView,
                            accessor.count,
                            accessor.byteOffset
                        );
                        var job = new ConvertVector3FloatToFloatJob
                        {
                            input = accessorData,
                            result = vectors
                        };
                        jobHandle = job.Schedule(accessor.count, DefaultBatchCount);
                    }
                    else
                    {
                        var accessorData = ((IGltfBuffers)this).GetAccessorData<float3>(
                            accessor.bufferView,
                            accessor.count,
                            accessor.byteOffset
                        );
                        var job = new MemCopyJob
                        {
                            input = (float*)accessorData.GetUnsafeReadOnlyPtr(),
                            bufferSize = accessor.count * 12,
                            result = (float*)vectors.GetUnsafePtr()
                        };
                        jobHandle = job.Schedule();
                    }
                    break;
                }
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
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
                accessor.bufferView,
                out _,
                accessor.byteOffset,
                accessor.ByteSize
                );

            Profiler.BeginSample("Alloc");
            vectors = new NativeArray<quaternion>(accessor.count, Allocator.Persistent);
            Profiler.EndSample();

            Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.VEC4);
            if (accessor.IsSparse)
            {
                Logger?.Error(LogCode.SparseAccessor, "Vector4");
            }

            Profiler.BeginSample("CreateJob");
            switch (accessor.componentType)
            {
                case GltfComponentType.Float:
                {
                    var job = new ConvertRotationsFloatToFloatJob
                    {
                        input = accessorData.Reinterpret<float4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.count, DefaultBatchCount);
                    break;
                }
                case GltfComponentType.Short:
                {
                    var job = new ConvertRotationsInt16ToFloatJob
                    {
                        input = accessorData.Reinterpret<short4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.count, DefaultBatchCount);
                    break;
                }
                case GltfComponentType.Byte:
                {
                    var job = new ConvertRotationsInt8ToFloatJob
                    {
                        input = accessorData.Reinterpret<sbyte4>().AsNativeArrayReadOnly(),
                        result = vectors
                    };
                    jobHandle = job.Schedule(accessor.count, DefaultBatchCount);
                    break;
                }
                default:
                    Logger?.Error(LogCode.IndexFormatInvalid, accessor.componentType.ToString());
                    jobHandle = null;
                    break;
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

#if UNITY_ANIMATION
        unsafe void GetScalarJob(int accessorIndex, out NativeArray<float>? scalars, out JobHandle? jobHandle) {
            Profiler.BeginSample("GetScalarJob");
            scalars = null;
            jobHandle = null;
            var accessor = Root.Accessors[accessorIndex];
            var accessorData = ((IGltfBuffers)this).GetBufferView(
                accessor.bufferView,
                out _,
                accessor.byteOffset,
                accessor.ByteSize
                );

            Assert.AreEqual(accessor.GetAttributeType(), GltfAccessorAttributeType.SCALAR);
            if (accessor.IsSparse) {
                Logger?.Error(LogCode.SparseAccessor,"scalars");
            }

            if (accessor.componentType == GltfComponentType.Float) {
                Profiler.BeginSample("CopyAnimationTimes");
                var bufferTimes = accessorData
                    .Reinterpret<float>()
                    .GetSubArray(0, accessor.count);
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
            } else
            if( accessor.normalized ) {
                Profiler.BeginSample("Alloc");
                scalars = new NativeArray<float>(accessor.count,Allocator.Persistent);
                Profiler.EndSample();

                switch( accessor.componentType ) {
                    case GltfComponentType.Byte: {
                        var job = new ConvertScalarInt8ToFloatNormalizedJob {
                            input = accessorData.Reinterpret<sbyte>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.count,DefaultBatchCount);
                        break;
                    }
                    case GltfComponentType.UnsignedByte: {
                        var job = new ConvertScalarUInt8ToFloatNormalizedJob {
                            input = accessorData.Reinterpret<byte>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.count,DefaultBatchCount);
                        break;
                    }
                    case GltfComponentType.Short: {
                        var job = new ConvertScalarInt16ToFloatNormalizedJob {
                            input = accessorData.Reinterpret<short>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.count,DefaultBatchCount);
                        break;
                    }
                    case GltfComponentType.UnsignedShort: {
                        var job = new ConvertScalarUInt16ToFloatNormalizedJob {
                            input = accessorData.Reinterpret<ushort>().AsNativeArrayReadOnly(),
                            result = scalars.Value
                        };
                        jobHandle = job.Schedule(accessor.count,DefaultBatchCount);
                        break;
                    }
                    default:
                        Logger?.Error(LogCode.AnimationFormatInvalid, accessor.componentType.ToString());
                        break;
                }
            } else {
                // Non-normalized
                Logger?.Error(LogCode.AnimationFormatInvalid, accessor.componentType.ToString());
            }
            Profiler.EndSample();
        }

#endif // UNITY_ANIMATION

        AccessorBase IGltfBuffers.GetAccessor(int index)
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
        unsafe void IGltfBuffers.GetAccessorAndData(int index, out AccessorBase accessor, out void* data, out int byteStride)
        {
            accessor = Root.Accessors[index];
            if (accessor.bufferView < 0 || accessor.bufferView >= Root.BufferViews.Count)
            {
                data = null;
                byteStride = 0;
                return;
            }
            var bufferView = Root.BufferViews[accessor.bufferView];
#if MESHOPT
            var meshopt = bufferView.Extensions?.EXT_meshopt_compression;
            if (meshopt != null) {
                byteStride = meshopt.byteStride;
                data = (byte*)m_MeshoptBufferViews[accessor.bufferView].GetUnsafeReadOnlyPtr() + accessor.byteOffset;
            } else
#endif
            {
                byteStride = bufferView.byteStride;
                var bufferIndex = bufferView.buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (accessor.byteOffset + bufferView.byteOffset + m_BinChunks[bufferIndex].Start);
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
            var bufferView = Root.BufferViews[(int)sparseIndices.bufferView];
#if MESHOPT
            var meshopt = bufferView.Extensions?.EXT_meshopt_compression;
            if (meshopt != null) {
                data = (byte*)m_MeshoptBufferViews[(int)sparseIndices.bufferView].GetUnsafeReadOnlyPtr() + sparseIndices.byteOffset;
            }
            else
#endif
            {
                var bufferIndex = bufferView.buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (sparseIndices.byteOffset + bufferView.byteOffset + m_BinChunks[bufferIndex].Start);
            }
        }

        /// <summary>
        /// Get sparse value raw data
        /// </summary>
        /// <param name="sparseValues">glTF sparse values accessor</param>
        /// <param name="data">Pointer to accessor's data in memory</param>
        public unsafe void GetAccessorSparseValues(AccessorSparseValues sparseValues, out void* data)
        {
            var bufferView = Root.BufferViews[(int)sparseValues.bufferView];
#if MESHOPT
            var meshopt = bufferView.Extensions?.EXT_meshopt_compression;
            if (meshopt != null) {
                data = (byte*)m_MeshoptBufferViews[(int)sparseValues.bufferView].GetUnsafeReadOnlyPtr() + sparseValues.byteOffset;
            }
            else
#endif
            {
                var bufferIndex = bufferView.buffer;
                var buffer = GetBuffer(bufferIndex);
                data = (byte*)buffer.GetUnsafeReadOnlyPtr()
                    + (sparseValues.byteOffset + bufferView.byteOffset + m_BinChunks[bufferIndex].Start);
            }
        }

#if KTX_IS_ENABLED
        struct KtxTranscodeTaskWrapper {
            public int index;
            public TextureResult result;
        }

        static async Task<KtxTranscodeTaskWrapper> KtxLoadAndTranscode(
            int index,
            KtxLoadContextBase ktx,
            bool linear,
            bool readable
            )
        {
            return new KtxTranscodeTaskWrapper {
                index = index,
                result = await ktx.LoadTexture2D(linear, readable)
            };
        }

        async Task ProcessKtxLoadContexts() {
            var maxCount = SystemInfo.processorCount+1;

            var totalCount = m_KtxLoadContextsBuffer.Count;
            var startedCount = 0;
            var ktxTasks = new List<Task<KtxTranscodeTaskWrapper>>(maxCount);

            while (startedCount < totalCount || ktxTasks.Count>0) {
                while (ktxTasks.Count < maxCount && startedCount < totalCount) {
                    var ktx = m_KtxLoadContextsBuffer[startedCount];
                    var forceSampleLinear = m_ImageGamma != null && !m_ImageGamma[ktx.imageIndex];
                    var readable = LoadImageReadable(ktx.imageIndex);
                    ktxTasks.Add(KtxLoadAndTranscode(startedCount, ktx, forceSampleLinear, readable));
                    startedCount++;
                    await DeferAgent.BreakPoint();
                }

                var kTask = await Task.WhenAny(ktxTasks);
                var i = kTask.Result.index;
                if (kTask.Result.result.errorCode == ErrorCode.Success) {
                    var ktx = m_KtxLoadContextsBuffer[i];
                    m_Images[ktx.imageIndex] = kTask.Result.result.texture;
                    if (!kTask.Result.result.orientation.IsYFlipped())
                    {
                        m_NonFlippedYTextureIndices ??= new HashSet<int>();
                        m_NonFlippedYTextureIndices.Add(ktx.imageIndex);
                    }
                    await DeferAgent.BreakPoint();
                }
                ktxTasks.Remove(kTask);
            }

            m_KtxLoadContextsBuffer.Clear();
        }
#endif // KTX_IS_ENABLED

#if UNITY_EDITOR
        /// <summary>
        /// Returns true if this import is for an asset, in contrast to
        /// runtime loading.
        /// </summary>
        static bool IsEditorImport => !EditorApplication.isPlaying;
#endif // UNITY_EDITOR
    }
}
