// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

// This file is written against the glTFast 6.x public API (root namespace `GLTFast`).
// It intentionally does NOT compile against glTFast 7.0, whose namespaces are `Unity.Cloud.Gltfast.*`.
// Opening this project with the 7.0 package and `-accept-apiupdate` must make Unity's API Updater
// rewrite every reference below to the new namespaces (via the [MovedFrom] attributes on the 7.0 types).
//
// It exercises one reference into each renamed dimension:
//   - core assembly (glTFast -> Unity.Cloud.Gltfast): GltfImport, RenderPipeline
//   - core sub-namespaces:  GLTFast.Schema, GLTFast.Logging, GLTFast.Materials
//   - Export assembly (glTFast.Export -> Unity.Cloud.Gltfast.Export): GameObjectExport, ExportSettings
// Both `using`-reachable unqualified names and fully-qualified references are used on purpose.

using UnityEngine;
using GLTFast;
using GLTFast.Schema;
using GLTFast.Logging;
using GLTFast.Export;

namespace ApiUpdaterMigration
{
    class UsesGltfastLegacy : MonoBehaviour
    {
        // unqualified names reachable through the `using` directives above
        GltfImport m_Import;
        ConsoleLogger m_Logger;
        GameObjectExport m_Export;

        // fully-qualified references
        GLTFast.Schema.Root m_Root;
        GLTFast.Export.ExportSettings m_Settings;
        GLTFast.RenderPipeline m_Pipeline;

        void Reference()
        {
            // static member access on a fully-qualified type
            var baseColor = GLTFast.Materials.MaterialProperty.BaseColor;

            // enum member on an unqualified type
            m_Pipeline = RenderPipeline.Unknown;

            // touch the fields so nothing is flagged as unused
            System.GC.KeepAlive(m_Import);
            System.GC.KeepAlive(m_Logger);
            System.GC.KeepAlive(m_Export);
            System.GC.KeepAlive(m_Root);
            System.GC.KeepAlive(m_Settings);
            _ = baseColor;
        }
    }
}
