// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace GLTFast.Tests
{
    abstract class DoubleComparer
    {
        const double k_DefaultError = 1e-15;
        protected readonly double m_AllowedError;

        public DoubleComparer() : this(k_DefaultError) { }

        public DoubleComparer(double allowedError)
        {
            m_AllowedError = allowedError;
        }

        protected static bool AreDoublesEqual(double expected, double actual, double epsilon)
        {
            // special case for infinity
            if (
                double.IsInfinity(expected)
                || double.IsInfinity(actual)
                || double.IsNegativeInfinity(expected)
                || double.IsNegativeInfinity(actual)
            )
                return expected == actual;

            // we cover both relative and absolute tolerance with this check
            // which is better than just relative in case of small (in abs value) args
            // please note that "usually" approximation is used [i.e. abs(x)+abs(y)+1]
            // but we speak about test code so we dont care that much about performance
            // but we do care about checks being more precise
            return math.abs(actual - expected) <= epsilon * math.max(math.max(math.abs(actual), math.abs(expected)), 1.0f);
        }
    }

    class Double3EqualityComparer : DoubleComparer, IEqualityComparer<double3>
    {
        public Double3EqualityComparer() : base() { }
        public Double3EqualityComparer(double allowedError) : base(allowedError) { }

        public bool Equals(double3 x, double3 y)
        {
            return AreDoublesEqual(x.x, y.x, m_AllowedError) &&
                AreDoublesEqual(x.y, y.y, m_AllowedError) &&
                AreDoublesEqual(x.z, y.z, m_AllowedError);
        }

        public int GetHashCode(double3 obj)
        {
            return 0;
        }
    }

    class Double4EqualityComparer : DoubleComparer, IEqualityComparer<double4>
    {
        public Double4EqualityComparer() : base() { }
        public Double4EqualityComparer(double allowedError) : base(allowedError) { }

        public bool Equals(double4 x, double4 y)
        {
            return AreDoublesEqual(x.x, y.x, m_AllowedError) &&
                AreDoublesEqual(x.y, y.y, m_AllowedError) &&
                AreDoublesEqual(x.z, y.z, m_AllowedError) &&
                AreDoublesEqual(x.w, y.w, m_AllowedError);
        }

        public int GetHashCode(double4 obj)
        {
            return 0;
        }
    }
}
