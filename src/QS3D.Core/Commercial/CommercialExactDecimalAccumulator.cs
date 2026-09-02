using System;
using System.Numerics;

namespace QS3D.Core.Commercial
{
    internal sealed class CommercialExactDecimalAccumulator
    {
        private static readonly BigInteger MaximumDecimalCoefficient = (BigInteger.One << 96) - BigInteger.One;
        private BigInteger _coefficient;
        private int _scale;
        private bool _hasValue;

        internal void Add(decimal value, string label)
        {
            if (value < 0m)
                throw new InvalidOperationException(label + " cannot aggregate a negative value.");

            var bits = decimal.GetBits(value);
            var scale = (bits[3] >> 16) & 0x7F;
            var coefficient =
                ((BigInteger)(uint)bits[2] << 64) |
                ((BigInteger)(uint)bits[1] << 32) |
                (uint)bits[0];

            if (!_hasValue)
            {
                _coefficient = coefficient;
                _scale = scale;
                _hasValue = true;
                return;
            }

            if (scale > _scale)
            {
                _coefficient *= BigInteger.Pow(10, scale - _scale);
                _scale = scale;
            }
            else if (scale < _scale)
            {
                coefficient *= BigInteger.Pow(10, _scale - scale);
            }

            _coefficient += coefficient;
        }

        internal decimal ToDecimal(string label)
        {
            if (!_hasValue || _coefficient.IsZero)
                return 0m;

            var coefficient = _coefficient;
            var scale = _scale;
            while (scale > 0 && coefficient % 10 == 0)
            {
                coefficient /= 10;
                scale--;
            }

            if (scale > 28 || coefficient > MaximumDecimalCoefficient)
                throw new OverflowException(label + " exact aggregate cannot be represented as decimal.");

            var low = unchecked((int)(uint)(coefficient & uint.MaxValue));
            var middle = unchecked((int)(uint)((coefficient >> 32) & uint.MaxValue));
            var high = unchecked((int)(uint)((coefficient >> 64) & uint.MaxValue));
            return new decimal(low, middle, high, false, (byte)scale);
        }
    }
}
