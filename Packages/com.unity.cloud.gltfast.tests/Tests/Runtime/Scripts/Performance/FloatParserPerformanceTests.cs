// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using NUnit.Framework;
using Unity.Cloud.Gltfast.Objects;
using Unity.Mathematics;
using Unity.PerformanceTesting;

namespace Unity.Cloud.Gltfast.Tests.Performance
{
    [TestFixture]
    [Category("Performance")]
    class FloatParserPerformanceTests
    {
        // ---------------------------------------------------------------------------
        // Input data — built once, shared across all tests.
        // Each category contains ~30 entries: enough variety to avoid branch
        // prediction artifacts while keeping per-iteration allocations at zero.
        // ---------------------------------------------------------------------------

        /// <summary>Whole-number values (no decimal point, no exponent).</summary>
        static readonly byte[][] k_Integers = Encode(
            "0", "1", "-1", "9", "-9",
            "42", "-42", "255", "-255", "1000",
            "-1000", "12345", "-12345", "99999", "-99999",
            "0", "1", "-1", "9", "-9",
            "42", "-42", "255", "-255", "1000",
            "-1000", "12345", "-12345", "99999", "-99999"
        );

        /// <summary>Decimal values without exponent — the most common form in 3-D data
        /// (positions, normals, quaternions, colours).</summary>
        static readonly byte[][] k_Decimals = Encode(
            "0.0", "1.0", "-1.0", "0.5", "-0.5",
            "3.14159", "-3.14159", "0.70710678", "-0.70710678", "1.41421356",
            "0.33333333", "-0.33333333", "99.99", "-99.99", "0.00001",
            "0.0", "1.0", "-1.0", "0.5", "-0.5",
            "3.14159", "-3.14159", "0.70710678", "-0.70710678", "1.41421356",
            "0.33333333", "-0.33333333", "99.99", "-99.99", "0.00001"
        );

        /// <summary>Values using exponent notation, covering positive and negative
        /// exponents and both integer and decimal mantissas.</summary>
        static readonly byte[][] k_Scientific = Encode(
            "1E3", "-1E3", "1.2E3", "-1.2E3", "1E-3",
            "-1E-3", "1.2E-3", "-1.2E-3", "5E2", "-5E2",
            "1e10", "-1e10", "1.5E-2", "-1.5E-2", "9.9E9",
            "1E3", "-1E3", "1.2E3", "-1.2E3", "1E-3",
            "-1E-3", "1.2E-3", "-1.2E-3", "5E2", "-5E2",
            "1e10", "-1e10", "1.5E-2", "-1.5E-2", "9.9E9"
        );

        /// <summary>Realistic mix that approximates what glTFast encounters when
        /// parsing node transforms, accessor min/max, and material parameters.</summary>
        static readonly byte[][] k_Mixed = Encode(
            "0", "1.0", "-0.70710678", "3.14159", "1E3",
            "-1E-3", "42", "0.5", "1.2E-3", "-99.99",
            "-42", "0.0", "1.41421356", "5E2", "-1.5E-2",
            "255", "-0.33333333", "9.9E9", "12345", "0.00001",
            "0", "1.0", "-0.70710678", "3.14159", "1E3",
            "-1E-3", "42", "0.5", "1.2E-3", "-99.99"
        );

        // ---------------------------------------------------------------------------
        // Integers
        // ---------------------------------------------------------------------------

        [Test, Performance]
        public void FloatParser_Integers()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = k_Integers;
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Nanosecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        // ---------------------------------------------------------------------------
        // Decimals
        // ---------------------------------------------------------------------------

        [Test, Performance]
        public void FloatParser_Decimals()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = k_Decimals;
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Nanosecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void FloatParser_DecimalsGenerated()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = GenerateTestData(1_000);
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Microsecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        // ---------------------------------------------------------------------------
        // Scientific notation
        // ---------------------------------------------------------------------------

        [Test, Performance]
        public void FloatParser_Scientific()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = k_Scientific;
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Nanosecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void FloatParser_ScientificGenerated()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = GenerateScientificTestData(1_000);
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Microsecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        [Test, Performance]
        public void FloatParser_ScientificGeneratedCommon()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = GenerateScientificTestData(1_000, -21, 21);
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Microsecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }


        [Test, Performance]
        public void FloatParser_ScientificGeneratedUnCommon()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = GenerateScientificTestData(1_000, 22);
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Microsecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        // ---------------------------------------------------------------------------
        // Realistic mixed workload
        // ---------------------------------------------------------------------------

        [Test, Performance]
        public void FloatParser_Mixed()
        {
#if !RUN_PERFORMANCE_TESTS
            Assert.Ignore("Skipping performance tests (scripting define RUN_PERFORMANCE_TESTS is not set).");
#endif
            var inputs = k_Mixed;
            Measure.Method(() =>
                {
                    foreach (var input in inputs)
                        FloatParser.GetDouble(input);
                })
                .SampleGroup(new SampleGroup("Time", SampleUnit.Nanosecond))
                .WarmupCount(1)
                .DynamicMeasurementCount()
                .Run();
        }

        // ---------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------

        static byte[][] Encode(params string[] values)
        {
            var result = new byte[values.Length][];
            for (var i = 0; i < values.Length; i++)
                result[i] = Encoding.UTF8.GetBytes(values[i]);
            return result;
        }

        static byte[][] GenerateTestData(int count)
        {
            var rng = new Random(42);
            var result = new byte[count][];
            for (var i = 0; i < count; i++)
            {
                var v = rng.NextDouble();
                result[i] = Encoding.UTF8.GetBytes(v.ToString());
            }
            return result;
        }

        static byte[][] GenerateScientificTestData(int count, int minExp = -100, int maxExp = 100)
        {
            var rng = new Random(42);
            var result = new byte[count][];
            for (var i = 0; i < count; i++)
            {
                var v = rng.NextInt();
                var exp = rng.NextInt(minExp, maxExp);
                var str = $"{v}e{exp}";
                result[i] = Encoding.UTF8.GetBytes(str);
            }
            return result;
        }
    }
}
