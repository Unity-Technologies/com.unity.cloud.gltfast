// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using UnityEditor;
using UnityEngine.UIElements;

namespace GLTFast.Editor
{

    static class EditorImportSettingsEditor
    {
        internal static void CreateUI(SerializedObject serializedObject, VisualElement container, string pathPrefix = "")
        {
            var properties = new[] {
                serializedObject.FindProperty($"{pathPrefix}textureCompression"),
                serializedObject.FindProperty($"{pathPrefix}textureCompressionQuality"),
            };
            var foldout = new Foldout { text = "Texture Compression", value = true };
            Utils.CreateProperties(foldout, properties);
            container.Add(foldout);
        }
    }
}
