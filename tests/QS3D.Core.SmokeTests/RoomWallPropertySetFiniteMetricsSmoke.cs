using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomWallPropertySetFiniteMetricsSmoke
    {
        internal static void Run()
        {
            RoomMetricsRejectNonFiniteValues();
            WallMetricsRejectNonFiniteValues();
        }

        private static void RoomMetricsRejectNonFiniteValues()
        {
            var properties = new RoomPropertySet();
            Equal(0d, properties.BaseOffsetMm, "room default base offset");
            Equal(0d, properties.TopOffsetMm, "room default top offset");

            properties.BaseOffsetMm = -125.5d;
            properties.TopOffsetMm = 250d;
            Equal(-125.5d, properties.BaseOffsetMm, "room finite negative base offset");
            Equal(250d, properties.TopOffsetMm, "room finite top offset");

            RejectsNonFinite(value => properties.BaseOffsetMm = value, () => properties.BaseOffsetMm, -125.5d, "room base offset");
            RejectsNonFinite(value => properties.TopOffsetMm = value, () => properties.TopOffsetMm, 250d, "room top offset");
        }

        private static void WallMetricsRejectNonFiniteValues()
        {
            var properties = new WallPropertySet();
            Equal(110d, properties.ThicknessMm, "wall default thickness");
            Equal(0d, properties.AxisToLeftMm, "wall default left axis offset");
            Equal(0d, properties.AxisToRightMm, "wall default right axis offset");
            Equal(0d, properties.BaseOffsetMm, "wall default base offset");
            Equal(0d, properties.TopOffsetMm, "wall default top offset");

            properties.ThicknessMm = -1d;
            properties.AxisToLeftMm = -25d;
            properties.AxisToRightMm = 25d;
            properties.BaseOffsetMm = -100d;
            properties.TopOffsetMm = 300d;
            Equal(-1d, properties.ThicknessMm, "wall finite negative thickness preserved");
            Equal(-25d, properties.AxisToLeftMm, "wall finite negative left axis offset");
            Equal(25d, properties.AxisToRightMm, "wall finite right axis offset");
            Equal(-100d, properties.BaseOffsetMm, "wall finite negative base offset");
            Equal(300d, properties.TopOffsetMm, "wall finite top offset");

            RejectsNonFinite(value => properties.ThicknessMm = value, () => properties.ThicknessMm, -1d, "wall thickness");
            RejectsNonFinite(value => properties.AxisToLeftMm = value, () => properties.AxisToLeftMm, -25d, "wall left axis offset");
            RejectsNonFinite(value => properties.AxisToRightMm = value, () => properties.AxisToRightMm, 25d, "wall right axis offset");
            RejectsNonFinite(value => properties.BaseOffsetMm = value, () => properties.BaseOffsetMm, -100d, "wall base offset");
            RejectsNonFinite(value => properties.TopOffsetMm = value, () => properties.TopOffsetMm, 300d, "wall top offset");
        }

        private static void RejectsNonFinite(Action<double> assign, Func<double> read, double expected, string label)
        {
            foreach (var invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Throws<ArgumentOutOfRangeException>(() => assign(invalid), label + " rejects non-finite value");
                Equal(expected, read(), label + " unchanged after rejection");
            }
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
