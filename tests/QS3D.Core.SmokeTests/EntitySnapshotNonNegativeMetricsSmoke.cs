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
            var setters = new (string Name, Action<double?> Set)[]
            {
                (nameof(EntitySnapshot.LengthDrawingUnits), value => snapshot.LengthDrawingUnits = value),
                (nameof(EntitySnapshot.AreaDrawingUnitsSquared), value => snapshot.AreaDrawingUnitsSquared = value),
                (nameof(EntitySnapshot.SurfaceAreaDrawingUnitsSquared), value => snapshot.SurfaceAreaDrawingUnitsSquared = value),
                (nameof(EntitySnapshot.VolumeDrawingUnitsCubed), value => snapshot.VolumeDrawingUnitsCubed = value)
            };

            foreach (var item in setters)
            {
                item.Set(null);
                item.Set(0d);
                item.Set(1.25d);
                Throws<ArgumentOutOfRangeException>(() => item.Set(-0.001d), item.Name + " negative");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.NaN), item.Name + " NaN");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.PositiveInfinity), item.Name + " positive infinity");
                Throws<ArgumentOutOfRangeException>(() => item.Set(double.NegativeInfinity), item.Name + " negative infinity");
            }
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
