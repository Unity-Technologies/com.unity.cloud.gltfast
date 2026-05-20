// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Gltfast.Text.Json;
using Unity.Gltfast.Text.Json.Serialization;

namespace GLTFast.Schema
{

    /// <summary>
    /// glTF root extensions
    /// </summary>
    [System.Serializable]
    public class RootExtensions : IGltfObject
    {

        /// <inheritdoc cref="LightsPunctual"/>
        // ReSharper disable once InconsistentNaming
        public LightsPunctual KHR_lights_punctual;

        /// <inheritdoc cref="MaterialsVariantsRootExtension"/>
        // ReSharper disable once InconsistentNaming
        public MaterialsVariantsRootExtension KHR_materials_variants;

        /// <summary>
        /// JSON properties without a matching member.
        /// </summary>
        [JsonExtensionData, JsonInclude] internal Dictionary<string, JsonElement> ExtensionsData { get; set; }

        /// <inheritdoc/>
        public bool TryGetValue<T>(string key, out T value)
        {
            return ExtensionsData.TryGetValue(key, out value);
        }

        internal void GltfSerialize(JsonWriter writer)
        {
            writer.AddObject();
            if (KHR_lights_punctual != null)
            {
                writer.AddProperty("KHR_lights_punctual");
                KHR_lights_punctual.GltfSerialize(writer);
            }
            if (KHR_materials_variants != null)
            {
                writer.AddProperty("KHR_materials_variants");
                KHR_materials_variants.GltfSerialize(writer);
            }
            writer.Close();
        }

        /// <summary>
        /// Has been used to clean up invalid parsing artifacts created by JsonUtility.
        /// </summary>
        /// <returns>True if the element itself still holds value. False if it can be safely removed.</returns>
        [Obsolete("Has become obsolete after the transition from JsonUtility to System.Text.Json.")]
        public virtual bool JsonUtilityCleanup()
        {
            return true;
        }
    }
}
