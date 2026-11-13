// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

namespace GLTFast.Editor
{

    /// <summary>
    /// Keeps the <see cref="EditorTextureCompression.CustomDependencyName"/> custom dependency
    /// in sync with the active build target's texture compression format. This is what makes
    /// glTF assets reimport automatically when that changes, while never reimporting on an
    /// unrelated build.
    /// </summary>
    /// <remarks>
    /// Two things can change the effective format without changing <see cref="BuildTarget"/>
    /// (which <see cref="IActiveBuildTargetChanged"/> alone would miss): switching to a
    /// different Build Profile that targets the same platform (e.g. two Android profiles with
    /// different Texture Compression Format overrides), and editing that setting on the profile
    /// or Player Settings currently active. Editing the setting directly isn't observable via a
    /// callback, so <see cref="EditorTextureCompression.CustomDependencyName"/> is also
    /// refreshed on load (<see cref="Register"/> in the static constructor) to catch drift from
    /// a previous session; <see cref="BuildProfile.activeProfileChanged"/> covers switching
    /// between profiles within a session, where it's available (see below).
    /// </remarks>
    [InitializeOnLoad]
    class TextureCompressionDependency : IActiveBuildTargetChanged
    {
        static TextureCompressionDependency()
        {
            Register();
            TrySubscribeToActiveProfileChanged();
        }

        /// <inheritdoc />
        public int callbackOrder => 0;

        /// <inheritdoc />
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            Register();
            AssetDatabase.Refresh();
        }

        static void Register()
        {
            AssetDatabase.RegisterCustomDependency(
                EditorTextureCompression.CustomDependencyName,
                EditorTextureCompression.GetTargetHash()
                );
        }

        static void OnActiveProfileChanged(BuildProfile previous, BuildProfile next)
        {
            Register();
            AssetDatabase.Refresh();
        }

        static void TrySubscribeToActiveProfileChanged()
        {
            // BuildProfile.activeProfileChanged was added in a later Unity 6.x Editor release
            // than this package's minimum supported version (confirmed absent in 6000.3.9f1,
            // present in 6000.5.4f1), so it's looked up via reflection rather than a direct `+=`,
            // which would fail to compile against older Unity 6 releases that lack it. Falling
            // back to just IActiveBuildTargetChanged there still covers platform switches, just
            // not switches between two profiles that target the same platform.
            var eventInfo = typeof(BuildProfile).GetEvent(
                "activeProfileChanged", BindingFlags.Public | BindingFlags.Static);
            if (eventInfo == null)
            {
                return;
            }
            var handler = (Action<BuildProfile, BuildProfile>)OnActiveProfileChanged;
            eventInfo.AddEventHandler(null, handler);
        }
    }
}
