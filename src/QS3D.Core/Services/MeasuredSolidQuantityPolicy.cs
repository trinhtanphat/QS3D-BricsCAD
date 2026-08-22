using System;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class MeasuredSolidQuantityPolicy
    {
        public const string VolumeProperty = "MeasuredSolidVolumeM3";
        public const string SurfaceAreaProperty = "MeasuredSolidSurfaceAreaM2";

        public static bool Apply(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            // Validate every applicable measured input before mutating quantities so
            // malformed later fields cannot leave a partially applied measurement.
            var hasSurfaceArea = TryRead(element, SurfaceAreaProperty, out var surfaceArea);
            var hasVolume = false;
            var volume = 0d;
            if (SupportsMaterialVolume(element.Category))
                hasVolume = TryRead(element, VolumeProperty, out volume);

            if (hasSurfaceArea)
                element.SetQuantity("MeasuredSurfaceAreaM2", surfaceArea);
            if (hasVolume)
            {
                element.SetQuantity("MeasuredSolidVolumeM3", volume);
                element.SetQuantity("GrossVolumeM3", volume);
                element.SetQuantity("NetVolumeM3", volume);
            }
            return hasSurfaceArea || hasVolume;
        }

        private static bool TryRead(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw)) return false;
            if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be a finite non-negative metric.");
            return true;
        }

        private static bool SupportsMaterialVolume(ElementCategory category)
        {
            switch (category)
            {
                case ElementCategory.Beam:
                case ElementCategory.Slab:
                case ElementCategory.Column:
                case ElementCategory.StructuralWall:
                case ElementCategory.ArchitecturalWall:
                case ElementCategory.GlassWall:
                case ElementCategory.WallFinish:
                case ElementCategory.WallPier:
                case ElementCategory.Stair:
                case ElementCategory.Foundation:
                case ElementCategory.Earthwork:
                case ElementCategory.Railing:
                case ElementCategory.CustomQuantity:
                    return true;
                default:
                    return false;
            }
        }
    }
}
