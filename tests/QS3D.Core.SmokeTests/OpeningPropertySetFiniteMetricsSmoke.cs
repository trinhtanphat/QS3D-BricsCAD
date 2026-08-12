using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class OpeningPropertySetFiniteMetricsSmoke
    {
        internal static void Run()
        {
            var properties = new OpeningPropertySet();
            Equal(900d, properties.WidthMm, "default width");
            Equal(2200d, properties.HeightMm, "default height");
            Equal(110d, properties.ThicknessMm, "default thickness");
            Equal(0d, properties.SillOffsetMm, "default sill offset");

            properties.WidthMm = 0d;
            properties.HeightMm = -1d;
            properties.ThicknessMm = 125.5d;
            properties.SillOffsetMm = -250d;
            Equal(0d, properties.WidthMm, "finite zero width");
            Equal(-1d, properties.HeightMm, "finite negative height preserved");
            Equal(125.5d, properties.ThicknessMm, "finite thickness");
            Equal(-250d, properties.SillOffsetMm, "finite negative sill preserved");

            Throws<ArgumentOutOfRangeException>(() => properties.WidthMm = double.NaN, "width NaN");
            Throws<ArgumentOutOfRangeException>(() => properties.HeightMm = double.PositiveInfinity, "height +Infinity");
            Throws<ArgumentOutOfRangeException>(() => properties.ThicknessMm = double.NegativeInfinity, "thickness -Infinity");
            Throws<ArgumentOutOfRangeException>(() => properties.SillOffsetMm = double.NaN, "sill NaN");

            Equal(0d, properties.WidthMm, "width unchanged after rejection");
            Equal(-1d, properties.HeightMm, "height unchanged after rejection");
            Equal(125.5d, properties.ThicknessMm, "thickness unchanged after rejection");
            Equal(-250d, properties.SillOffsetMm, "sill unchanged after rejection");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
