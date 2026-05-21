// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
#if UNITY_6000_5_OR_NEWER
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;
#endif

namespace GLTFast.Schema
{

    /// <summary>
    /// Material extensions.
    /// </summary>
    [System.Serializable]
    public class MaterialExtensions : IGltfObject
    {
        // Names are identical to glTF specified property names, that's why
        // inconsistent names are ignored.
        // ReSharper disable InconsistentNaming

        /// <inheritdoc cref="PbrSpecularGlossiness"/>
        public PbrSpecularGlossiness KHR_materials_pbrSpecularGlossiness;

        /// <inheritdoc cref="MaterialUnlit"/>
        public MaterialUnlit KHR_materials_unlit;

        /// <inheritdoc cref="Transmission"/>
        public Transmission KHR_materials_transmission;

        /// <inheritdoc cref="ClearCoat"/>
        public ClearCoat KHR_materials_clearcoat;

        /// <inheritdoc cref="Sheen"/>
        public Sheen KHR_materials_sheen;

        /// <inheritdoc cref="MaterialSpecular"/>
        public MaterialSpecular KHR_materials_specular;

        /// <inheritdoc cref="MaterialIor"/>
        public MaterialIor KHR_materials_ior;

        /// <inheritdoc cref="Root.extras"/>
        public UnclassifiedData extras;

        /// <summary>JSON properties without a matching member.</summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        // ReSharper restore InconsistentNaming

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (KHR_materials_pbrSpecularGlossiness != null)
            {
                writer.AddProperty("KHR_materials_pbrSpecularGlossiness");
                KHR_materials_pbrSpecularGlossiness.GltfSerialize(writer);
            }
            if (KHR_materials_unlit != null)
            {
                writer.AddProperty("KHR_materials_unlit");
                KHR_materials_unlit.GltfSerialize(writer);
            }
            if (KHR_materials_transmission != null)
            {
                writer.AddProperty("KHR_materials_transmission");
                KHR_materials_transmission.GltfSerialize(writer);
            }
            if (KHR_materials_clearcoat != null)
            {
                writer.AddProperty("KHR_materials_clearcoat");
                KHR_materials_clearcoat.GltfSerialize(writer);
            }
            if (KHR_materials_sheen != null)
            {
                writer.AddProperty("KHR_materials_sheen");
                KHR_materials_sheen.GltfSerialize(writer);
            }
            if (KHR_materials_specular != null)
            {
                writer.AddProperty("KHR_materials_specular");
                KHR_materials_specular.GltfSerialize(writer);
            }
            if (KHR_materials_ior != null)
            {
                writer.AddProperty("KHR_materials_ior");
                KHR_materials_ior.GltfSerialize(writer);
            }
            writer.Close();
        }
    }
}
