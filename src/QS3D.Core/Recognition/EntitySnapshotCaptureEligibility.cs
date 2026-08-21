using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Legacy;
using QS3D.Core.Model;

namespace QS3D.Core.Recognition
{
    public static class EntitySnapshotCaptureEligibility
    {
        public static bool IsReady(EntitySnapshot snapshot, ElementCategory category, out string reason)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!Enum.IsDefined(typeof(ElementCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category), category, "Capture eligibility requires a defined ElementCategory.");
            if (snapshot.HasQs3dGeneratedOwnershipMarker)
            {
                reason = "CAD object has a native QS3D generated-output ownership marker and cannot be captured as a semantic source.";
                return false;
            }
            if (!string.Equals(snapshot.EntityType, "ProxyEntity", StringComparison.OrdinalIgnoreCase))
            {
                reason = string.Empty;
                return true;
            }

            // A public, explicitly unit-labelled BLT ConcreteM3 value is authoritative
            // quantity evidence even when the unloaded proxy exposes no measurable host
            // geometry. It is intentionally limited to material-volume categories; a
            // FormworkM2 value alone does not establish primary capture geometry.
            if (SupportsLegacyConcrete(category) && TryPositiveLegacyConcrete(snapshot, out _))
            {
                reason = string.Empty;
                return true;
            }

            var hasLength = Positive(snapshot.LengthDrawingUnits);
            var hasArea = Positive(snapshot.AreaDrawingUnitsSquared);
            var hasVolume = Positive(snapshot.VolumeDrawingUnitsCubed);
            bool ready;
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.StructuralWall:
                case ElementCategory.WallPier:
                case ElementCategory.Railing:
                    ready = hasLength || hasVolume;
                    break;
                case ElementCategory.Column:
                case ElementCategory.Slab:
                case ElementCategory.Foundation:
                case ElementCategory.Stair:
                case ElementCategory.Earthwork:
                case ElementCategory.Room:
                case ElementCategory.FloorFinish:
                case ElementCategory.Waterproofing:
                case ElementCategory.WallFinish:
                case ElementCategory.CeilingFinish:
                    ready = hasArea || hasVolume;
                    break;
                default:
                    ready = hasLength || hasArea || hasVolume;
                    break;
            }
            reason = ready ? string.Empty : "ProxyEntity has no finite positive primary metric for " + category + "; review it after a supported BRC measurement adapter supplies Length, Area, Volume, or explicit BLT ConcreteM3 evidence.";
            return ready;
        }

        public static void EnsureReady(EntitySnapshot snapshot, ElementCategory category)
        {
            if (!IsReady(snapshot, category, out var reason)) throw new InvalidOperationException(reason);
        }

        private static bool TryPositiveLegacyConcrete(EntitySnapshot snapshot, out double value)
        {
            value = 0d;
            if (!snapshot.Metadata.TryGetValue(BltLegacyMetadataKeys.ConcreteM3, out var raw) ||
                raw == null || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return false;
            return value > 0d && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool SupportsLegacyConcrete(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.StructuralWall:
                case ElementCategory.Foundation:
                    return true;
                default:
                    return false;
            }
        }

        private static bool Positive(double? value) => value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value) && value.Value > 0d;
    }
}
