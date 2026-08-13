using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class WallPropertySetSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var wall = new WallPropertySet();

            wall.AxisToLeftMm = -0d;
            CanonicalPositiveZero(wall.AxisToLeftMm, "AxisToLeftMm");
            wall.AxisToRightMm = -0d;
            CanonicalPositiveZero(wall.AxisToRightMm, "AxisToRightMm");
            wall.BaseOffsetMm = -0d;
            CanonicalPositiveZero(wall.BaseOffsetMm, "BaseOffsetMm");
            wall.TopOffsetMm = -0d;
            CanonicalPositiveZero(wall.TopOffsetMm, "TopOffsetMm");

            wall.AxisToLeftMm = -55d;
            Equal(-55d, wall.AxisToLeftMm, "negative left axis offset");
            wall.AxisToRightMm = 70d;
            Equal(70d, wall.AxisToRightMm, "positive right axis offset");
            wall.BaseOffsetMm = -120d;
            Equal(-120d, wall.BaseOffsetMm, "negative base offset");
            wall.TopOffsetMm = 250d;
            Equal(250d, wall.TopOffsetMm, "positive top offset");

            Equal(110d, wall.ThicknessMm, "default thickness");
            wall.ThicknessMm = 200d;
            Equal(200d, wall.ThicknessMm, "positive thickness");
            Throws<ArgumentOutOfRangeException>(() => wall.ThicknessMm = -0d);
            Throws<ArgumentOutOfRangeException>(() => wall.ThicknessMm = 0d);
            Throws<ArgumentOutOfRangeException>(() => wall.ThicknessMm = -1d);

            Throws<ArgumentOutOfRangeException>(() => wall.AxisToLeftMm = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => wall.AxisToRightMm = double.PositiveInfinity);
            Throws<ArgumentOutOfRangeException>(() => wall.BaseOffsetMm = double.NegativeInfinity);
            Throws<ArgumentOutOfRangeException>(() => wall.TopOffsetMm = double.NaN);

            Require(!wall.CloseProfile, "CloseProfile default changed.");
            Require(!wall.FreeformProfile, "FreeformProfile default changed.");
            Require(string.Equals("top_level", wall.TopLevel, StringComparison.Ordinal), "TopLevel default changed.");
            Require(string.Equals("bottom_level", wall.BottomLevel, StringComparison.Ordinal), "BottomLevel default changed.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
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
