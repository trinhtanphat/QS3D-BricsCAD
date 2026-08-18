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

            var boundOverrideConflict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = LengthUnit.Meter.ToString(),
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = LengthUnit.Foot.ToString(),
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = LengthUnit.Foot.ToString(),
                [DrawingUnitResolutionPolicy.BindingSourceMetadataKey] = DrawingUnitResolutionSource.NativeInsunits.ToString()
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.SetProjectOverride(boundOverrideConflict, LengthUnit.Millimeter));
            if (boundOverrideConflict[DrawingUnitResolutionPolicy.OverrideMetadataKey] != LengthUnit.Foot.ToString() ||
                boundOverrideConflict[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] != LengthUnit.Foot.ToString() ||
                boundOverrideConflict[DrawingUnitResolutionPolicy.BindingSourceMetadataKey] != DrawingUnitResolutionSource.NativeInsunits.ToString())
                throw new Exception("A rejected drawing-unit override must not partially mutate project metadata.");
            DrawingUnitResolutionPolicy.SetProjectOverride(boundOverrideConflict, LengthUnit.Meter);
            if (boundOverrideConflict[DrawingUnitResolutionPolicy.BoundMetadataKey] != LengthUnit.Meter.ToString() ||
                boundOverrideConflict[DrawingUnitResolutionPolicy.OverrideMetadataKey] != LengthUnit.Meter.ToString() ||
                boundOverrideConflict[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] != LengthUnit.Meter.ToString() ||
                boundOverrideConflict[DrawingUnitResolutionPolicy.BindingSourceMetadataKey] != DrawingUnitResolutionSource.ProjectOverride.ToString())
                throw new Exception("A drawing-unit override matching the quantity binding must remain supported.");

            var legacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Millimeter (assumed)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "INSUNITS unsupported/undefined; assumed Millimeter"
            };
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(legacy, true, LengthUnit.Millimeter);
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(legacy, true, LengthUnit.Meter));

            var legacyOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Millimeter (assumed)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "INSUNITS unsupported/undefined; assumed Millimeter"
            };
            DrawingUnitResolutionPolicy.SetProjectOverride(legacyOverride, LengthUnit.Meter);
            if (legacyOverride[DrawingUnitResolutionPolicy.OverrideMetadataKey] != LengthUnit.Meter.ToString())
                throw new Exception("Explicit override must still be stored for an unbound legacy project.");
            if (legacyOverride[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] != "Millimeter (assumed)")
                throw new Exception("Setting an override must preserve unbound legacy effective-unit evidence until quantity compatibility is resolved.");
            if (!DrawingUnitResolutionPolicy.TryResolve(null, legacyOverride, out var legacyOverrideResolution) ||
                legacyOverrideResolution.Unit != LengthUnit.Meter ||
                legacyOverrideResolution.Source != DrawingUnitResolutionSource.ProjectOverride)
                throw new Exception("Preserving legacy binding evidence must not prevent explicit override resolution.");
            Throws<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(legacyOverride, true, LengthUnit.Meter));
            Throws<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    legacyOverride,
                    true,
                    LengthUnit.Meter,
                    DrawingUnitResolutionSource.ProjectOverride));
            if (legacyOverride.ContainsKey(DrawingUnitResolutionPolicy.BoundMetadataKey) ||
                legacyOverride[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] != "Millimeter (assumed)")
                throw new Exception("Rejected legacy quantity rebinding must not establish a false bound or rewrite historical unit evidence.");
            if (!DrawingUnitResolutionPolicy.BindQuantityUnit(
                    legacyOverride,
                    false,
                    LengthUnit.Meter,
                    DrawingUnitResolutionSource.ProjectOverride))
                throw new Exception("An empty legacy project must remain free to adopt the explicit override.");
            if (legacyOverride[DrawingUnitResolutionPolicy.BoundMetadataKey] != LengthUnit.Meter.ToString() ||
                legacyOverride[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] != LengthUnit.Meter.ToString() ||
                legacyOverride[DrawingUnitResolutionPolicy.BindingSourceMetadataKey] != DrawingUnitResolutionSource.ProjectOverride.ToString())
                throw new Exception("Binding an empty legacy project must replace old evidence with the canonical override binding.");

            var lowercaseOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = "meter"
            };
            if (!DrawingUnitResolutionPolicy.TryResolve(null, lowercaseOverride, out var lowercaseResolution) ||
                lowercaseResolution.Unit != LengthUnit.Meter)
                throw new Exception("Named drawing-unit metadata must remain case-insensitive.");

            var numericOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = ((int)LengthUnit.Meter).ToString()
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, numericOverride, out _));

            var paddedOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = " Meter "
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, paddedOverride, out _));

            var emptyOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = string.Empty
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, emptyOverride, out _));

            var whitespaceOverride = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = "   "
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, whitespaceOverride, out _));

            var lowercaseBound = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = "meter"
            };
            DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(lowercaseBound, true, LengthUnit.Meter);

            var numericBound = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = ((int)LengthUnit.Meter).ToString()
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(numericBound, true, LengthUnit.Meter));

            var paddedBound = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = " Meter "
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(paddedBound, true, LengthUnit.Meter));

            var emptyBound = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = string.Empty
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(emptyBound, false, LengthUnit.Meter));

            var whitespaceBoundWithLegacyFallback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = "   ",
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Millimeter (assumed)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "INSUNITS unsupported/undefined; assumed Millimeter"
            };
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.ValidateQuantityCompatibility(whitespaceBoundWithLegacyFallback, true, LengthUnit.Millimeter));

            metadata[DrawingUnitResolutionPolicy.OverrideMetadataKey] = "NotAUnit";
            Throws<InvalidOperationException>(() => DrawingUnitResolutionPolicy.TryResolve(null, metadata, out _));
            Throws<ArgumentOutOfRangeException>(() => DrawingUnitResolutionPolicy.SetProjectOverride(metadata, (LengthUnit)999));

            Throws<ArgumentOutOfRangeException>(() => new ProjectUnitPolicy((LengthUnit)999));
            Throws<ArgumentOutOfRangeException>(() => new ProjectUnitPolicy(LengthUnit.Meter, 10));
            Throws<ArgumentOutOfRangeException>(() => ProjectUnitPolicy.ToDrawingUnit((LengthUnit)999));
            var unitPolicy = new ProjectUnitPolicy(LengthUnit.Centimeter, 2);
            if (unitPolicy.DrawingUnit != LengthUnit.Centimeter || unitPolicy.DisplayDecimals != 2 ||
                Math.Abs(unitPolicy.ToMeters(123d) - 1.23d) > 1e-12d ||
                Math.Abs(unitPolicy.RoundForDisplay(1.236d) - 1.24d) > 1e-12d)
                throw new Exception("Defined ProjectUnitPolicy values must preserve conversion and display behavior.");

            foreach (LengthUnit lengthUnit in Enum.GetValues(typeof(LengthUnit)))
            {
                var drawingUnit = ProjectUnitPolicy.ToDrawingUnit(lengthUnit);
                if (!string.Equals(lengthUnit.ToString(), drawingUnit.ToString(), StringComparison.Ordinal))
                    throw new Exception("ProjectUnitPolicy unit mapping changed semantic meaning for " + lengthUnit + ".");
            }

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
