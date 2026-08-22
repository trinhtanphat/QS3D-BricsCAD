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
