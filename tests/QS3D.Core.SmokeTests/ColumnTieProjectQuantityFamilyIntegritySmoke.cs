using System;
using QS3D.Core.Domain;
using QS3D.Core.Rebar;

namespace QS3D.Core.SmokeTests
{
    internal static class ColumnTieProjectQuantityFamilyIntegritySmoke
    {
        public static void Run()
        {
            MismatchedColumnFamilyIsRejected();
            MatchingFamilyFallbackIsCaseInsensitive();
            NullFamilyUsesElementProperties();
        }

        private static void MismatchedColumnFamilyIsRejected()
        {
            var element = new ProjectElement("C1", ElementCategory.Column, "COL-A", string.Empty, string.Empty);
            var family = CreateFamily("COL-B");

            Throws<InvalidOperationException>(() => ColumnTieProjectQuantityService.Calculate(element, family));
        }

        private static void MatchingFamilyFallbackIsCaseInsensitive()
        {
            var element = new ProjectElement("C2", ElementCategory.Column, "COL-A", string.Empty, string.Empty);
            var family = CreateFamily("col-a");

            AssertPositive(ColumnTieProjectQuantityService.Calculate(element, family));
        }

        private static void NullFamilyUsesElementProperties()
        {
            var element = new ProjectElement("C3", ElementCategory.Column, "COL-A", string.Empty, string.Empty);
            SetCoreDimensions(element);

            AssertPositive(ColumnTieProjectQuantityService.Calculate(element, null));
        }

        private static ProjectFamily CreateFamily(string id)
        {
            var family = new ProjectFamily(id, "Column Family", ElementCategory.Column);
            family.Properties["WidthM"] = "0.4";
            family.Properties["DepthM"] = "0.5";
            family.Properties["HeightM"] = "3";
            return family;
        }

        private static void SetCoreDimensions(ProjectElement element)
        {
            element.Properties["WidthM"] = "0.4";
            element.Properties["DepthM"] = "0.5";
            element.Properties["HeightM"] = "3";
        }

        private static void AssertPositive(ColumnTieQuantity quantity)
        {
            if (quantity.Count <= 0) throw new Exception("Expected at least one column tie.");
            if (quantity.TotalLengthM <= 0d) throw new Exception("Expected positive column tie total length.");
            if (quantity.TotalWeightKg <= 0d) throw new Exception("Expected positive column tie total weight.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
