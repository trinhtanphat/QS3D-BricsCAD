using System;
using QS3D.Core.Domain;
using QS3D.Core.Model;

namespace QS3D.Core.Recognition
{
    public static class EntitySnapshotCaptureEligibility
    {
        public static bool IsReady(EntitySnapshot snapshot, ElementCategory category, out string reason)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (!string.Equals(snapshot.EntityType, "ProxyEntity", StringComparison.OrdinalIgnoreCase))
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
            reason = ready ? string.Empty : "ProxyEntity has no finite positive primary metric for " + category + "; review it after a supported BRC measurement adapter supplies Length, Area, or Volume.";
            return ready;
        }

        public static void EnsureReady(EntitySnapshot snapshot, ElementCategory category)
        {
            if (!IsReady(snapshot, category, out var reason)) throw new InvalidOperationException(reason);
        }

        private static bool Positive(double? value) => value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value) && value.Value > 0d;
    }
}
