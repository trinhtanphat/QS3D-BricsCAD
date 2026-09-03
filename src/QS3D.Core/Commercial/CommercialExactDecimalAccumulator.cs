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

            Decode(value, out var coefficient, out var scale);
            if (coefficient.Sign < 0)
                throw new InvalidOperationException(label + " cannot aggregate a negative value.");

            if (!_hasValue)
            {
                _coefficient = coefficient;
                _scale = scale;
                _hasValue = true;
                return;
            }

            AlignScales(ref _coefficient, ref _scale, ref coefficient, ref scale);
            _coefficient += coefficient;
        }

        internal decimal ToDecimal(string label)
        {
            if (!_hasValue)
                return 0m;
            return MaterializeAggregate(
                _coefficient,
                _scale,
                label + " exact aggregate cannot be represented as decimal without precision loss.");
        }

        internal static decimal AddExact(decimal left, decimal right, string label)
        {
            Decode(left, out var leftCoefficient, out var leftScale);
            Decode(right, out var rightCoefficient, out var rightScale);
            AlignScales(ref leftCoefficient, ref leftScale, ref rightCoefficient, ref rightScale);
            return MaterializeArithmetic(
                leftCoefficient + rightCoefficient,
                leftScale,
                label + " overflowed decimal arithmetic.",
                "Commercial addition precision loss: " + label + ".");
        }

        internal static decimal SubtractExact(decimal left, decimal right, string label)
        {
            Decode(left, out var leftCoefficient, out var leftScale);
            Decode(right, out var rightCoefficient, out var rightScale);
            AlignScales(ref leftCoefficient, ref leftScale, ref rightCoefficient, ref rightScale);
            return MaterializeArithmetic(
                leftCoefficient - rightCoefficient,
                leftScale,
                label + " overflowed decimal arithmetic.",
                "Commercial subtraction precision loss: " + label + ".");
        }

        private static void Decode(decimal value, out BigInteger coefficient, out int scale)
        {
            var bits = decimal.GetBits(value);
            scale = (bits[3] >> 16) & 0x7F;
            coefficient =
                ((BigInteger)(uint)bits[2] << 64) |
                ((BigInteger)(uint)bits[1] << 32) |
                (uint)bits[0];
            if ((bits[3] & unchecked((int)0x80000000)) != 0)
                coefficient = BigInteger.Negate(coefficient);
        }

        private static void AlignScales(
            ref BigInteger leftCoefficient,
            ref int leftScale,
            ref BigInteger rightCoefficient,
            ref int rightScale)
        {
            if (leftScale < rightScale)
            {
                leftCoefficient *= BigInteger.Pow(10, rightScale - leftScale);
                leftScale = rightScale;
            }
            else if (rightScale < leftScale)
            {
                rightCoefficient *= BigInteger.Pow(10, leftScale - rightScale);
                rightScale = leftScale;
            }
        }

        private static decimal MaterializeArithmetic(
            BigInteger signedCoefficient,
            int scale,
            string overflowMessage,
            string precisionLossMessage)
        {
            if (signedCoefficient.IsZero)
                return 0m;

            if (scale > 28)
                throw new OverflowException(precisionLossMessage);

            var coefficient = BigInteger.Abs(signedCoefficient);
            while (coefficient > MaximumDecimalCoefficient && scale > 0 && coefficient % 10 == 0)
            {
                signedCoefficient /= 10;
                coefficient /= 10;
                scale--;
            }

            if (coefficient > MaximumDecimalCoefficient)
            {
                var maximumAtScale = MaximumDecimalCoefficient * BigInteger.Pow(10, scale);
                if (coefficient > maximumAtScale)
                    throw new OverflowException(overflowMessage);
                throw new OverflowException(precisionLossMessage);
            }

            return CreateDecimal(signedCoefficient, scale);
        }

        private static decimal MaterializeAggregate(BigInteger signedCoefficient, int scale, string precisionLossMessage)
        {
            if (signedCoefficient.IsZero)
                return 0m;

            while (scale > 0 && signedCoefficient % 10 == 0)
            {
                signedCoefficient /= 10;
                scale--;
            }

            var coefficient = BigInteger.Abs(signedCoefficient);
            if (scale > 28 || coefficient > MaximumDecimalCoefficient)
                throw new OverflowException(precisionLossMessage);

            return CreateDecimal(signedCoefficient, scale);
        }

        private static decimal CreateDecimal(BigInteger signedCoefficient, int scale)
        {
            var isNegative = signedCoefficient.Sign < 0;
            var coefficient = BigInteger.Abs(signedCoefficient);
            var low = unchecked((int)(uint)(coefficient & uint.MaxValue));
            var middle = unchecked((int)(uint)((coefficient >> 32) & uint.MaxValue));
            var high = unchecked((int)(uint)((coefficient >> 64) & uint.MaxValue));
            return new decimal(low, middle, high, isNegative, (byte)scale);
        }
    }
}
