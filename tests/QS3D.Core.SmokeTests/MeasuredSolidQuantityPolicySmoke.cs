using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasuredSolidQuantityPolicySmoke
    {
        public static void Run()
        {
            RemovesPolicyOwnedVolumesWhenSourceDisappears();
            PreservesIndependentVolumeOverride();
            RemovesPolicyOwnedVolumesWhenCategoryBecomesUnsupported();
            CleanupMarksPreviouslyCleanElementQuantityDirty();
            NoRemovalLeavesCleanElementClean();
        }

        private static void RemovesPolicyOwnedVolumesWhenSourceDisappears()
        {
            var element = CreateMeasuredBeam();
            element.SetProperty(MeasuredSolidQuantityPolicy.SurfaceAreaProperty, "25");
            element.Properties.Remove(MeasuredSolidQuantityPolicy.VolumeProperty);

            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new Exception("Removing a measured solid source must be handled by the quantity policy.");

            Missing(element, "MeasuredSolidVolumeM3");
            Missing(element, "GrossVolumeM3");
            Missing(element, "NetVolumeM3");
            Near(25d, element.Quantities["MeasuredSurfaceAreaM2"]);
        }

        private static void PreservesIndependentVolumeOverride()
        {
            var element = CreateMeasuredBeam();
            element.SetQuantity("GrossVolumeM3", 99d);
            element.Properties.Remove(MeasuredSolidQuantityPolicy.VolumeProperty);

            MeasuredSolidQuantityPolicy.Apply(element);

            Missing(element, "MeasuredSolidVolumeM3");
            Missing(element, "NetVolumeM3");
            Near(99d, element.Quantities["GrossVolumeM3"]);
        }

        private static void RemovesPolicyOwnedVolumesWhenCategoryBecomesUnsupported()
        {
            var element = CreateMeasuredBeam();
            element.Category = ElementCategory.Room;

            MeasuredSolidQuantityPolicy.Apply(element);

            Missing(element, "MeasuredSolidVolumeM3");
            Missing(element, "GrossVolumeM3");
            Missing(element, "NetVolumeM3");
        }

        private static void CleanupMarksPreviouslyCleanElementQuantityDirty()
        {
            var element = CreateMeasuredBeam();
            element.MarkClean(ElementDirtyFlags.All);
            element.Properties.Remove(MeasuredSolidQuantityPolicy.VolumeProperty);

            if (element.Dirty != ElementDirtyFlags.None)
                throw new Exception("Measured solid cleanup regression precondition requires a clean element.");
            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new Exception("Measured solid cleanup must report handled when policy-owned outputs are removed.");

            if (element.Dirty != ElementDirtyFlags.Quantity)
                throw new Exception("Measured solid cleanup must mark exactly Quantity dirty on a previously clean element.");
            Missing(element, "MeasuredSolidVolumeM3");
            Missing(element, "GrossVolumeM3");
            Missing(element, "NetVolumeM3");
        }

        private static void NoRemovalLeavesCleanElementClean()
        {
            var element = new ProjectElement("R1", ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);

            if (MeasuredSolidQuantityPolicy.Apply(element))
                throw new Exception("Measured solid policy must not report handled when no measured inputs or stale outputs exist.");
            if (element.Dirty != ElementDirtyFlags.None)
                throw new Exception("Measured solid no-op must not invent dirty state.");
        }

        private static ProjectElement CreateMeasuredBeam()
        {
            var element = new ProjectElement("B1", ElementCategory.Beam);
            element.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, "12.5");
            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new Exception("Measured solid volume was not applied.");

            Near(12.5d, element.Quantities["MeasuredSolidVolumeM3"]);
            Near(12.5d, element.Quantities["GrossVolumeM3"]);
            Near(12.5d, element.Quantities["NetVolumeM3"]);
            return element;
        }

        private static void Missing(ProjectElement element, string key)
        {
            if (element.Quantities.ContainsKey(key))
                throw new Exception(key + " survived after its measured source was removed.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }
    }
}
