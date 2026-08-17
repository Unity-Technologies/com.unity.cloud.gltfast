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
//   - methods carrying [Obsolete("(UnityUpgradable) -> ...")] shims instead of [MovedFrom]:
//     the Async suffix, and the instantiator's AddMesh/AddMeshInstanced
// Both `using`-reachable unqualified names and fully-qualified references are used on purpose.

using UnityEngine;
using GLTFast;
using GLTFast.Schema;
using GLTFast.Logging;
using GLTFast.Export;
using GLTFast.Loading;

namespace ApiUpdaterMigration
{
    class UsesGltfastLegacy : MonoBehaviour
    {
        // unqualified names reachable through the `using` directives above
        GltfImport m_Import;
        ConsoleLogger m_Logger;
        GameObjectExport m_Export;
        GltfWriter m_Writer;
        GltfAsset m_Asset;
        GameObjectInstantiator m_Instantiator;
        DefaultDownloadProvider m_DownloadProvider;
        CustomHeaderDownloadProvider m_HeaderDownloadProvider;
        TimeBudgetPerFrameDeferAgent m_DeferAgent;
        UninterruptedDeferAgent m_UninterruptedDeferAgent;

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

            _ = m_Import.Load("probe.gltf");
            _ = m_Import.Load(new System.Uri("probe.gltf", System.UriKind.Relative));
            _ = m_Import.Load(new byte[1]);
            _ = m_Import.LoadFile("probe.gltf");
            _ = m_Import.LoadStream(System.IO.Stream.Null);
            _ = m_Import.LoadGltfJson("{}");
            _ = m_Export.SaveToFileAndDispose("out.gltf");
            _ = m_Export.SaveToFileAndDispose("out.gltf", true);
            _ = m_Export.SaveToStreamAndDispose(System.IO.Stream.Null);
            _ = m_Export.SaveToStreamAndDispose(System.IO.Stream.Null, true);
            _ = m_Writer.SaveToFileAndDispose("out.gltf");
            _ = m_Writer.SaveToStreamAndDispose(System.IO.Stream.Null);
            _ = m_Asset.Instantiate();
            _ = m_Asset.InstantiateScene(0);
            _ = m_Asset.Load("probe.gltf");
            _ = m_DownloadProvider.Request(new System.Uri("probe.gltf", System.UriKind.Relative));
            _ = m_DownloadProvider.RequestTexture(new System.Uri("probe.gltf", System.UriKind.Relative), false);
            _ = m_HeaderDownloadProvider.Request(new System.Uri("probe.gltf", System.UriKind.Relative));
            _ = m_HeaderDownloadProvider.RequestTexture(new System.Uri("probe.gltf", System.UriKind.Relative), false);
            _ = m_DeferAgent.BreakPoint();
            _ = m_DeferAgent.BreakPoint(0.1f);
            _ = m_UninterruptedDeferAgent.BreakPoint();
            _ = m_UninterruptedDeferAgent.BreakPoint(0.1f);
            m_Instantiator.AddPrimitive(0, "mesh", default);
            m_Instantiator.AddPrimitiveInstanced(0, "mesh", default, 1, null, null, null);

            // touch the fields so nothing is flagged as unused
            System.GC.KeepAlive(m_Import);
            System.GC.KeepAlive(m_Logger);
            System.GC.KeepAlive(m_Export);
            System.GC.KeepAlive(m_Root);
            System.GC.KeepAlive(m_Instantiator);
            System.GC.KeepAlive(m_Settings);
            _ = baseColor;
        }
    }

    // A 6.x subclass reaching the protected InstantiateScene overload — the only shim whose call site
    // cannot be reached from outside the type.
    class LegacySubclass : GltfAssetBase
    {
        public override void ClearScenes() { }

        protected override IInstantiator GetDefaultInstantiator(ICodeLogger logger)
            => new GameObjectInstantiator(Importer, transform, logger);

        void Reference()
        {
            _ = InstantiateScene(0, new GameObjectInstantiator(Importer, transform));
        }
    }
}
