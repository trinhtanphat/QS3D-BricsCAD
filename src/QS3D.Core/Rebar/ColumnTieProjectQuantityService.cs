using System;
using System.Globalization;
using QS3D.Core.Domain;

namespace QS3D.Core.Rebar
{
    public static class ColumnTieProjectQuantityService
    {
        public static ColumnTieQuantity Calculate(ProjectElement element, ProjectFamily? family)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Category != ElementCategory.Column)
                throw new InvalidOperationException("Column tie quantities can only be calculated for Column elements.");
            if (family != null)
            {
                if (family.Category != ElementCategory.Column)
                    throw new InvalidOperationException("Column tie quantity family must have the Column category.");
                if (!string.Equals(family.Id, element.FamilyId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Column tie quantity family must match the element family id.");
            }

            var diameterMm = Positive(Number(element, family, "RebarTieDiameterMm", 8d), element.Id + "/RebarTieDiameterMm");
            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = Positive(Number(element, family, "WidthM"), element.Id + "/WidthM"),
                DepthM = Positive(Number(element, family, "DepthM"), element.Id + "/DepthM"),
                HeightM = Positive(Number(element, family, "HeightM"), element.Id + "/HeightM"),
                CoverM = NonNegative(Number(element, family, "RebarCoverM", 0.04d), element.Id + "/RebarCoverM"),
                DiameterMm = diameterMm,
                SpacingMm = Positive(Number(element, family, "RebarTieSpacingMm", 150d), element.Id + "/RebarTieSpacingMm"),
                BottomClearanceM = NonNegative(Number(element, family, "RebarTieBottomClearanceM", 0d), element.Id + "/RebarTieBottomClearanceM"),
                TopClearanceM = NonNegative(Number(element, family, "RebarTieTopClearanceM", 0d), element.Id + "/RebarTieTopClearanceM")
            });
            var hookAllowanceM = NonNegative(Number(element, family, "RebarTieHookAllowanceM", 0d), element.Id + "/RebarTieHookAllowanceM");
            return ColumnTieQuantityCalculator.Calculate(layout, diameterMm, hookAllowanceM);
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string key, double fallback = 0d)
        {
            if (element.Properties.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return ParseFinite(value, element.Id + "/" + key);
            if (family != null && family.Properties.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
                return ParseFinite(value, "family " + family.Id + "/" + key);
            return fallback;
        }

        private static double ParseFinite(string value, string label)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ||
                double.IsNaN(result) || double.IsInfinity(result))
                throw new InvalidOperationException(label + " must be a finite invariant-culture number: " + value);
            return result;
        }

        private static double Positive(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new InvalidOperationException(label + " must be greater than zero.");
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException(label + " must be non-negative.");
            return value;
        }
    }
}
