// SPDX-FileCopyrightText: 2025 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if USING_URP || USING_HDRP || GLTFAST_BUILTIN_SHADER_GRAPH
#define GLTFAST_SHADER_GRAPH
#endif

using System.Linq;
using GLTFast.Logging;
using GLTFast.Materials;
using NUnit.Framework;
using UnityEngine;
using GltfMaterial = GLTFast.Schema.Material;
using Material = UnityEngine.Material;

namespace GLTFast.Tests
{
    [Category("Import")]
    class MaterialGeneratorTests
    {
        [Test]
        public void FindShader()
        {
            var materialGenerator = new CustomMaterialGenerator();
            var logger = new CollectingLogger();
            materialGenerator.SetLogger(logger);
            var shader = materialGenerator.TestFindShader("glTF/PbrMetallicRoughness");
            Assert.IsNotNull(shader);
            Assert.AreEqual("glTF/PbrMetallicRoughness", shader.name);

            var nullShader = materialGenerator.TestFindShader("DoesNotExistShader");
            Assert.IsNull(nullShader);
            Assert.AreEqual(1, logger.Count);
            var item = logger.Items.First();
            Assert.AreEqual(LogType.Error, item.Type);
            Assert.AreEqual(LogCode.ShaderMissing, item.Code);
        }

        [Test]
        public void LoadShaderByName()
        {
#if GLTFAST_SHADER_GRAPH
            var materialGenerator = new CustomShaderGraphMaterialGenerator();
            var logger = new CollectingLogger();
            materialGenerator.SetLogger(logger);
            var shader = materialGenerator.TestLoadShaderByName("glTF-pbrMetallicRoughness");
            Assert.IsNotNull(shader);
            Assert.AreEqual("Shader Graphs/glTF-pbrMetallicRoughness", shader.name);

            var nullShader = materialGenerator.TestLoadShaderByName("DoesNotExistShader");
            Assert.IsNull(nullShader);
            Assert.AreEqual(1, logger.Count);
            var item = logger.Items.First();
            Assert.AreEqual(LogType.Error, item.Type);
            Assert.AreEqual(LogCode.ShaderMissing, item.Code);
#else
            Assert.Ignore("Shader Graph not supported in test environment");
#endif
        }
    }

    class CustomMaterialGenerator : MaterialGenerator
    {
        public Shader TestFindShader(string shaderName)
        {
            return FindShader(shaderName);
        }

        protected override Material GenerateDefaultMaterial(bool pointsSupport = false)
        {
            throw new System.NotImplementedException();
        }
        public override Material GenerateMaterial(GltfMaterial gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
        {
            throw new System.NotImplementedException();
        }
    }

#if GLTFAST_SHADER_GRAPH
    class CustomShaderGraphMaterialGenerator : ShaderGraphMaterialGenerator
    {
        public Shader TestLoadShaderByName(string shaderName)
        {
            return LoadShaderByName(shaderName);
        }
    }
#endif
}
