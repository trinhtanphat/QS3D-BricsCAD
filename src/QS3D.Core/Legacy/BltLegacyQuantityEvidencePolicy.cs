using System;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Legacy
{
    /// <summary>
    /// Re-applies authoritative legacy quantity evidence after normal semantic
    /// regeneration/rules. This prevents default Family geometry from silently
    /// fabricating legacy formwork and keeps explicit legacy concrete stable after
    /// any later dirty/regenerate cycle.
    /// </summary>
    public static class BltLegacyQuantityEvidencePolicy
    {
        private const string SourceSystemProperty = "CAD.BLT.SourceSystem";
        private const string LegacyConcreteProperty = "CAD.BLT.LegacyConcreteM3";
        private const string LegacyFormworkProperty = "CAD.BLT.LegacyFormworkM2";
        private const string FormworkStatusProperty = "CAD.BLT.FormworkStatus";

        public static bool Apply(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!element.Properties.TryGetValue(SourceSystemProperty, out var source) ||
                !string.Equals((source ?? string.Empty).Trim(), "BLT3D", StringComparison.OrdinalIgnoreCase))
                return false;

            var changed = false;
            if (TryRead(element, LegacyConcreteProperty, out var concrete))
            {
                changed |= Set(element, "MeasuredSolidVolumeM3", concrete);
                changed |= Set(element, "GrossVolumeM3", concrete);
                changed |= Set(element, "NetVolumeM3", concrete);
                changed |= Set(element, "DeductionM3", 0d);
            }

            if (TryRead(element, LegacyFormworkProperty, out var formwork))
            {
                changed |= Set(element, "FormworkM2", formwork);
                if (!element.Properties.TryGetValue(FormworkStatusProperty, out var current) ||
                    !string.Equals(current, "ExactLegacyQuantity", StringComparison.Ordinal))
                {
                    element.Properties[FormworkStatusProperty] = "ExactLegacyQuantity";
                    changed = true;
                }
            }
            else
            {
                if (element.Quantities.Remove("FormworkM2")) changed = true;
                if (!element.Properties.TryGetValue(FormworkStatusProperty, out var current) ||
                    !string.Equals(current, "PENDING_EXACT_EVIDENCE", StringComparison.Ordinal))
                {
                    element.Properties[FormworkStatusProperty] = "PENDING_EXACT_EVIDENCE";
                    changed = true;
                }
            }

            return changed;
        }

        private static bool TryRead(ProjectElement element, string key, out double value)
        {
            value = 0d;
            if (!element.Properties.TryGetValue(key, out var raw)) return false;
            if (raw == null || !string.Equals(raw, raw.Trim(), StringComparison.Ordinal) ||
                !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(element.Id + "/" + key + " must be a canonical finite non-negative invariant quantity.");
            return true;
        }

        private static bool Set(ProjectElement element, string key, double value)
        {
            if (element.Quantities.TryGetValue(key, out var current) && current.Equals(value)) return false;
            element.SetQuantity(key, value);
            return true;
        }
    }
}
