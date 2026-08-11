using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class DrawingUnitResolutionSmoke
    {
        public static void Run()
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (DrawingUnitResolutionPolicy.TryResolve(null, metadata, out _))
                throw new Exception("Undefined INSUNITS must remain unresolved without an explicit project override.");

            DrawingUnitResolutionPolicy.SetProjectOverride(metadata, LengthUnit.Meter);
            if (!DrawingUnitResolutionPolicy.TryResolve(null, metadata, out var project) ||
                project.Unit != LengthUnit.Meter || project.Source != DrawingUnitResolutionSource.ProjectOverride)
                throw new Exception("Explicit project drawing-unit override was not resolved.");

            if (!DrawingUnitResolutionPolicy.TryResolve(LengthUnit.Foot, metadata, out var native) ||
                native.Unit != LengthUnit.Foot || native.Source != DrawingUnitResolutionSource.NativeInsunits)
                throw new Exception("Known native INSUNITS must take precedence over a project override.");

            var empty = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!DrawingUnitResolutionPolicy.BindQuantityUnit(empty, false, LengthUnit.Meter, DrawingUnitResolutionSource.ProjectOverride))
                throw new Exception("First quantity use must bind the effective drawing unit.");
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(empty, true, LengthUnit.Meter);
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(empty, true, LengthUnit.Millimeter));

            var legacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Millimeter (assumed)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "INSUNITS unsupported/undefined; assumed Millimeter"
            };
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(legacy, true, LengthUnit.Millimeter);
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(legacy, true, LengthUnit.Meter));

            metadata[DrawingUnitResolutionPolicy.OverrideMetadataKey] = "NotAUnit";
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, metadata, out _));
            Throws<ArgumentOutOfRangeException>(() => DrawingUnitResolutionPolicy.SetProjectOverride(metadata, (LengthUnit)999));

            Throws<ArgumentOutOfRangeException>(() => new ProjectUnitPolicy((LengthUnit)999));
            Throws<ArgumentOutOfRangeException>(() => new ProjectUnitPolicy(LengthUnit.Meter, 10));
            var unitPolicy = new ProjectUnitPolicy(LengthUnit.Centimeter, 2);
            if (unitPolicy.DrawingUnit != LengthUnit.Centimeter || unitPolicy.DisplayDecimals != 2 ||
                Math.Abs(unitPolicy.ToMeters(123d) - 1.23d) > 1e-12d ||
                Math.Abs(unitPolicy.RoundForDisplay(1.236d) - 1.24d) > 1e-12d)
                throw new Exception("Defined ProjectUnitPolicy values must preserve conversion and display behavior.");

            var path = Path.Combine(Path.GetTempPath(), "qs3d-unit-binding-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var state = new ProjectState("unit", "Unit binding");
                DrawingUnitResolutionPolicy.SetProjectOverride(state.Metadata, LengthUnit.Centimeter);
                DrawingUnitResolutionPolicy.BindQuantityUnit(state.Metadata, false, LengthUnit.Centimeter, DrawingUnitResolutionSource.ProjectOverride);
                new QsdbProjectStore().Save(state, path);
                var loaded = new QsdbProjectStore().Load(path);
                if (!DrawingUnitResolutionPolicy.TryResolve(null, loaded.Metadata, out var persisted) || persisted.Unit != LengthUnit.Centimeter)
                    throw new Exception("Drawing-unit override did not round-trip through QSDB.");
                DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(loaded.Metadata, true, LengthUnit.Centimeter);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
