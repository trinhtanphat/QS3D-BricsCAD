using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class EntitySnapshotNonNegativeMetricsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var snapshot = new EntitySnapshot("A1", "ProxyEntity", "A-MODEL");
            var metrics = new (string Name, Action<double?> Set, Func<double?> Read)[]
            {
                (nameof(EntitySnapshot.LengthDrawingUnits), value => snapshot.LengthDrawingUnits = value, () => snapshot.LengthDrawingUnits),
                (nameof(EntitySnapshot.AreaDrawingUnitsSquared), value => snapshot.AreaDrawingUnitsSquared = value, () => snapshot.AreaDrawingUnitsSquared),
                (nameof(EntitySnapshot.SurfaceAreaDrawingUnitsSquared), value => snapshot.SurfaceAreaDrawingUnitsSquared = value, () => snapshot.SurfaceAreaDrawingUnitsSquared),
                (nameof(EntitySnapshot.VolumeDrawingUnitsCubed), value => snapshot.VolumeDrawingUnitsCubed = value, () => snapshot.VolumeDrawingUnitsCubed)
            };
            var negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);

            foreach (var item in metrics)
            {
                item.Set(null);
                if (item.Read().HasValue)
                    throw new Exception("EntitySnapshotNonNegativeMetricsSmoke did not preserve null for " + item.Name + ".");

                item.Set(0d);
                AssertPositiveZero(item.Read(), item.Name + " positive zero");

                item.Set(negativeZero);
                AssertPositiveZero(item.Read(), item.Name + " negative zero");

                item.Set(1.25d);
                if (item.Read() != 1.25d)
                    throw new Exception("EntitySnapshotNonNegativeMetricsSmoke changed a positive finite value for " + item.Name + ".");

                Throws<ArgumentOutOfRangeException>(() => item.Set(-0.001d), item.Name + " negative");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.NaN), item.Name + " NaN");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.PositiveInfinity), item.Name + " positive infinity");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.NegativeInfinity), item.Name + " negative infinity");
            }
        }

        private static void AssertPositiveZero(double? value, string label)
        {
            if (!value.HasValue || BitConverter.DoubleToInt64Bits(value.Value) != 0L)
                throw new Exception("EntitySnapshotNonNegativeMetricsSmoke expected canonical positive zero for " + label + ".");
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
            throw new Exception("EntitySnapshotNonNegativeMetricsSmoke expected " + typeof(TException).Name + " for " + label + ".");
        }
    }
}
