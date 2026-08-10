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
                throw new InvalidOperationException("Column tie quantity requires a Column semantic element: " + element.Id);
            if (family != null && family.Category != ElementCategory.Column)
                throw new InvalidOperationException("Column tie family category mismatch: " + family.Id);

            var widthM = Positive(Number(element, family, "WidthM", 0.4d), element.Id + "/WidthM");
            var depthM = Positive(Number(element, family, "DepthM", 0.4d), element.Id + "/DepthM");
            var heightM = Positive(Number(element, family, "HeightM", 3.6d), element.Id + "/HeightM");
            var coverM = NonNegative(Number(element, family, "RebarCoverM", 0.04d), element.Id + "/RebarCoverM");
            var diameterMm = Positive(Number(element, family, "RebarTieDiameterMm", 8d), element.Id + "/RebarTieDiameterMm");
            var spacingMm = Positive(Number(element, family, "RebarTieSpacingMm", 150d), element.Id + "/RebarTieSpacingMm");
            var bottomClearanceM = NonNegative(Number(element, family, "RebarTieBottomClearanceM", 0d), element.Id + "/RebarTieBottomClearanceM");
            var topClearanceM = NonNegative(Number(element, family, "RebarTieTopClearanceM", 0d), element.Id + "/RebarTieTopClearanceM");
            var hookAllowanceM = NonNegative(Number(element, family, "RebarTieHookAllowanceM", 0d), element.Id + "/RebarTieHookAllowanceM");

            var layout = ColumnTieLayoutPlanner.Plan(new ColumnTieLayoutInput
            {
                WidthM = widthM,
                DepthM = depthM,
                HeightM = heightM,
                CoverM = coverM,
                DiameterMm = diameterMm,
                SpacingMm = spacingMm,
                BottomClearanceM = bottomClearanceM,
                TopClearanceM = topClearanceM
            });
            return ColumnTieQuantityCalculator.Calculate(layout, diameterMm, hookAllowanceM);
        }

        private static double Number(ProjectElement element, ProjectFamily? family, string key, double fallback)
        {
            if (element.Properties.TryGetValue(key, out var instance) && !string.IsNullOrWhiteSpace(instance))
                return ParseFinite(instance, element.Id + "/" + key);
            if (family != null && family.Properties.TryGetValue(key, out var inherited) && !string.IsNullOrWhiteSpace(inherited))
                return ParseFinite(inherited, "family " + family.Id + "/" + key);
            return Finite(fallback, "fallback " + key);
        }

        private static double ParseFinite(string text, string label)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidOperationException(label + " không phải số hữu hạn hợp lệ: " + text);
            return value;
        }

        private static double Positive(double value, string label)
        {
            value = Finite(value, label);
            if (value <= 0d) throw new InvalidOperationException(label + " phải lớn hơn 0.");
            return value;
        }

        private static double NonNegative(double value, string label)
        {
            value = Finite(value, label);
            if (value < 0d) throw new InvalidOperationException(label + " phải >= 0.");
            return value;
        }

        private static double Finite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new InvalidOperationException(label + " phải là số hữu hạn.");
            return value;
        }
    }
}
