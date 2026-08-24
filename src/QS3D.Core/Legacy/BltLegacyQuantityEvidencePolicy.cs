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

        internal readonly struct PreparedEvidence
        {
            public PreparedEvidence(bool isLegacy, bool hasConcrete, double concrete, bool hasFormwork, double formwork)
            {
                IsLegacy = isLegacy;
                HasConcrete = hasConcrete;
                Concrete = concrete;
                HasFormwork = hasFormwork;
                Formwork = formwork;
            }

            public bool IsLegacy { get; }
            public bool HasConcrete { get; }
            public double Concrete { get; }
            public bool HasFormwork { get; }
            public double Formwork { get; }
        }

        public static bool Apply(ProjectElement element)
        {
            return ApplyPrepared(element, Prepare(element));
        }

        internal static PreparedEvidence Prepare(ProjectElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!element.Properties.TryGetValue(SourceSystemProperty, out var source) ||
                !string.Equals((source ?? string.Empty).Trim(), "BLT3D", StringComparison.OrdinalIgnoreCase))
                return default;

            // Parse every applicable legacy field before any quantity/property write.
            // A malformed later field must never leave earlier legacy evidence applied.
            var hasConcrete = TryRead(element, LegacyConcreteProperty, out var concrete);
            var hasFormwork = TryRead(element, LegacyFormworkProperty, out var formwork);
            return new PreparedEvidence(true, hasConcrete, concrete, hasFormwork, formwork);
        }

        internal static bool ApplyPrepared(ProjectElement element, PreparedEvidence evidence)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (!evidence.IsLegacy) return false;

            var changed = false;
            if (evidence.HasConcrete)
            {
                changed |= Set(element, "MeasuredSolidVolumeM3", evidence.Concrete);
                changed |= Set(element, "GrossVolumeM3", evidence.Concrete);
                changed |= Set(element, "NetVolumeM3", evidence.Concrete);
                changed |= Set(element, "DeductionM3", 0d);
            }

            if (evidence.HasFormwork)
            {
                changed |= Set(element, "FormworkM2", evidence.Formwork);
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
