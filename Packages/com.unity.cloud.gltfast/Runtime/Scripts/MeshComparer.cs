// SPDX-FileCopyrightText: 2024 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GLTFast.Schema;

namespace GLTFast
{
    class MeshComparer
        : IEqualityComparer<MeshPrimitive>
        , IEqualityComparer<IReadOnlyList<MeshPrimitive>>
    {
        public bool Equals(IReadOnlyList<MeshPrimitive> x, IReadOnlyList<MeshPrimitive> y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.Count != y.Count) return false;
            for (var index = 0; index < x.Count; index++)
            {
                if (!Equals(x[index], y[index]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(IReadOnlyList<MeshPrimitive> obj)
        {
            var hashCode = new HashCode();
            foreach (var primitive in obj)
            {
                hashCode.Add(GetHashCode(primitive));
            }
            return hashCode.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MeshPrimitive x, MeshPrimitive y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Indices == y.Indices
                && Equals(x.Attributes, y.Attributes)
                && Equals(x.Targets, y.Targets);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(MeshPrimitive primitive)
        {
            return HashCode.Combine(
                primitive.Indices,
                GetHashCode(primitive.Attributes),
                GetHashCode(primitive.Targets)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetHashCode(Attributes x)
        {
            if (x == null) return 0;
            HashCode hash = new();
            hash.Add(x.Position);
            hash.Add(x.Normal);
            hash.Add(x.Tangent);
            hash.Add(x.TexCoord0);
            hash.Add(x.TexCoord1);
            hash.Add(x.TexCoord2);
            hash.Add(x.TexCoord3);
            hash.Add(x.TexCoord4);
            hash.Add(x.TexCoord5);
            hash.Add(x.TexCoord6);
            hash.Add(x.TexCoord7);
            hash.Add(x.Color0);
            hash.Add(x.Joints0);
            hash.Add(x.Weights0);
            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetHashCode(MorphTarget[] x)
        {
            if (x == null) return 0;
            HashCode hash = new();
            hash.Add(x.Length);
            foreach (var target in x)
            {
                if (target == null)
                {
                    hash.Add(0);
                    continue;
                }
                hash.Add(target.Position);
                hash.Add(target.Normal);
                hash.Add(target.Tangent);
            }
            return hash.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Equals(MorphTarget[] x, MorphTarget[] y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            if (x.Length != y.Length) return false;
            for (var i = 0; i < x.Length; i++)
            {
                if (!Equals(x[i], y[i]))
                    return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Equals(MorphTarget x, MorphTarget y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Position == y.Position
                && x.Normal == y.Normal
                && x.Tangent == y.Tangent;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool Equals(Attributes x, Attributes y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Position == y.Position
                && x.Normal == y.Normal
                && x.Tangent == y.Tangent
                && x.TexCoord0 == y.TexCoord0
                && x.TexCoord1 == y.TexCoord1
                && x.TexCoord2 == y.TexCoord2
                && x.TexCoord3 == y.TexCoord3
                && x.TexCoord4 == y.TexCoord4
                && x.TexCoord5 == y.TexCoord5
                && x.TexCoord6 == y.TexCoord6
                && x.TexCoord7 == y.TexCoord7
                && x.Color0 == y.Color0
                && x.Joints0 == y.Joints0
                && x.Weights0 == y.Weights0;
        }
    }
}
