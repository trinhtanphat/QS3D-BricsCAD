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
            RejectsNumericLiteralUnderflowAtomically();
            PreservesZeroAndRepresentableSubnormalMetrics();
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

        private static void RejectsNumericLiteralUnderflowAtomically()
        {
            var element = new ProjectElement("B-underflow", ElementCategory.Beam);
            element.SetProperty(MeasuredSolidQuantityPolicy.SurfaceAreaProperty, "25");
            element.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, "1e-4000");
            element.MarkClean(ElementDirtyFlags.All);

            var error = Capture<InvalidOperationException>(() => MeasuredSolidQuantityPolicy.Apply(element));
            Contains("B-underflow/MeasuredSolidVolumeM3 underflowed to zero.", error.Message);
            Missing(element, "MeasuredSurfaceAreaM2");
            Missing(element, "MeasuredSolidVolumeM3");
            Missing(element, "GrossVolumeM3");
            Missing(element, "NetVolumeM3");
            if (element.Dirty != ElementDirtyFlags.None)
                throw new Exception("Rejected measured literal underflow must not mutate dirty state.");
        }

        private static void PreservesZeroAndRepresentableSubnormalMetrics()
        {
            var zero = new ProjectElement("B-zero", ElementCategory.Beam);
            zero.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, "0e-4000");
            if (!MeasuredSolidQuantityPolicy.Apply(zero))
                throw new Exception("True zero measured metric must remain valid.");
            Exact(0d, zero.Quantities["MeasuredSolidVolumeM3"]);

            var subnormal = new ProjectElement("B-subnormal", ElementCategory.Beam);
            subnormal.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, "5e-324");
            if (!MeasuredSolidQuantityPolicy.Apply(subnormal))
                throw new Exception("Representable subnormal measured metric must remain valid.");
            Exact(double.Epsilon, subnormal.Quantities["MeasuredSolidVolumeM3"]);
            Exact(double.Epsilon, subnormal.Quantities["GrossVolumeM3"]);
            Exact(double.Epsilon, subnormal.Quantities["NetVolumeM3"]);
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

        private static void Exact(double expected, double actual)
        {
            if (!expected.Equals(actual))
                throw new Exception("Expected exact " + expected + " but got " + actual + ".");
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected '" + actual + "' to contain '" + expected + "'.");
        }
    }
}
