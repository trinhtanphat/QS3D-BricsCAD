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
            var supportsMaterialVolume = SupportsMaterialVolume(element.Category);
            var hasVolume = false;
            var volume = 0d;
            if (supportsMaterialVolume)
                hasVolume = TryRead(element, VolumeProperty, out volume);

            var hadMeasuredVolume = element.Quantities.TryGetValue("MeasuredSolidVolumeM3", out var previousMeasuredVolume);
            var handled = false;
            var removed = false;
            if (hasSurfaceArea)
            {
                element.SetQuantity("MeasuredSurfaceAreaM2", surfaceArea);
                handled = true;
            }
            else if (element.Quantities.Remove("MeasuredSurfaceAreaM2"))
            {
                handled = true;
                removed = true;
            }

            if (hasVolume)
            {
                element.SetQuantity("MeasuredSolidVolumeM3", volume);
                element.SetQuantity("GrossVolumeM3", volume);
                element.SetQuantity("NetVolumeM3", volume);
                handled = true;
            }
            else if (hadMeasuredVolume)
            {
                element.Quantities.Remove("MeasuredSolidVolumeM3");
                RemoveQuantityIfMatches(element, "GrossVolumeM3", previousMeasuredVolume);
                RemoveQuantityIfMatches(element, "NetVolumeM3", previousMeasuredVolume);
                handled = true;
                removed = true;
            }

            if (removed) element.MarkDirty(ElementDirtyFlags.Quantity);
            return handled;
        }

        private static bool RemoveQuantityIfMatches(ProjectElement element, string key, double expected)
        {
            return element.Quantities.TryGetValue(key, out var current) &&
                   current.Equals(expected) &&
                   element.Quantities.Remove(key);
        }

        private static bool TryRead(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw)) return false;
            if (raw != null && !string.Equals(raw, raw.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException(element.Id + "/" + key + " must not contain surrounding whitespace.");
            if (raw == null || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) || double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be a finite non-negative metric.");
            if (value == 0d && HasNonZeroSignificand(raw))
                throw new InvalidOperationException(element.Id + "/" + key + " underflowed to zero.");
            return true;
        }

        private static bool HasNonZeroSignificand(string raw)
        {
            if (raw == null) return false;
            for (var i = 0; i < raw.Length; i++)
            {
                var character = raw[i];
                if (character == 'e' || character == 'E') break;
                if (character >= '1' && character <= '9') return true;
            }
            return false;
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
