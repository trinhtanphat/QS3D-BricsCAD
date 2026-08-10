using System;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieProjectQuantitySmoke
    {
        public static void Run()
        {
            FamilyDefaultsDriveTieQuantity();
            InstanceOverridesWin();
            RejectsNonColumnAndInvalidProperty();
        }

        private static void FamilyDefaultsDriveTieQuantity()
        {
            var family = new ProjectFamily("F", ElementCategory.Column, "Column-400x500");
            family.Properties["WidthM"] = "0.4";
            family.Properties["DepthM"] = "0.5";
            family.Properties["HeightM"] = "3";
            family.Properties["RebarCoverM"] = "0.04";
            family.Properties["RebarTieDiameterMm"] = "8";
            family.Properties["RebarTieSpacingMm"] = "150";
            var element = new ProjectElement("C1", ElementCategory.Column, family.Id, string.Empty, string.Empty);
            var quantity = ColumnTieProjectQuantityService.Calculate(element, family);
            if (quantity.Count <= 1) throw new Exception("Expected multiple ties from family defaults.");
            Near(1.448d, quantity.CuttingLengthPerTieM, 1e-12d);
        }

        private static void InstanceOverridesWin()
        {
            var family = new ProjectFamily("F", ElementCategory.Column, "Column");
            family.Properties["WidthM"] = "0.4";
            family.Properties["DepthM"] = "0.4";
            family.Properties["HeightM"] = "2";
            family.Properties["RebarTieSpacingMm"] = "200";
            var element = new ProjectElement("C1", ElementCategory.Column, family.Id, string.Empty, string.Empty);
            element.Properties["RebarTieSpacingMm"] = "100";
            var tighter = ColumnTieProjectQuantityService.Calculate(element, family);
            element.Properties["RebarTieSpacingMm"] = "200";
            var wider = ColumnTieProjectQuantityService.Calculate(element, family);
            if (tighter.Count <= wider.Count) throw new Exception("Instance spacing override did not affect tie count.");
        }

        private static void RejectsNonColumnAndInvalidProperty()
        {
            Throws<InvalidOperationException>(() => ColumnTieProjectQuantityService.Calculate(
                new ProjectElement("B1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty), null));
            var element = new ProjectElement("C1", ElementCategory.Column, string.Empty, string.Empty, string.Empty);
            element.Properties["WidthM"] = "NaN";
            Throws<InvalidOperationException>(() => ColumnTieProjectQuantityService.Calculate(element, null));
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
