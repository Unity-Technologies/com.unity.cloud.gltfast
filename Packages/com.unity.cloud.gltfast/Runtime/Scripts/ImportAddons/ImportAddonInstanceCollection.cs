// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace GLTFast.Addons
{
    sealed class ImportAddonInstanceCollection : IDisposable
    {
        readonly List<ImportAddonInstance> m_Addons = new();

        public void Add<T>(T importInstance) where T : ImportAddonInstance
        {
            m_Addons.Add(importInstance);
        }

        public T Get<T>() where T : ImportAddonInstance
        {
            foreach (var addon in m_Addons)
            {
                if (addon is T typedAddon)
                {
                    return typedAddon;
                }
            }

            return null;
        }

        public void ForEach(Action<ImportAddonInstance> action)
        {
            foreach (var instance in m_Addons)
            {
                action(instance);
            }
        }

        public bool AnySupportsGltfExtension(string extensionName)
        {
            foreach (var instance in m_Addons)
            {
                if (instance.SupportsGltfExtension(extensionName))
                {
                    return true;
                }
            }

            return false;
        }

        public void Dispose()
        {
            foreach (var importInstance in m_Addons)
            {
                importInstance.Dispose();
            }
            m_Addons.Clear();
        }
    }
}
