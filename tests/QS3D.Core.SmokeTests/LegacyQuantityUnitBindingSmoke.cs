using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class LegacyQuantityUnitBindingSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        private static void Run()
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Millimeter (legacy assumption)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "Millimeter"
            };

            if (!DrawingUnitResolutionPolicy.TryResolve(LengthUnit.Millimeter, metadata, out var resolution))
                throw new InvalidOperationException("Live millimeter INSUNITS must resolve even when only legacy quantity metadata exists.");
            if (resolution.Source != DrawingUnitResolutionSource.NativeInsunits || resolution.Unit != LengthUnit.Millimeter)
                throw new InvalidOperationException("Live INSUNITS must remain authoritative during legacy BQ binding migration.");

            if (!DrawingUnitResolutionPolicy.BindQuantityUnit(metadata, true, resolution.Unit, resolution.Source))
                throw new InvalidOperationException("Compatible legacy quantity metadata must migrate to a canonical binding.");
            if (!string.Equals(metadata[DrawingUnitResolutionPolicy.BoundMetadataKey], "Millimeter", StringComparison.Ordinal))
                throw new InvalidOperationException("Legacy BQ migration did not persist the canonical millimeter binding.");
            if (!string.Equals(metadata[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey], "Millimeter", StringComparison.Ordinal))
                throw new InvalidOperationException("Legacy BQ migration did not normalize the effective drawing unit.");
            if (!string.Equals(metadata[DrawingUnitResolutionPolicy.BindingSourceMetadataKey], DrawingUnitResolutionSource.NativeInsunits.ToString(), StringComparison.Ordinal))
                throw new InvalidOperationException("Legacy BQ migration did not record the live INSUNITS binding source.");
            if (DrawingUnitResolutionPolicy.BindQuantityUnit(metadata, true, resolution.Unit, resolution.Source))
                throw new InvalidOperationException("Canonical quantity binding migration must be idempotent.");

            var mismatchedLegacy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "Meter (legacy assumption)",
                [DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "Meter"
            };
            var mismatchBlocked = false;
            try
            {
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    mismatchedLegacy,
                    true,
                    LengthUnit.Millimeter,
                    DrawingUnitResolutionSource.NativeInsunits);
            }
            catch (InvalidOperationException)
            {
                mismatchBlocked = true;
            }

            if (!mismatchBlocked)
                throw new InvalidOperationException("Legacy binding migration must reject a unit that conflicts with live INSUNITS.");
        }
    }
}
