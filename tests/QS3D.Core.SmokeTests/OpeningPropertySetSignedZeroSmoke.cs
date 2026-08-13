using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningPropertySetSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var opening = new OpeningPropertySet();

            opening.SillOffsetMm = -0d;
            CanonicalPositiveZero(opening.SillOffsetMm);

            opening.SillOffsetMm = -125d;
            Equal(-125d, opening.SillOffsetMm);
            opening.SillOffsetMm = 250d;
            Equal(250d, opening.SillOffsetMm);

            opening.WidthMm = 1000d;
            opening.HeightMm = 2100d;
            opening.ThicknessMm = 120d;
            Equal(1000d, opening.WidthMm);
            Equal(2100d, opening.HeightMm);
            Equal(120d, opening.ThicknessMm);

            Throws<ArgumentOutOfRangeException>(() => opening.SillOffsetMm = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => opening.SillOffsetMm = double.PositiveInfinity);
            Throws<ArgumentOutOfRangeException>(() => opening.SillOffsetMm = double.NegativeInfinity);

            Throws<ArgumentOutOfRangeException>(() => opening.WidthMm = -0d);
            Throws<ArgumentOutOfRangeException>(() => opening.HeightMm = 0d);
            Throws<ArgumentOutOfRangeException>(() => opening.ThicknessMm = -1d);
        }

        private static void CanonicalPositiveZero(double value)
        {
            if (value != 0d)
                throw new InvalidOperationException("Expected zero but got " + value + ".");
            if (BitConverter.DoubleToInt64Bits(value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException("Expected canonical positive zero.");
        }

        private static void Equal(double expected, double actual)
        {
            if (expected != actual)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
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
