// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace GLTFast.Addons
{
    sealed class ImportAddonInstanceCollection : IDisposable
    {
        public delegate bool TryCreateResult<in TAddon, in TInput, TResult>
            (TAddon addon, TInput input, out TResult result);

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

        public T First<T>(Func<T, bool> predicate)
        {
            foreach (var instance in m_Addons)
            {
                if (instance is T target && predicate(target))
                {
                    return target;
                }
            }

            return default;
        }

        public void ForEach(Action<ImportAddonInstance> action)
        {
            foreach (var instance in m_Addons)
            {
                action(instance);
            }
        }

        public bool TryGet<TAddon, TInput, TResult>(
            TInput input,
            TryCreateResult<TAddon, TInput, TResult> action,
            out TResult result
        )
        {
            foreach (var instance in m_Addons)
            {
                if (instance is TAddon typedInstance && action(typedInstance, input, out result))
                {
                    return true;
                }
            }

            result = default;
            return false;
        }

        public void ForEachTryGet<TAddon, TInput, TResult>(
            IReadOnlyList<TInput> list,
            TryCreateResult<TAddon, TInput, TResult> predicate,
            Action<TAddon, int, TResult> resultAction
        )
        {
            List<TAddon> addons = null;
            foreach (var instance in m_Addons)
            {
                if (instance is TAddon addon)
                {
                    addons ??= new List<TAddon>();
                    addons.Add(addon);
                }
            }

            if (addons != null)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var element = list[i];
                    foreach (var addon in addons)
                    {
                        if (predicate(addon, element, out var result))
                        {
                            resultAction(addon, i, result);
                            break;
                        }
                    }
                }
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
