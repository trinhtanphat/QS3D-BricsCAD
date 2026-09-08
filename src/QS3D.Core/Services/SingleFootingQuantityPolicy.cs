using System;
using System.Globalization;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;

namespace QS3D.Core.Services
{
    /// <summary>Quantity projection of the same six dimensions used by the native footing builder.</summary>
    public static class SingleFootingQuantityPolicy
    {
        public static bool TryApply(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Category != ElementCategory.Foundation ||
                (!HasMarker(element, "CategoryCode", "Foundation.SingleFooting") &&
                 !HasMarker(element, "SingleFootingSubtype", "MongDon"))) return false;

            Apply(element, new SingleFootingDimensions(
                Read(element, "L1"), Read(element, "W1"), Read(element, "L2"),
                Read(element, "W2"), Read(element, "H1"), Read(element, "H2")));
            return true;
        }

        public static void Apply(ProjectElement element, SingleFootingDimensions dimensions)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            if (element.Category != ElementCategory.Foundation)
                throw new InvalidOperationException("Single footing quantities require a Foundation element.");
            // A captured/measured-solid override is a different evidence lane. In particular,
            // RegenerationEngine applies it after structural quantities. Require it to be
            // reconciled rather than overwrite this loft with an older measured prism.
            if (element.Properties.ContainsKey(MeasuredSolidQuantityPolicy.VolumeProperty) ||
                element.Properties.ContainsKey(MeasuredSolidQuantityPolicy.SurfaceAreaProperty) ||
                element.Quantities.ContainsKey("MeasuredSolidVolumeM3") ||
                element.Quantities.ContainsKey("MeasuredSurfaceAreaM2"))
                throw new InvalidOperationException("Single footing has incompatible measured-solid evidence; reconcile it before regeneration.");

            // Prepare every value before changing quantities. Never fall back to the captured
            // footprint's old AreaM2/ThicknessM prism or a cached VolumeM3 property.
            var area = QuantityMath.Multiply(dimensions.L1M, dimensions.W1M, "single footing base area");
            var height = QuantityMath.Add(dimensions.H1M, dimensions.H2M, "single footing height");
            var volume = QuantityMath.Positive(dimensions.VolumeM3);
            if (volume <= 0d)
                throw new InvalidOperationException("Single footing volume must be representable and positive.");

            // There is no footing-specific rule/contact projection here. The generic captured
            // prism's formwork totals are not evidence for the replacement loft. As for beams,
            // fail closed instead of reporting stale totals or inventing a formwork rule.
            foreach (var key in new[] { "FormworkM2", "NetFormworkM2", "GrossFormworkM2",
                "ConcreteContactDeductionM2", "FormworkDeductionM2" })
                element.Quantities.Remove(key);

            element.SetQuantity("AreaM2", area);
            element.SetQuantity("ThicknessM", height);
            element.SetQuantity("GrossVolumeM3", volume);
            element.SetQuantity("NetVolumeM3", volume);
            // Reporting prefers these aliases. Remove stale cache entries rather than
            // manufacture aliases that would mask subsequent user rules on Gross/NetVolume.
            // With gross=net, reporting derives a verified zero deduction; a later explicit
            // quantity rule may supply a different net volume/deduction by the usual contract.
            element.Quantities.Remove("GrossConcreteM3");
            element.Quantities.Remove("NetConcreteM3");
            element.Quantities.Remove("DeductionM3");
        }

        private static bool HasMarker(ProjectElement element, string key, string expected) =>
            element.Properties.TryGetValue(key, out var value) &&
            string.Equals((value ?? string.Empty).Trim(), expected, StringComparison.OrdinalIgnoreCase);

        private static double Read(ProjectElement element, string suffix)
        {
            var key = "SINGLE_FOOTING_" + suffix;
            // Legacy keys are admitted only when the canonical key is absent. A corrupt
            // current value must not silently fall back to an older dimension.
            if (!element.Properties.TryGetValue(key, out var raw))
                element.Properties.TryGetValue("SingleFooting" + suffix + "M", out raw);
            if (string.IsNullOrWhiteSpace(raw) ||
                !double.TryParse(raw!.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(element.Id + "/" + key + " requires a finite dimension.");
            if (value == 0d && (BitConverter.DoubleToInt64Bits(value) < 0L || HasNonZeroSignificand(raw!)))
                throw new InvalidOperationException(element.Id + "/" + key + " underflowed or is negative zero.");
            return value;
        }

        private static bool HasNonZeroSignificand(string raw)
        {
            foreach (var character in raw)
            {
                if (character == 'e' || character == 'E') break;
                if (character >= '1' && character <= '9') return true;
            }
            return false;
        }
    }
}
