using System;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotFiniteMetricsSmoke
    {
        internal static void Run()
        {
            var snapshot = new EntitySnapshot("1A", "Line", "A-WALL");
            Equal(null, snapshot.LengthDrawingUnits, "default length");
            Equal(null, snapshot.AreaDrawingUnitsSquared, "default area");
            Equal(null, snapshot.SurfaceAreaDrawingUnitsSquared, "default surface area");
            Equal(null, snapshot.VolumeDrawingUnitsCubed, "default volume");

            snapshot.LengthDrawingUnits = 0d;
            snapshot.AreaDrawingUnitsSquared = -1d;
            snapshot.SurfaceAreaDrawingUnitsSquared = 12.5d;
            snapshot.VolumeDrawingUnitsCubed = null;
            Equal(0d, snapshot.LengthDrawingUnits, "finite zero length");
            Equal(-1d, snapshot.AreaDrawingUnitsSquared, "finite negative area preserved");
            Equal(12.5d, snapshot.SurfaceAreaDrawingUnitsSquared, "finite surface area");
            Equal(null, snapshot.VolumeDrawingUnitsCubed, "nullable volume");

            Throws<ArgumentOutOfRangeException>(() => snapshot.LengthDrawingUnits = double.NaN, "length NaN");
            Throws<ArgumentOutOfRangeException>(() => snapshot.AreaDrawingUnitsSquared = double.PositiveInfinity, "area +Infinity");
            Throws<ArgumentOutOfRangeException>(() => snapshot.SurfaceAreaDrawingUnitsSquared = double.NegativeInfinity, "surface area -Infinity");
            Throws<ArgumentOutOfRangeException>(() => snapshot.VolumeDrawingUnitsCubed = double.NaN, "volume NaN");

            Equal(0d, snapshot.LengthDrawingUnits, "length unchanged after rejection");
            Equal(-1d, snapshot.AreaDrawingUnitsSquared, "area unchanged after rejection");
            Equal(12.5d, snapshot.SurfaceAreaDrawingUnitsSquared, "surface area unchanged after rejection");
            Equal(null, snapshot.VolumeDrawingUnitsCubed, "volume unchanged after rejection");
        }

        private static void Equal(double? expected, double? actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + Format(expected) + ", actual " + Format(actual) + ".");
        }

        private static string Format(double? value) => value.HasValue ? value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "<null>";

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
