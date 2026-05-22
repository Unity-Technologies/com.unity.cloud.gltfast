// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Unity.Mathematics;

namespace GLTFast.Schema
{
    static class FloatParser
    {
        static readonly double[] k_PosPowersOf10 = {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10,
            1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22
        };

        public static double GetDouble(ReadOnlySpan<byte> json)
        {
            if (json.Length == 0)
                throw new InvalidDataException("Empty input");

            var pos = 0;

            var negative = false;
            var currentByte = json[pos];
            if (currentByte == '-')
            {
                negative = true;
                pos++;
            }

            var value = .0;
            var hasDigit = false;
            while (pos < json.Length)
            {
                currentByte = json[pos];
                if (currentByte >= '0' && currentByte <= '9')
                {
                    hasDigit = true;
                    value = value * 10L + (currentByte - 48);
                    pos++;
                }
                else if (currentByte == '.')
                {
                    pos++;
                    goto Radix;
                }
                else if ((currentByte & 0b11011111) == 'E')
                {
                    if (!hasDigit)
                        throw new InvalidDataException($"Expected digit before exponent at {pos}");
                    pos++;
                    goto Exponent;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected char at {pos}");
                }
            }

            if (!hasDigit)
                throw new InvalidDataException("Missing integer digits");

            return negative ? -value : value;

        Radix:
            double factor = 1;
            var hasRadixDigit = false;
            while (pos < json.Length)
            {
                currentByte = json[pos];
                if (currentByte >= '0' && currentByte <= '9')
                {
                    hasRadixDigit = true;
                    factor *= .1;
                    value += (currentByte - 48) * factor;
                    pos++;
                }
                else if ((currentByte & 0b11011111) == 'E')
                {
                    if (!hasRadixDigit)
                        throw new InvalidDataException($"Expected digit after '.' at {pos}");
                    pos++;
                    goto Exponent;
                }
                else if (currentByte == '.')
                {
                    throw new InvalidDataException($"Multiple radix points in number at {pos}");
                }
                else
                {
                    throw new InvalidDataException($"Unexpected char at {pos}");
                }
            }

            if (!hasRadixDigit)
                throw new InvalidDataException($"Expected digit after '.' at {pos}");

            return negative ? -value : value;

        Exponent:
            short exponent = 0;
            var negateExponent = false;
            if (pos >= json.Length)
                throw new InvalidDataException("Unexpected end of input in exponent");
            currentByte = json[pos];
            if (currentByte == '+')
            {
                pos++;
            }
            else if (currentByte == '-')
            {
                pos++;
                negateExponent = true;
            }

            if (pos >= json.Length)
                throw new InvalidDataException("Missing exponent digits");

            while (pos < json.Length)
            {
                currentByte = json[pos];
                if (currentByte >= '0' && currentByte <= '9')
                {
                    exponent = (short)(exponent * 10 + (currentByte - 48));
                    pos++;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected char at {pos}");
                }
            }

            double scale;
            if (exponent >= 0 && exponent < k_PosPowersOf10.Length)
            {
                scale = k_PosPowersOf10[exponent];
                if (negateExponent)
                {
                    scale = 1.0 / scale;
                }
            }
            else
            {
                scale = Math.Pow(10, negateExponent ? -exponent : exponent);
            }
            return (negative ? -value : value) * scale;
        }
    }
}
