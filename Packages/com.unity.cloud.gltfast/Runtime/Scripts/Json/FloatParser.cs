// SPDX-FileCopyrightText: 2026 Unity Technologies and the glTFast authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;

namespace GLTFast.Schema
{
    static class FloatParser
    {
        public static double GetDouble(ReadOnlySpan<byte> json)
        {
            if (json.Length == 0)
                throw new InvalidDataException("Empty input");

            var pos = 0;

            var negative = false;
            if (json[pos] == '-')
            {
                negative = true;
                pos++;
            }

            var value = .0;
            var hasDigit = false;
            while(pos < json.Length)
            {
                if (json[pos] >= '0' && json[pos] <= '9')
                {
                    hasDigit = true;
                    value = value * 10L + (json[pos] - 48);
                    pos++;
                }
                else if (json[pos] == '.')
                {
                    pos++;
                    goto Radix;
                }
                else if ((json[pos] & 0b11011111) == 'E')
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
            while(pos < json.Length)
            {
                if (json[pos] >= '0' && json[pos] <= '9')
                {
                    hasRadixDigit = true;
                    factor *= .1;
                    value += (json[pos] - 48) * factor;
                    pos++;
                }
                else if ((json[pos] & 0b11011111) == 'E')
                {
                    if (!hasRadixDigit)
                        throw new InvalidDataException($"Expected digit after '.' at {pos}");
                    pos++;
                    goto Exponent;
                }
                else if (json[pos] == '.')
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
            long exponent = 0;
            var negateExponent = false;
            if (pos >= json.Length)
                throw new InvalidDataException("Unexpected end of input in exponent");
            if (json[pos] == '+')
            {
                pos++;
            }
            else if (json[pos] == '-')
            {
                pos++;
                negateExponent = true;
            }

            if (pos >= json.Length)
                throw new InvalidDataException("Missing exponent digits");

            while(pos < json.Length)
            {
                if (json[pos] >= '0' && json[pos] <= '9')
                {
                    exponent = exponent * 10L + (json[pos] - 48);
                    pos++;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected char at {pos}");
                }
            }

            return (negative ? -value : value) * Math.Pow(10, negateExponent ? -exponent : exponent);
        }
    }
}
