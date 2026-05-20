// SPDX-FileCopyrightText: 2023 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

#if (UNITY_ANIMATION || GLTFAST_ANIMATION) && NEWTONSOFT_JSON

using System;

namespace GLTFast.Newtonsoft.Schema
{
    [Obsolete("Use GLTFast.Schema.AnimationSampler instead.")]
    public class AnimationSampler : GLTFast.Schema.AnimationSampler, IJsonObject { }
}

#endif
