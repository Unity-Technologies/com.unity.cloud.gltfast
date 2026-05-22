// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using System.Text;
using GLTFast.Schema;
using NUnit.Framework;

namespace GLTFast.Tests.JsonParsing
{
    [Category("JsonParsing")]
    class FloatParserTests
    {
        const double k_Delta = 1e-10;

        static ReadOnlySpan<byte> Utf8(string s) => Encoding.UTF8.GetBytes(s);

        // --- Integers ---

        [TestCase("0", 0.0)]
        [TestCase("1", 1.0)]
        [TestCase("9", 9.0)]
        [TestCase("42", 42.0)]
        [TestCase("100", 100.0)]
        [TestCase("12345", 12345.0)]
        public static void Integer(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)));
        }

        [TestCase("-0", 0.0)]
        [TestCase("-1", -1.0)]
        [TestCase("-9", -9.0)]
        [TestCase("-42", -42.0)]
        [TestCase("-12345", -12345.0)]
        public static void NegativeInteger(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)));
        }

        // --- Decimal fractions ---

        [TestCase("0.0", 0.0)]
        [TestCase("0.5", 0.5)]
        [TestCase("1.5", 1.5)]
        [TestCase("3.14159", 3.14159)]
        [TestCase("1.23456789", 1.23456789)]
        public static void Decimal(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        [TestCase("-0.0", 0.0)]
        [TestCase("-0.5", -0.5)]
        [TestCase("-1.5", -1.5)]
        [TestCase("-3.14159", -3.14159)]
        public static void NegativeDecimal(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Scientific notation: positive exponent, integer base ---

        [TestCase("1E0", 1.0)]
        [TestCase("1E1", 10.0)]
        [TestCase("1E3", 1000.0)]
        [TestCase("5E2", 500.0)]
        [TestCase("1E10", 1e10)]
        public static void IntegerBasePositiveExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Scientific notation: positive exponent, decimal base ---

        [TestCase("1.2E3", 1200.0)]
        [TestCase("1.5E2", 150.0)]
        [TestCase("2.5E4", 25000.0)]
        [TestCase("1.23E6", 1230000.0)]
        public static void DecimalBasePositiveExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Scientific notation: lowercase 'e' ---

        [TestCase("1e3", 1000.0)]
        [TestCase("1.2e3", 1200.0)]
        [TestCase("5e2", 500.0)]
        public static void LowercaseExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Scientific notation: explicit '+' exponent sign ---

        [TestCase("1E+0", 1.0)]
        [TestCase("1E+3", 1000.0)]
        [TestCase("1.2E+3", 1200.0)]
        [TestCase("5E+2", 500.0)]
        public static void ExplicitPositiveExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Scientific notation: negative exponent ---

        [TestCase("1E-0", 1.0)]
        [TestCase("1E-1", 0.1)]
        [TestCase("1E-3", 0.001)]
        [TestCase("1.2E-3", 0.0012)]
        [TestCase("5E-2", 0.05)]
        [TestCase("1e-3", 0.001)]
        [TestCase("1.5E-2", 0.015)]
        public static void NegativeExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Negative value with positive exponent (integer base) ---

        [TestCase("-1E0", -1.0)]
        [TestCase("-1E3", -1000.0)]
        [TestCase("-5E2", -500.0)]
        [TestCase("-2E10", -2e10)]
        public static void NegativeIntegerBasePositiveExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Negative value with positive exponent (decimal base) ---
        // The Radix→Exponent path correctly applies the sign, so these should pass.

        [TestCase("-1.2E3", -1200.0)]
        [TestCase("-1.5E2", -150.0)]
        [TestCase("-2.5E4", -25000.0)]
        public static void NegativeDecimalBasePositiveExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Negative value with negative exponent ---

        [TestCase("-1E-3", -0.001)]
        [TestCase("-1.2E-3", -0.0012)]
        [TestCase("-5E-2", -0.05)]
        [TestCase("-1.5E-2", -0.015)]
        public static void NegativeValueNegativeExponent(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), k_Delta);
        }

        // --- Large / boundary values ---

        [TestCase("1E15", 1e15)]
        [TestCase("9.99E14", 9.99e14)]
        [TestCase("1E-15", 1e-15)]
        public static void LargeAndSmall(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), expected * 1e-9);
        }

        // --- Exponent lookup table corner cases ---

        [TestCase("42E22", 42e22)]
        [TestCase("42E23", 42e23)]
        [TestCase("42E-23", 42e-23)]
        [TestCase("42E-22", 42e-22)]
        public static void ExponentLookupTableCornerCases(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), expected * 1e-9);
        }

        // --- Exponent corner cases ---

        [TestCase("1E308", 1e308)]
        [TestCase("2E308", double.PositiveInfinity)] // → overflows
        [TestCase("1E-323", 1e-323)]
        [TestCase("2E-323", 2e-323)]
        [TestCase("1E-324", 1e-324)] // → silently becomes 0.0 (underflows)
        public static void ExponentCornerCases(string input, double expected)
        {
            Assert.AreEqual(expected, FloatParser.GetDouble(Utf8(input)), expected * 1e-9);
        }

        // --- Error cases ---

        [Test]
        public static void MultipleDecimalPoints()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1.2.3")));
        }

        [Test]
        public static void InvalidCharacterInInteger()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1a2")));
        }

        [Test]
        public static void InvalidCharacterInExponent()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1E3a")));
        }

        [Test]
        public static void EmptyInput()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(ReadOnlySpan<byte>.Empty));
        }

        [Test]
        public static void JustSign()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("-")));
        }

        [Test]
        public static void LeadingDecimalPoint()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8(".")));
        }

        [Test]
        public static void NegativeLeadingDecimalPoint()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("-.")));
        }

        [Test]
        public static void TrailingDecimalPoint()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1.")));
        }

        [Test]
        public static void NoDigitsAfterDecimalBeforeExponent()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1.E3")));
        }

        [Test]
        public static void InvalidCharacterInDecimalPart()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1.2a")));
        }

        [Test]
        public static void MissingExponent()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1E")));
        }

        [Test]
        public static void MissingExponentAfterPlusSign()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1E+")));
        }

        [Test]
        public static void MissingExponentAfterMinusSign()
        {
            Assert.Throws<InvalidDataException>(() =>
                FloatParser.GetDouble(Utf8("1E-")));
        }
    }
}
