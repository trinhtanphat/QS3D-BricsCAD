using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorDefinitionSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL));
            var floor = new FloorDefinition("F-ZERO", "Zero", negativeZero);
            CanonicalPositiveZero(floor.ElevationM, "constructor elevation");

            floor.ElevationM = negativeZero;
            CanonicalPositiveZero(floor.ElevationM, "setter elevation");

            floor.ElevationM = -3.25d;
            Equal(-3.25d, floor.ElevationM, "negative elevation");
            floor.ElevationM = 12.5d;
            Equal(12.5d, floor.ElevationM, "positive elevation");

            Throws<ArgumentOutOfRangeException>(() => floor.ElevationM = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => floor.ElevationM = double.PositiveInfinity);
            Throws<ArgumentOutOfRangeException>(() => new FloorDefinition("F-INF", "Invalid", double.NegativeInfinity));
        }

        private static void CanonicalPositiveZero(double value, string label)
        {
            if (value != 0d)
                throw new InvalidOperationException(label + ": expected zero but got " + value + ".");
            if (BitConverter.DoubleToInt64Bits(value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException(label + ": expected canonical positive zero.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
