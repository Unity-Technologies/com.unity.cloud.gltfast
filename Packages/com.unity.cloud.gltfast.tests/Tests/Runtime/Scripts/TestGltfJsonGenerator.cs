// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Mathematics;
using Application = UnityEngine.Application;

namespace GLTFast.Tests
{
    [Flags]
    enum GltfFeatures
    {
        None = 0,
        Materials = 1 << 0,
        Extras = 1 << 1,
        Xmp = 1 << 2,
        Animation = 1 << 3,
        Cameras = 1 << 4,
        Lights = 1 << 5,
        SubMeshes = 1 << 6,
        DataUri = 1 << 7,
        Omni = Materials | Extras | Xmp | Animation | Cameras | Lights | SubMeshes
    }

    [Flags]
    enum TransformType
    {
        None = 0,
        Translation = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
        Matrix = 1 << 3,
        Simple = 1 << 4,
        TRS = Translation | Rotation | Scale,
        TranslationSimple = Translation | Simple,
    }

    class TestGltfConfiguration
    {
        const int k_DefaultBranching = 7;
        const int k_DefaultDepth = 4;

        public string name;
        public GltfFeatures features;
        public int branching = k_DefaultBranching;
        public int depth = k_DefaultDepth;
        public int animationCount;
        public int channelsPerAnimation;
        public int materialCount;
        public int meshCount;
        public int primitivesPerMesh;
        public TransformType transforms = TransformType.TRS;
        public int xmpPacketCount;
        public bool uniqueXmpPerNode;
        public int lightCount;
        public int cameraCount;
        public int bufferDataUriSize;
        public int imageDataUriLength;
        public bool namedNodes = true;

        public string GetPath() => Path.Combine(TestGltfJsonGenerator.FolderPath, $"{name}.gltf");
        public string GetPath(string folderPath) => Path.Combine(folderPath, $"{name}.gltf");

        public bool Has(GltfFeatures f) => (features & f) != 0;
    }

    /// <summary>
    /// Procedurally generates glTF JSON files (no binary payload) for JSON
    /// deserialization performance tests. Scenarios cover real-world variants
    /// of a balanced node hierarchy plus a data-URI heavy minimal scene.
    /// </summary>
    static class TestGltfJsonGenerator
    {
        const string k_FolderName = "gltf-perf";

        internal const int k_DefaultMaterialCount = 16;
        internal const int k_DefaultMeshCount = 16;
        internal const int k_DefaultPrimitivesPerMesh = 6;
        internal const int k_DefaultAnimationCount = 16;
        internal const int k_DefaultChannelsPerAnimation = 6;
        internal const int k_DefaultMorphTargetsPerMesh = 4;
        internal const int k_DefaultLightCount = 12;
        internal const int k_DefaultCameraCount = 6;
        internal const int k_DefaultSkinCount = 8;
        internal const int k_DefaultJointsPerSkin = 20;
        internal const int k_DefaultXmpPacketCount = 8;

        const int k_DefaultBufferDataUriSize = 128 * 1024;
        const int k_DefaultImageDataUriSize = 8 * 1024;

        public static string FolderPath =>
            Path.Combine(Application.streamingAssetsPath, k_FolderName);

        public static TestGltfConfiguration[] Configurations = {
            new() { name = "hierarchy-complex"},
            new() { name = "hierarchy-complex-matrix", branching = 6, transforms = TransformType.Matrix},
            new() { name = "hierarchy-complex-no-names", namedNodes = false },
            new() { name = "hierarchy-complex-min-transforms", branching = 9, transforms = TransformType.TranslationSimple },
            new() { name = "hierarchy-complex-shallow", branching = 2_500, depth = 1 },
            new() { name = "materials", branching = 1, depth = 0, materialCount = 512, transforms = TransformType.TranslationSimple, features =  GltfFeatures.Materials },
            new() { name = "extras", branching = 10, depth = 3, features = GltfFeatures.Extras, transforms = TransformType.TranslationSimple },
            new() { name = "xmp", branching = 22, depth = 2, features =  GltfFeatures.Xmp, uniqueXmpPerNode = true, transforms = TransformType.TranslationSimple },
            new() { name = "animation", branching = 3, depth = 2, animationCount = 256, channelsPerAnimation = 8, features =  GltfFeatures.Animation },
            new() { name = "cameras", branching = 1, depth = 0, cameraCount = 3000, features = GltfFeatures.Cameras, transforms = TransformType.None },
            new() { name = "lights", branching = 1, depth = 0, lightCount = 3000, features = GltfFeatures.Lights, transforms = TransformType.None },
            new() { name = "sub-meshes", branching = 1, depth = 0, meshCount = 800, primitivesPerMesh = 6, materialCount = 8, transforms = TransformType.TranslationSimple, features = GltfFeatures.SubMeshes },
            new() { name = "data-uri", bufferDataUriSize = k_DefaultBufferDataUriSize, imageDataUriLength = k_DefaultImageDataUriSize, meshCount = 1, materialCount = 1, branching = 1, depth = 0, features = GltfFeatures.DataUri | GltfFeatures.Materials },
            new() { name = "omni", branching = 5, features = GltfFeatures.Omni},
        };

        internal static void CreateMissing()
        {
            if (!Directory.Exists(FolderPath))
                Directory.CreateDirectory(FolderPath);
            foreach (var config in Configurations)
            {
                var path = config.GetPath();
                if (!File.Exists(path)) Generate(path, config);
            }
        }

        /// <summary>
        /// Generates every scenario into <paramref name="folder"/>, overwriting
        /// any existing files. Used by the determinism test; not for normal
        /// runtime use (which goes through <see cref="CreateMissing"/>).
        /// </summary>
        internal static void GenerateAll(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            foreach (var config in Configurations)
            {
                var path = config.GetPath(folder);
                Generate(path, config);
            }
        }

        internal static bool AllFilesPresent()
        {
            foreach (var config in Configurations)
            {
                var path = config.GetPath();
                if (!File.Exists(path))
                {
                    return false;
                }
            }

            return true;
        }

        static void Generate(
            string path,
            TestGltfConfiguration config)
        {
            EnsureFolder(path);
            using var builder = new GltfJsonBuilder(path);
            builder.WriteScenario(config);
        }

        static void EnsureFolder(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }
    }

    /// <summary>
    /// Emits glTF JSON to a file via Newtonsoft.Json.JsonTextWriter. Holds the
    /// shared per-scenario state (hierarchy, RNG, accessor cursor).
    /// </summary>
    sealed class GltfJsonBuilder : IDisposable
    {
        const int k_Seed = 0xCAFE;
        const string k_Generator = "GLTFast.Tests TestGltfJsonGenerator";

        // Accessor layout: 0..3 are the shared mesh attribute accessors.
        const int k_AccessorPosition = 0;
        const int k_AccessorNormal = 1;
        const int k_AccessorTexcoord = 2;
        const int k_AccessorIndices = 3;
        const int k_FirstDynamicAccessor = 4;

        readonly FileStream m_Stream;
        readonly StreamWriter m_Text;
        readonly CompactJsonWriter m_Jw;

        Xorshift32 m_Rng;

        TestGltfConfiguration m_Config;

        int m_Branching;
        int m_Depth;
        int[] m_LevelStart;
        int m_TotalNodes;
        int m_LeafStart;

        int m_MaterialCount;
        int m_MeshCount;
        int m_PrimitivesPerMesh;
        int m_AnimationCount;
        int m_ChannelsPerAnimation;
        int m_MorphTargetCount;
        int m_LightCount;
        int m_CameraCount;
        int m_SkinCount;
        int m_JointsPerSkin;
        int m_XmpPacketCount;

        int m_AccessorCursor;

        int m_AnimationTimeAccessor;
        int m_AnimationOutputAccessorStart;
        int m_MorphPositionAccessorStart;
        int m_IbmAccessorStart;

        public GltfJsonBuilder(string path)
        {
            m_Stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            m_Text = new StreamWriter(m_Stream, new UTF8Encoding(false))
            {
                NewLine = "\n"
            };
            m_Jw = new CompactJsonWriter(m_Text);
        }

        public void Dispose()
        {
            m_Jw?.Flush();
            m_Text?.Dispose();
            m_Stream?.Dispose();
        }

        public void WriteScenario(TestGltfConfiguration config)
        {
            if (config.branching < 1) throw new ArgumentOutOfRangeException(nameof(config.branching));
            if (config.depth < 0) throw new ArgumentOutOfRangeException(nameof(config.depth));

            m_Rng = new Xorshift32(k_Seed);
            m_Config = config;

            ComputeHierarchy(config.branching, config.depth);
            ConfigureCounts();

            m_Jw.WriteStartObject();

            WriteAsset();
            WriteExtensionsUsed();
            WriteRootExtensions();
            WriteSceneSection();
            WriteNodes();
            WriteMeshes();

            if (m_Config.Has(GltfFeatures.Materials))
            {
                WriteMaterials();
                WriteTextures();
                WriteImages(config.imageDataUriLength);
                WriteSamplers();
            }

            if (m_Config.Has(GltfFeatures.Cameras))
            {
                WriteCameras();
            }

            if (m_Config.Has(GltfFeatures.Animation))
            {
                WriteSkins();
                WriteAnimations();
            }

            WriteAccessors();
            WriteBufferViews();
            WriteBuffers(m_Config.bufferDataUriSize);

            m_Jw.WriteEndObject();
            m_Jw.Flush();
        }

        void ComputeHierarchy(int branching, int depth)
        {
            m_Branching = branching;
            m_Depth = depth;
            m_LevelStart = new int[depth + 2];
            m_LevelStart[0] = 0;
            var countAtLevel = 1;
            for (var l = 0; l <= depth; l++)
            {
                m_LevelStart[l + 1] = m_LevelStart[l] + countAtLevel;
                countAtLevel *= branching;
            }
            m_TotalNodes = m_LevelStart[depth + 1];
            m_LeafStart = m_LevelStart[depth];
        }

        void ConfigureCounts()
        {
            m_MaterialCount = m_Config.Has(GltfFeatures.Materials)
                ? m_Config.materialCount > 0 ? m_Config.materialCount : TestGltfJsonGenerator.k_DefaultMaterialCount
                : 0;
            if (m_Config.Has(GltfFeatures.SubMeshes))
            {
                m_MeshCount = m_Config.meshCount > 0 ? m_Config.meshCount : TestGltfJsonGenerator.k_DefaultMeshCount;
                m_PrimitivesPerMesh = m_Config.primitivesPerMesh > 0
                    ? m_Config.primitivesPerMesh
                    : TestGltfJsonGenerator.k_DefaultPrimitivesPerMesh;
            }
            else
            {
                m_MeshCount = 1;
                m_PrimitivesPerMesh = 1;
            }
            m_AnimationCount = m_Config.Has(GltfFeatures.Animation)
                ? m_Config.animationCount > 0 ? m_Config.animationCount : TestGltfJsonGenerator.k_DefaultAnimationCount
                : 0;
            m_ChannelsPerAnimation = m_Config.channelsPerAnimation > 0
                ? m_Config.channelsPerAnimation
                : TestGltfJsonGenerator.k_DefaultChannelsPerAnimation;
            m_MorphTargetCount = m_Config.Has(GltfFeatures.Animation) ? TestGltfJsonGenerator.k_DefaultMorphTargetsPerMesh : 0;
            m_LightCount = m_Config.Has(GltfFeatures.Lights)
                ? m_Config.lightCount > 0 ? m_Config.lightCount : TestGltfJsonGenerator.k_DefaultLightCount
                : 0;
            m_CameraCount = m_Config.Has(GltfFeatures.Cameras)
                ? m_Config.cameraCount > 0 ? m_Config.cameraCount : TestGltfJsonGenerator.k_DefaultCameraCount
                : 0;
            m_SkinCount = m_Config.Has(GltfFeatures.Animation) ? TestGltfJsonGenerator.k_DefaultSkinCount : 0;
            m_JointsPerSkin = TestGltfJsonGenerator.k_DefaultJointsPerSkin;
            if (m_Config.Has(GltfFeatures.Xmp))
            {
                m_XmpPacketCount = m_Config.uniqueXmpPerNode
                    ? m_TotalNodes
                    : m_Config.xmpPacketCount > 0 ? m_Config.xmpPacketCount : TestGltfJsonGenerator.k_DefaultXmpPacketCount;
            }
            else
            {
                m_XmpPacketCount = 0;
            }

            m_AccessorCursor = k_FirstDynamicAccessor;

            m_AnimationTimeAccessor = -1;
            m_AnimationOutputAccessorStart = -1;
            m_MorphPositionAccessorStart = -1;
            m_IbmAccessorStart = -1;

            if (m_Config.Has(GltfFeatures.Animation))
            {
                m_AnimationTimeAccessor = m_AccessorCursor++;

                m_AnimationOutputAccessorStart = m_AccessorCursor;
                m_AccessorCursor += m_AnimationCount * m_ChannelsPerAnimation;

                m_MorphPositionAccessorStart = m_AccessorCursor;
                m_AccessorCursor += m_MorphTargetCount
                    * (m_Config.Has(GltfFeatures.Animation) ? m_MorphTargetCount : 1);

                m_IbmAccessorStart = m_AccessorCursor;
                m_AccessorCursor += m_SkinCount;
            }
        }

        void WriteAsset()
        {
            m_Jw.WritePropertyName("asset");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("version");
            m_Jw.WriteValue("2.0");
            m_Jw.WritePropertyName("generator");
            m_Jw.WriteValue(k_Generator);
            if (m_Config.Has(GltfFeatures.Xmp))
            {
                m_Jw.WritePropertyName("extensions");
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("KHR_xmp_json_ld");
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("packet");
                m_Jw.WriteValue(0);
                m_Jw.WriteEndObject();
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndObject();
        }

        void WriteExtensionsUsed()
        {
            var used = new List<string>();
            if (m_Config.Has(GltfFeatures.Materials))
            {
                used.Add("KHR_materials_ior");
                used.Add("KHR_materials_specular");
                used.Add("KHR_materials_clearcoat");
            }
            if (m_Config.Has(GltfFeatures.Xmp))
                used.Add("KHR_xmp_json_ld");
            if (m_Config.Has(GltfFeatures.Lights))
                used.Add("KHR_lights_punctual");
            if (used.Count == 0)
                return;
            m_Jw.WritePropertyName("extensionsUsed");
            m_Jw.WriteStartArray();
            foreach (var u in used)
                m_Jw.WriteValue(u);
            m_Jw.WriteEndArray();
        }

        void WriteRootExtensions()
        {
            if (!m_Config.Has(GltfFeatures.Xmp) && !m_Config.Has(GltfFeatures.Lights))
                return;
            m_Jw.WritePropertyName("extensions");
            m_Jw.WriteStartObject();
            if (m_Config.Has(GltfFeatures.Xmp))
                WriteXmpPackets();
            if (m_Config.Has(GltfFeatures.Lights))
                WriteLightsPunctualRoot();
            m_Jw.WriteEndObject();
        }

        void WriteXmpPackets()
        {
            m_Jw.WritePropertyName("KHR_xmp_json_ld");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("packets");
            m_Jw.WriteStartArray();
            for (var i = 0; i < m_XmpPacketCount; i++)
                WriteXmpPacket(i);
            m_Jw.WriteEndArray();
            m_Jw.WriteEndObject();
        }

        void WriteXmpPacket(int i)
        {
            m_Jw.WriteStartObject();

            m_Jw.WritePropertyName("@context");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("dc");
            m_Jw.WriteValue("https://purl.org/dc/elements/1.1/");
            m_Jw.WritePropertyName("dcterms");
            m_Jw.WriteValue("https://purl.org/dc/terms/");
            m_Jw.WritePropertyName("xmp");
            m_Jw.WriteValue("https://ns.adobe.com/xap/1.0/");
            m_Jw.WritePropertyName("xmpRights");
            m_Jw.WriteValue("https://ns.adobe.com/xap/1.0/rights/");
            m_Jw.WritePropertyName("model3d");
            m_Jw.WriteValue("https://schema.khronos.org/model3d/xsd/1.0/");
            m_Jw.WriteEndObject();

            WriteLocalized("dc:title", $"Performance Asset {i}");
            WriteLocalized("dc:description",
                $"Procedurally generated KHR_xmp_json_ld packet number {i} used by glTFast JSON deserialization performance tests.");
            WriteLocalized("dc:rights", "Apache-2.0");
            WriteLocalized("xmpRights:UsageTerms",
                "This metadata packet is auto-generated for performance benchmarking and carries no warranty of any kind.");

            WriteList("dc:creator", "GLTFast Performance Suite", "TestGltfJsonGenerator", "Unity Technologies");

            WriteList("dc:subject",
                "performance", "benchmark", "glTF", "metadata", "KHR_xmp_json_ld",
                $"node-{i}", $"asset-group-{i % 16}");

            m_Jw.WritePropertyName("dc:identifier");
            m_Jw.WriteValue($"urn:gltfast:perf:packet:{i:D8}");

            m_Jw.WritePropertyName("dc:type");
            m_Jw.WriteValue("InteractiveResource");

            m_Jw.WritePropertyName("dc:format");
            m_Jw.WriteValue("model/gltf+json");

            m_Jw.WritePropertyName("dcterms:created");
            m_Jw.WriteValue("2026-01-01T00:00:00Z");

            m_Jw.WritePropertyName("dcterms:modified");
            m_Jw.WriteValue("2026-06-26T12:00:00Z");

            m_Jw.WritePropertyName("xmp:MetadataDate");
            m_Jw.WriteValue("2026-06-26T12:00:00Z");

            m_Jw.WritePropertyName("xmpRights:Marked");
            m_Jw.WriteValue(true);

            m_Jw.WritePropertyName("xmpRights:WebStatement");
            m_Jw.WriteValue("https://example.com/licenses/perf-asset");

            WriteList("model3d:preferredSurfaces", "horizontal", "vertical");
            WriteList("model3d:tags",
                "test", "synthetic", "deterministic", $"seed-{i & 0xFF}",
                "json-parse", "stress", "balanced");

            m_Jw.WritePropertyName("model3d:category");
            m_Jw.WriteValue("benchmark");

            m_Jw.WritePropertyName("model3d:complexity");
            m_Jw.WriteValue(i % 5);

            m_Jw.WriteEndObject();
        }

        void WriteLocalized(string property, string value)
        {
            m_Jw.WritePropertyName(property);
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("@type");
            m_Jw.WriteValue("rdf:Alt");
            m_Jw.WritePropertyName("rdf:_1");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("@language");
            m_Jw.WriteValue("en-US");
            m_Jw.WritePropertyName("@value");
            m_Jw.WriteValue(value);
            m_Jw.WriteEndObject();
            m_Jw.WriteEndObject();
        }

        void WriteList(string property, params string[] items)
        {
            m_Jw.WritePropertyName(property);
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("@list");
            m_Jw.WriteStartArray();
            foreach (var item in items)
                m_Jw.WriteValue(item);
            m_Jw.WriteEndArray();
            m_Jw.WriteEndObject();
        }

        void WriteLightsPunctualRoot()
        {
            m_Jw.WritePropertyName("KHR_lights_punctual");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("lights");
            m_Jw.WriteStartArray();
            string[] types = { "directional", "point", "spot" };
            for (var i = 0; i < m_LightCount; i++)
            {
                var type = types[i % types.Length];
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Light_{i:D2}_{type}");
                m_Jw.WritePropertyName("type");
                m_Jw.WriteValue(type);
                m_Jw.WritePropertyName("color");
                m_Jw.WriteStartArray();
                m_Jw.WriteValue(NextUnit());
                m_Jw.WriteValue(NextUnit());
                m_Jw.WriteValue(NextUnit());
                m_Jw.WriteEndArray();
                m_Jw.WritePropertyName("intensity");
                m_Jw.WriteValue(NextFloat(100f, 5000f));
                if (type != "directional")
                {
                    m_Jw.WritePropertyName("range");
                    m_Jw.WriteValue(NextFloat(2f, 50f));
                }
                if (type == "spot")
                {
                    m_Jw.WritePropertyName("spot");
                    m_Jw.WriteStartObject();
                    var inner = NextFloat(0.05f, 0.4f);
                    m_Jw.WritePropertyName("innerConeAngle");
                    m_Jw.WriteValue(inner);
                    m_Jw.WritePropertyName("outerConeAngle");
                    m_Jw.WriteValue(inner + NextFloat(0.05f, 0.8f));
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
            m_Jw.WriteEndObject();
        }

        void WriteSceneSection()
        {
            m_Jw.WritePropertyName("scene");
            m_Jw.WriteValue(0);

            m_Jw.WritePropertyName("scenes");
            m_Jw.WriteStartArray();
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("nodes");
            m_Jw.WriteStartArray();
            m_Jw.WriteValue(0);
            m_Jw.WriteEndArray();
            if (m_Config.Has(GltfFeatures.Xmp))
            {
                m_Jw.WritePropertyName("extensions");
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("KHR_xmp_json_ld");
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("packet");
                m_Jw.WriteValue(0);
                m_Jw.WriteEndObject();
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndObject();
            m_Jw.WriteEndArray();
        }

        void WriteNodes()
        {
            m_Jw.WritePropertyName("nodes");
            m_Jw.WriteStartArray();
            for (var level = 0; level <= m_Depth; level++)
            {
                var start = m_LevelStart[level];
                var end = m_LevelStart[level + 1];
                for (var i = start; i < end; i++)
                {
                    WriteNode(i, level);
                }
            }
            m_Jw.WriteEndArray();
        }

        void WriteNode(int i, int level)
        {
            var isLeaf = level == m_Depth;

            m_Jw.WriteStartObject();

            if (m_Config.namedNodes)
            {
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue(NextNodeName(i));
            }

            if (m_Config.transforms.HasFlag(TransformType.Translation))
            {
                m_Jw.WritePropertyName("translation");
                var t = m_Config.transforms.HasFlag(TransformType.Simple)
                    ? new float3(i, 0, 0)
                    : new float3(NextSignedFloat(75f), NextSignedFloat(75f), NextSignedFloat(75f));
                WriteFloatArray(t.x, t.y, t.z);
            }

            if (m_Config.transforms.HasFlag(TransformType.Rotation))
            {
                m_Jw.WritePropertyName("rotation");
                var q = m_Config.transforms.HasFlag(TransformType.Simple)
                    ? (0, 0, 0, 1)
                    : NextQuaternion();
                WriteFloatArray(q.x, q.y, q.z, q.w);
            }

            if (m_Config.transforms.HasFlag(TransformType.Scale))
            {
                m_Jw.WritePropertyName("scale");
                var s = m_Config.transforms.HasFlag(TransformType.Simple)
                    ? new float3(1, 2, 3)
                    : new float3(NextFloat(0.1f, 4f), NextFloat(0.1f, 4f), NextFloat(0.1f, 4f));
                WriteFloatArray(s.x, s.y, s.z);
            }

            if (m_Config.transforms.HasFlag(TransformType.Matrix))
            {
                m_Jw.WritePropertyName("matrix");
                m_Jw.WriteStartArray();
                if (m_Config.transforms.HasFlag(TransformType.Simple))
                {
                    var m = new[] {
                        1f, 0f, 0f, 0f,
                        0f, 1f, 0f, 0f,
                        0f, 0f, 1f, 0f,
                        0f, 0f, 0f, 1f
                    };
                    m[3] = i;
                    for (var j = 0; j < 16; j++)
                    {
                        m_Jw.WriteValue(m[j]);
                    }
                }
                else
                {
                    var m = float4x4.TRS(
                        new float3(NextFloat(-1000f, 1000f), NextFloat(-1000f, 1000f), NextFloat(-1000f, 1000f)),
                        quaternion.EulerXZY(NextFloat(0.1f, 4f), NextFloat(0.1f, 4f), NextFloat(0.1f, 4f)),
                        new float3(NextFloat(0.1f, 4f), NextFloat(0.1f, 4f), NextFloat(0.1f, 4f))
                        );

                    for (var p = 0; p < 4; p++)
                    {
                        for (var j = 0; j < 4; j++)
                        {
                            m_Jw.WriteValue(m[p][j]);
                        }
                    }
                }

                m_Jw.WriteEndArray();
            }

            if (isLeaf)
            {
                m_Jw.WritePropertyName("mesh");
                m_Jw.WriteValue(LeafMeshIndex(i));

                if (m_Config.Has(GltfFeatures.Animation) && m_SkinCount > 0)
                {
                    m_Jw.WritePropertyName("skin");
                    m_Jw.WriteValue(NextInt(m_SkinCount));
                }
            }
            else
            {
                var firstChild = m_LevelStart[level + 1] + (i - m_LevelStart[level]) * m_Branching;
                m_Jw.WritePropertyName("children");
                m_Jw.WriteStartArray();
                for (var c = 0; c < m_Branching; c++)
                    m_Jw.WriteValue(firstChild + c);
                m_Jw.WriteEndArray();
            }

            var pickCamera = m_Config.Has(GltfFeatures.Cameras) && m_CameraCount > 0 && NextInt(10) == 0;
            if (pickCamera)
            {
                m_Jw.WritePropertyName("camera");
                m_Jw.WriteValue(NextInt(m_CameraCount));
            }

            if (m_Config.Has(GltfFeatures.Extras))
            {
                m_Jw.WritePropertyName("extras");
                WriteRandomExtras(2, 20);
            }

            var pickLight = m_Config.Has(GltfFeatures.Lights) && m_LightCount > 0;
            var nodeXmp = m_Config.Has(GltfFeatures.Xmp);
            if (pickLight || nodeXmp)
            {
                m_Jw.WritePropertyName("extensions");
                m_Jw.WriteStartObject();
                if (pickLight)
                {
                    m_Jw.WritePropertyName("KHR_lights_punctual");
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("light");
                    m_Jw.WriteValue(NextInt(m_LightCount));
                    m_Jw.WriteEndObject();
                }
                if (nodeXmp)
                {
                    m_Jw.WritePropertyName("KHR_xmp_json_ld");
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("packet");
                    m_Jw.WriteValue(m_Config.uniqueXmpPerNode ? i : i % m_XmpPacketCount);
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndObject();
            }

            m_Jw.WriteEndObject();
        }

        int LeafMeshIndex(int nodeIndex)
        {
            if (m_MeshCount <= 1) return 0;
            var leafIdx = nodeIndex - m_LeafStart;
            return leafIdx % m_MeshCount;
        }

        void WriteMeshes()
        {
            m_Jw.WritePropertyName("meshes");
            m_Jw.WriteStartArray();
            for (var m = 0; m < m_MeshCount; m++)
                WriteMesh(m);
            m_Jw.WriteEndArray();
        }

        void WriteMesh(int meshIndex)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("name");
            m_Jw.WriteValue($"Mesh_{meshIndex:D3}");

            m_Jw.WritePropertyName("primitives");
            m_Jw.WriteStartArray();
            for (var p = 0; p < m_PrimitivesPerMesh; p++)
                WritePrimitive(meshIndex, p);
            m_Jw.WriteEndArray();

            if (m_Config.Has(GltfFeatures.Animation) && m_MorphTargetCount > 0)
            {
                m_Jw.WritePropertyName("weights");
                m_Jw.WriteStartArray();
                for (var t = 0; t < m_MorphTargetCount; t++)
                    m_Jw.WriteValue(NextFloat(0f, 1f));
                m_Jw.WriteEndArray();

                m_Jw.WritePropertyName("extras");
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("targetNames");
                m_Jw.WriteStartArray();
                for (var t = 0; t < m_MorphTargetCount; t++)
                    m_Jw.WriteValue($"morph_{meshIndex:D2}_{t}");
                m_Jw.WriteEndArray();
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndObject();
        }

        void WritePrimitive(int meshIndex, int primitiveIndex)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("attributes");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("POSITION");
            m_Jw.WriteValue(k_AccessorPosition);
            m_Jw.WritePropertyName("NORMAL");
            m_Jw.WriteValue(k_AccessorNormal);
            m_Jw.WritePropertyName("TEXCOORD_0");
            m_Jw.WriteValue(k_AccessorTexcoord);
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("indices");
            m_Jw.WriteValue(k_AccessorIndices);

            if (m_Config.Has(GltfFeatures.Materials))
            {
                m_Jw.WritePropertyName("material");
                m_Jw.WriteValue((meshIndex * m_PrimitivesPerMesh + primitiveIndex) % m_MaterialCount);
            }

            if (m_Config.Has(GltfFeatures.Animation) && m_MorphTargetCount > 0)
            {
                m_Jw.WritePropertyName("targets");
                m_Jw.WriteStartArray();
                for (var t = 0; t < m_MorphTargetCount; t++)
                {
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("POSITION");
                    m_Jw.WriteValue(m_MorphPositionAccessorStart + m_MorphTargetCount * primitiveIndex + t);
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndArray();
            }
            m_Jw.WriteEndObject();
        }

        void WriteMaterials()
        {
            m_Jw.WritePropertyName("materials");
            m_Jw.WriteStartArray();
            for (var i = 0; i < m_MaterialCount; i++)
                WriteMaterial(i);
            m_Jw.WriteEndArray();
        }

        void WriteMaterial(int index)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("name");
            m_Jw.WriteValue($"Material_{index:D2}_{NextWord()}");

            m_Jw.WritePropertyName("pbrMetallicRoughness");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("baseColorFactor");
            WriteFloatArray(NextUnit(), NextUnit(), NextUnit(), NextUnit());
            m_Jw.WritePropertyName("metallicFactor");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WritePropertyName("roughnessFactor");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WritePropertyName("baseColorTexture");
            WriteTextureInfo(0, index);
            m_Jw.WritePropertyName("metallicRoughnessTexture");
            WriteTextureInfo(0, index);
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("normalTexture");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("index");
            m_Jw.WriteValue(1 % Math.Max(1, m_MaterialCount));
            m_Jw.WritePropertyName("texCoord");
            m_Jw.WriteValue(0);
            m_Jw.WritePropertyName("scale");
            m_Jw.WriteValue(NextFloat(0.5f, 1.5f));
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("occlusionTexture");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("index");
            m_Jw.WriteValue(2 % Math.Max(1, m_MaterialCount));
            m_Jw.WritePropertyName("strength");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("emissiveTexture");
            WriteTextureInfo(3 % Math.Max(1, m_MaterialCount), 0);

            m_Jw.WritePropertyName("emissiveFactor");
            WriteFloatArray(NextUnit() * 0.5f, NextUnit() * 0.5f, NextUnit() * 0.5f);

            var alphaMode = (index % 3) switch
            {
                0 => "OPAQUE",
                1 => "MASK",
                _ => "BLEND"
            };
            m_Jw.WritePropertyName("alphaMode");
            m_Jw.WriteValue(alphaMode);
            if (alphaMode == "MASK")
            {
                m_Jw.WritePropertyName("alphaCutoff");
                m_Jw.WriteValue(NextUnit());
            }
            m_Jw.WritePropertyName("doubleSided");
            m_Jw.WriteValue((index & 1) == 0);

            m_Jw.WritePropertyName("extensions");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("KHR_materials_ior");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("ior");
            m_Jw.WriteValue(NextFloat(1.0f, 2.5f));
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("KHR_materials_specular");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("specularFactor");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WritePropertyName("specularColorFactor");
            WriteFloatArray(NextUnit(), NextUnit(), NextUnit());
            m_Jw.WriteEndObject();

            m_Jw.WritePropertyName("KHR_materials_clearcoat");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("clearcoatFactor");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WritePropertyName("clearcoatRoughnessFactor");
            m_Jw.WriteValue(NextUnit());
            m_Jw.WriteEndObject();
            m_Jw.WriteEndObject();

            m_Jw.WriteEndObject();
        }

        void WriteTextureInfo(int textureIndex, int texCoord)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("index");
            m_Jw.WriteValue(textureIndex % Math.Max(1, m_MaterialCount));
            m_Jw.WritePropertyName("texCoord");
            m_Jw.WriteValue(texCoord & 1);
            m_Jw.WriteEndObject();
        }

        void WriteTextures()
        {
            m_Jw.WritePropertyName("textures");
            m_Jw.WriteStartArray();
            // One texture per material so primitive material refs stay valid.
            for (var i = 0; i < m_MaterialCount; i++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Texture_{i:D2}");
                m_Jw.WritePropertyName("sampler");
                m_Jw.WriteValue(i % 4);
                m_Jw.WritePropertyName("source");
                m_Jw.WriteValue(i);
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteImages(int imageDataUriSize)
        {
            m_Jw.WritePropertyName("images");
            m_Jw.WriteStartArray();
            for (var i = 0; i < m_MaterialCount; i++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Image_{i:D2}");
                m_Jw.WritePropertyName("mimeType");
                m_Jw.WriteValue("image/png");
                m_Jw.WritePropertyName("uri");
                if (imageDataUriSize > 0)
                {
                    m_Jw.WriteValue("data:image/png;base64," + RandomBase64(imageDataUriSize));
                }
                else
                {
                    m_Jw.WriteValue($"textures/image_{i:D2}_{NextWord()}.png");
                }
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteSamplers()
        {
            m_Jw.WritePropertyName("samplers");
            m_Jw.WriteStartArray();
            int[] minF = { 9728, 9729, 9984, 9987 };
            int[] magF = { 9728, 9729, 9729, 9729 };
            int[] wrap = { 10497, 33071, 33648, 10497 };
            for (var i = 0; i < 4; i++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("magFilter");
                m_Jw.WriteValue(magF[i]);
                m_Jw.WritePropertyName("minFilter");
                m_Jw.WriteValue(minF[i]);
                m_Jw.WritePropertyName("wrapS");
                m_Jw.WriteValue(wrap[i]);
                m_Jw.WritePropertyName("wrapT");
                m_Jw.WriteValue(wrap[(i + 1) % 4]);
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteCameras()
        {
            m_Jw.WritePropertyName("cameras");
            m_Jw.WriteStartArray();
            for (var i = 0; i < m_CameraCount; i++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Camera_{i:D2}");
                if ((i & 1) == 0)
                {
                    m_Jw.WritePropertyName("type");
                    m_Jw.WriteValue("perspective");
                    m_Jw.WritePropertyName("perspective");
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("aspectRatio");
                    m_Jw.WriteValue(NextFloat(1.0f, 2.4f));
                    m_Jw.WritePropertyName("yfov");
                    m_Jw.WriteValue(NextFloat(0.4f, 1.4f));
                    m_Jw.WritePropertyName("znear");
                    m_Jw.WriteValue(NextFloat(0.01f, 0.5f));
                    m_Jw.WritePropertyName("zfar");
                    m_Jw.WriteValue(NextFloat(100f, 5000f));
                    m_Jw.WriteEndObject();
                }
                else
                {
                    m_Jw.WritePropertyName("type");
                    m_Jw.WriteValue("orthographic");
                    m_Jw.WritePropertyName("orthographic");
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("xmag");
                    m_Jw.WriteValue(NextFloat(1f, 20f));
                    m_Jw.WritePropertyName("ymag");
                    m_Jw.WriteValue(NextFloat(1f, 20f));
                    m_Jw.WritePropertyName("znear");
                    m_Jw.WriteValue(NextFloat(0.01f, 0.5f));
                    m_Jw.WritePropertyName("zfar");
                    m_Jw.WriteValue(NextFloat(100f, 5000f));
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteSkins()
        {
            m_Jw.WritePropertyName("skins");
            m_Jw.WriteStartArray();
            for (var i = 0; i < m_SkinCount; i++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Skin_{i:D2}");
                m_Jw.WritePropertyName("inverseBindMatrices");
                m_Jw.WriteValue(m_IbmAccessorStart + i);
                m_Jw.WritePropertyName("skeleton");
                m_Jw.WriteValue(NextInt(m_TotalNodes));
                m_Jw.WritePropertyName("joints");
                m_Jw.WriteStartArray();
                for (var j = 0; j < m_JointsPerSkin; j++)
                    m_Jw.WriteValue(NextInt(m_TotalNodes));
                m_Jw.WriteEndArray();
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteAnimations()
        {
            m_Jw.WritePropertyName("animations");
            m_Jw.WriteStartArray();
            string[] paths = { "translation", "rotation", "scale", "weights" };
            string[] interpolations = { "LINEAR", "STEP", "CUBICSPLINE" };
            var outputCursor = m_AnimationOutputAccessorStart;
            for (var a = 0; a < m_AnimationCount; a++)
            {
                m_Jw.WriteStartObject();
                m_Jw.WritePropertyName("name");
                m_Jw.WriteValue($"Clip_{a:D2}_{NextWord()}");

                m_Jw.WritePropertyName("samplers");
                m_Jw.WriteStartArray();
                for (var c = 0; c < m_ChannelsPerAnimation; c++)
                {
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("input");
                    m_Jw.WriteValue(m_AnimationTimeAccessor);
                    m_Jw.WritePropertyName("interpolation");
                    m_Jw.WriteValue(interpolations[c % interpolations.Length]);
                    m_Jw.WritePropertyName("output");
                    m_Jw.WriteValue(outputCursor++);
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndArray();

                m_Jw.WritePropertyName("channels");
                m_Jw.WriteStartArray();
                for (var c = 0; c < m_ChannelsPerAnimation; c++)
                {
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("sampler");
                    m_Jw.WriteValue(c);
                    m_Jw.WritePropertyName("target");
                    m_Jw.WriteStartObject();
                    m_Jw.WritePropertyName("node");
                    m_Jw.WriteValue(NextInt(m_TotalNodes));
                    m_Jw.WritePropertyName("path");
                    m_Jw.WriteValue(paths[c % paths.Length]);
                    m_Jw.WriteEndObject();
                    m_Jw.WriteEndObject();
                }
                m_Jw.WriteEndArray();
                m_Jw.WriteEndObject();
            }
            m_Jw.WriteEndArray();
        }

        void WriteAccessors()
        {
            m_Jw.WritePropertyName("accessors");
            m_Jw.WriteStartArray();
            // 0: POSITION
            WriteAccessor(0, "VEC3", 5126, 24, true);
            // 1: NORMAL
            WriteAccessor(1, "VEC3", 5126, 24, false);
            // 2: TEXCOORD_0
            WriteAccessor(2, "VEC2", 5126, 24, false);
            // 3: indices
            WriteAccessor(3, "SCALAR", 5123, 36, false);

            if (m_Config.Has(GltfFeatures.Animation))
            {
                // time
                WriteAccessor(4, "SCALAR", 5126, 10, false);
                var bv = 5;
                string[] outTypes = { "VEC3", "VEC4", "VEC3", "SCALAR" };
                for (var a = 0; a < m_AnimationCount; a++)
                {
                    for (var c = 0; c < m_ChannelsPerAnimation; c++)
                    {
                        var t = outTypes[c % outTypes.Length];
                        WriteAccessor(bv++, t, 5126, 10, false);
                    }
                }
                // Morph target sparse accessors (POSITION delta).
                for (var t = 0; t < m_MorphTargetCount; t++)
                {
                    WriteSparseAccessor("VEC3", 5126, 24, bv, bv + 1);
                    bv += 2;
                }
                // Inverse bind matrices.
                for (var s = 0; s < m_SkinCount; s++)
                    WriteAccessor(bv++, "MAT4", 5126, m_JointsPerSkin, false);
            }
            m_Jw.WriteEndArray();
        }

        void WriteAccessor(int bufferView, string type, int componentType, int count, bool includeBounds)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("bufferView");
            m_Jw.WriteValue(bufferView);
            m_Jw.WritePropertyName("componentType");
            m_Jw.WriteValue(componentType);
            m_Jw.WritePropertyName("count");
            m_Jw.WriteValue(count);
            m_Jw.WritePropertyName("type");
            m_Jw.WriteValue(type);
            if (includeBounds && type == "VEC3")
            {
                m_Jw.WritePropertyName("min");
                WriteFloatArray(-1f, -1f, -1f);
                m_Jw.WritePropertyName("max");
                WriteFloatArray(1f, 1f, 1f);
            }
            m_Jw.WriteEndObject();
        }

        void WriteSparseAccessor(string type, int componentType, int count, int indicesBufferView, int valuesBufferView)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("componentType");
            m_Jw.WriteValue(componentType);
            m_Jw.WritePropertyName("count");
            m_Jw.WriteValue(count);
            m_Jw.WritePropertyName("type");
            m_Jw.WriteValue(type);
            m_Jw.WritePropertyName("sparse");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("count");
            m_Jw.WriteValue(Math.Max(1, count / 4));
            m_Jw.WritePropertyName("indices");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("bufferView");
            m_Jw.WriteValue(indicesBufferView);
            m_Jw.WritePropertyName("componentType");
            m_Jw.WriteValue(5123);
            m_Jw.WriteEndObject();
            m_Jw.WritePropertyName("values");
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("bufferView");
            m_Jw.WriteValue(valuesBufferView);
            m_Jw.WriteEndObject();
            m_Jw.WriteEndObject();
            m_Jw.WriteEndObject();
        }

        void WriteBufferViews()
        {
            m_Jw.WritePropertyName("bufferViews");
            m_Jw.WriteStartArray();
            var offset = 0;
            offset = WriteBufferView(0, offset, 24 * 12, 34962);  // positions
            offset = WriteBufferView(0, offset, 24 * 12, 34962);  // normals
            offset = WriteBufferView(0, offset, 24 * 8, 34962);   // texcoords
            offset = WriteBufferView(0, offset, 36 * 2, 34963);   // indices

            if (m_Config.Has(GltfFeatures.Animation))
            {
                offset = WriteBufferView(0, offset, 10 * 4, 0);   // time
                int[] outStride = { 12, 16, 12, 4 };
                for (var a = 0; a < m_AnimationCount; a++)
                {
                    for (var c = 0; c < m_ChannelsPerAnimation; c++)
                        offset = WriteBufferView(0, offset, 10 * outStride[c % outStride.Length], 0);
                }
                for (var t = 0; t < m_MorphTargetCount; t++)
                {
                    offset = WriteBufferView(0, offset, 6 * 2, 0);    // sparse indices
                    offset = WriteBufferView(0, offset, 6 * 12, 0);   // sparse values
                }
                for (var s = 0; s < m_SkinCount; s++)
                    offset = WriteBufferView(0, offset, m_JointsPerSkin * 64, 0);
            }
            m_Jw.WriteEndArray();
        }

        int WriteBufferView(int buffer, int offset, int length, int target)
        {
            m_Jw.WriteStartObject();
            m_Jw.WritePropertyName("buffer");
            m_Jw.WriteValue(buffer);
            m_Jw.WritePropertyName("byteOffset");
            m_Jw.WriteValue(offset);
            m_Jw.WritePropertyName("byteLength");
            m_Jw.WriteValue(length);
            if (target != 0)
            {
                m_Jw.WritePropertyName("target");
                m_Jw.WriteValue(target);
            }
            m_Jw.WriteEndObject();
            return offset + length;
        }

        void WriteBuffers(int dataUriLength)
        {
            m_Jw.WritePropertyName("buffers");
            m_Jw.WriteStartArray();
            m_Jw.WriteStartObject();
            // Large enough byteLength to cover all referenced bufferViews; the
            // binary payload itself is never loaded by these JSON-parse tests.
            m_Jw.WritePropertyName("byteLength");
            m_Jw.WriteValue(1 << 22);
            m_Jw.WritePropertyName("uri");
            if (dataUriLength > 0)
            {
                m_Jw.WriteValue("data:application/octet-stream;base64," + RandomBase64(dataUriLength));
            }
            else
            {
                m_Jw.WriteValue("buffer.bin");
            }
            m_Jw.WriteEndObject();
            m_Jw.WriteEndArray();
        }

        void WriteRandomExtras(int min, int max)
        {
            var count = NextInt(min, max + 1);
            m_Jw.WriteStartObject();
            for (var i = 0; i < count; i++)
            {
                m_Jw.WritePropertyName($"prop_{i}");
                WriteRandomExtraValue();
            }
            m_Jw.WriteEndObject();
        }

        void WriteRandomExtraValue()
        {
            var kind = NextInt(8);
            switch (kind)
            {
                case 0:
                    m_Jw.WriteValue(NextWord());
                    break;
                case 1:
                    m_Jw.WriteValue(NextSignedFloat(1000f));
                    break;
                case 2:
                    m_Jw.WriteValue(NextInt(-1_000_000, 1_000_000));
                    break;
                case 3:
                    m_Jw.WriteValue(NextInt(2) == 0);
                    break;
                case 4:
                {
                    var n = NextInt(2, 6);
                    m_Jw.WriteStartArray();
                    for (var i = 0; i < n; i++) m_Jw.WriteValue(NextWord());
                    m_Jw.WriteEndArray();
                    break;
                }
                case 5:
                {
                    var n = NextInt(2, 6);
                    m_Jw.WriteStartArray();
                    for (var i = 0; i < n; i++) m_Jw.WriteValue(NextSignedFloat(10f));
                    m_Jw.WriteEndArray();
                    break;
                }
                case 6:
                {
                    var n = NextInt(2, 6);
                    m_Jw.WriteStartArray();
                    for (var i = 0; i < n; i++) m_Jw.WriteValue(NextInt(0, 10_000));
                    m_Jw.WriteEndArray();
                    break;
                }
                default:
                {
                    var n = NextInt(2, 6);
                    m_Jw.WriteStartArray();
                    for (var i = 0; i < n; i++) m_Jw.WriteValue(NextInt(2) == 0);
                    m_Jw.WriteEndArray();
                    break;
                }
            }
        }

        void WriteFloatArray(float a, float b, float c)
        {
            m_Jw.WriteStartArray();
            m_Jw.WriteValue(a);
            m_Jw.WriteValue(b);
            m_Jw.WriteValue(c);
            m_Jw.WriteEndArray();
        }

        void WriteFloatArray(float a, float b, float c, float d)
        {
            m_Jw.WriteStartArray();
            m_Jw.WriteValue(a);
            m_Jw.WriteValue(b);
            m_Jw.WriteValue(c);
            m_Jw.WriteValue(d);
            m_Jw.WriteEndArray();
        }

        // Random helpers.

        float NextUnit() => (float)m_Rng.NextDouble();

        float NextFloat(float min, float max) => min + (max - min) * (float)m_Rng.NextDouble();

        float NextSignedFloat(float magnitude) => ((float)m_Rng.NextDouble() * 2f - 1f) * magnitude;

        int NextInt(int upperExclusive) => m_Rng.NextInt(upperExclusive);

        int NextInt(int min, int upperExclusive) => m_Rng.NextInt(min, upperExclusive);

        (float x, float y, float z, float w) NextQuaternion()
        {
            // Marsaglia's method for uniform random quaternions. Computed in
            // double via System.Math (deterministic per IEEE 754) and cast to
            // float, so byte output stays stable across runtimes that disagree
            // on UnityEngine.Mathf or implicit float-trig precision.
            var u1 = m_Rng.NextDouble();
            var u2 = m_Rng.NextDouble();
            var u3 = m_Rng.NextDouble();
            var s1 = Math.Sqrt(1.0 - u1);
            var s2 = Math.Sqrt(u1);
            var t1 = 2.0 * Math.PI * u2;
            var t2 = 2.0 * Math.PI * u3;
            return (
                (float)(s1 * Math.Sin(t1)),
                (float)(s1 * Math.Cos(t1)),
                (float)(s2 * Math.Sin(t2)),
                (float)(s2 * Math.Cos(t2)));
        }

        static readonly char[] k_SNameAlphabet =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-".ToCharArray();

        string NextNodeName(int index)
        {
            var length = NextInt(10, 31);
            var sb = new StringBuilder(length);
            sb.Append('N');
            sb.Append(index.ToString("D4"));
            sb.Append('_');
            while (sb.Length < length)
                sb.Append(k_SNameAlphabet[NextInt(k_SNameAlphabet.Length)]);
            return sb.ToString();
        }

        static readonly string[] k_SWords =
        {
            "alpha", "bravo", "charlie", "delta", "echo", "foxtrot",
            "golf", "hotel", "india", "juliet", "kilo", "lima",
            "mike", "november", "oscar", "papa", "quebec", "romeo",
            "sierra", "tango", "uniform", "victor", "whiskey", "xray",
            "yankee", "zulu"
        };

        string NextWord() => k_SWords[NextInt(k_SWords.Length)];

        string RandomBase64(int byteCount)
        {
            var bytes = new byte[byteCount];
            // System.Random.NextBytes is deterministic for the seeded RNG.
            m_Rng.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    /// <summary>
    /// Minimal compact JSON writer for the generator. Tracks comma state per
    /// depth so nested objects/arrays inside arrays work; emits floats in
    /// round-trippable invariant culture form. No whitespace, no third-party
    /// dependencies.
    /// </summary>
    sealed class CompactJsonWriter
    {
        readonly StreamWriter m_Stream;
        readonly bool[] m_NeedsComma = new bool[32];
        int m_Depth;
        bool m_PendingValue;

        public CompactJsonWriter(StreamWriter stream)
        {
            m_Stream = stream;
        }

        public void WriteStartObject()
        {
            ValueSeparator();
            m_Stream.Write('{');
            EnterScope();
        }

        public void WriteEndObject()
        {
            m_Stream.Write('}');
            ExitScope();
        }

        public void WriteStartArray()
        {
            ValueSeparator();
            m_Stream.Write('[');
            EnterScope();
        }

        public void WriteEndArray()
        {
            m_Stream.Write(']');
            ExitScope();
        }

        public void WritePropertyName(string name)
        {
            ValueSeparator();
            m_Stream.Write('"');
            WriteEscaped(name);
            m_Stream.Write("\":");
            m_PendingValue = true;
        }

        public void WriteValue(int value)
        {
            ValueSeparator();
            m_Stream.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteValue(float value)
        {
            ValueSeparator();
            // "G9" guarantees round-trippable single-precision output with a
            // fixed shortest representation across runtimes. "R" is not
            // byte-stable across .NET Framework / Mono / modern .NET.
            m_Stream.Write(value.ToString("G9", CultureInfo.InvariantCulture));
        }

        public void WriteValue(bool value)
        {
            ValueSeparator();
            m_Stream.Write(value ? "true" : "false");
        }

        public void WriteValue(string value)
        {
            ValueSeparator();
            if (value == null)
            {
                m_Stream.Write("null");
                return;
            }
            m_Stream.Write('"');
            WriteEscaped(value);
            m_Stream.Write('"');
        }

        public void Flush() => m_Stream.Flush();

        void ValueSeparator()
        {
            if (m_PendingValue)
            {
                m_PendingValue = false;
                return;
            }
            if (m_NeedsComma[m_Depth])
                m_Stream.Write(',');
            m_NeedsComma[m_Depth] = true;
        }

        void EnterScope()
        {
            m_Depth++;
            m_NeedsComma[m_Depth] = false;
            m_PendingValue = false;
        }

        void ExitScope()
        {
            m_Depth--;
        }

        void WriteEscaped(string value)
        {
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': m_Stream.Write(@"\\"); break;
                    case '"': m_Stream.Write("\\\""); break;
                    case '\b': m_Stream.Write("\\b"); break;
                    case '\f': m_Stream.Write("\\f"); break;
                    case '\n': m_Stream.Write("\\n"); break;
                    case '\r': m_Stream.Write("\\r"); break;
                    case '\t': m_Stream.Write("\\t"); break;
                    default:
                        if (c < 0x20)
                            m_Stream.Write($"\\u{(int)c:x4}");
                        else
                            m_Stream.Write(c);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Self-contained xorshift32 PRNG. The algorithm is fully specified by the
    /// three shift constants below, so the sequence is bit-identical across
    /// platforms, runtimes, and .NET versions — unlike System.Random which has
    /// changed implementation between .NET Framework, Mono, and .NET 6+.
    /// </summary>
    sealed class Xorshift32
    {
        uint m_State;

        public Xorshift32(uint seed)
        {
            // xorshift32 collapses to 0 forever once the state hits 0; bias it
            // away from that absorbing fixed point.
            m_State = seed == 0u ? 0x9E3779B9u : seed;
        }

        public uint NextUInt()
        {
            var x = m_State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            m_State = x;
            return x;
        }

        public int NextInt(int upperExclusive)
        {
            if (upperExclusive <= 0) return 0;
            return (int)(NextUInt() % (uint)upperExclusive);
        }

        public int NextInt(int min, int upperExclusive)
        {
            var range = upperExclusive - min;
            if (range <= 0) return min;
            return min + (int)(NextUInt() % (uint)range);
        }

        public double NextDouble()
        {
            // Drop the sign bit, divide by 2^31 — yields a value in [0, 1).
            return (NextUInt() & 0x7FFFFFFFu) / (double)0x80000000u;
        }

        public void NextBytes(byte[] buffer)
        {
            var i = 0;
            while (i + 4 <= buffer.Length)
            {
                var x = NextUInt();
                buffer[i++] = (byte)(x & 0xFF);
                buffer[i++] = (byte)((x >> 8) & 0xFF);
                buffer[i++] = (byte)((x >> 16) & 0xFF);
                buffer[i++] = (byte)((x >> 24) & 0xFF);
            }
            if (i < buffer.Length)
            {
                var x = NextUInt();
                while (i < buffer.Length)
                {
                    buffer[i++] = (byte)(x & 0xFF);
                    x >>= 8;
                }
            }
        }
    }
}
