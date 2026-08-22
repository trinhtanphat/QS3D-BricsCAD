using System;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class MeasuredSolidQuantityAtomicitySmoke
    {
        public static void Run()
        {
            InvalidVolumeDoesNotPartiallyApplySurfaceArea();
            ValidSurfaceAndVolumeApplyTogether();
            UnsupportedCategoryStillIgnoresVolumeProperty();
            MissingSourcesRetractOnlyPolicyOwnedQuantities();
            RemovalAdvancesFreshnessWithoutNoOpTouch();
            RegenerationFallsBackAfterMeasuredSourcesDisappear();
        }

        private static void InvalidVolumeDoesNotPartiallyApplySurfaceArea()
        {
            var element = new ProjectElement("B-MEASURE-FAIL", ElementCategory.Beam);
            element.SetQuantity("MeasuredSurfaceAreaM2", 9d);
            element.Properties[MeasuredSolidQuantityPolicy.SurfaceAreaProperty] = "12.5";
            element.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "not-a-number";

            Throws<InvalidOperationException>(() => MeasuredSolidQuantityPolicy.Apply(element));

            Near(9d, element.Quantities["MeasuredSurfaceAreaM2"]);
            False(element.Quantities.ContainsKey("MeasuredSolidVolumeM3"));
            False(element.Quantities.ContainsKey("GrossVolumeM3"));
            False(element.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void ValidSurfaceAndVolumeApplyTogether()
        {
            var element = new ProjectElement("B-MEASURE-OK", ElementCategory.Beam);
            element.Properties[MeasuredSolidQuantityPolicy.SurfaceAreaProperty] = "12.5";
            element.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "3.75";

            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new InvalidOperationException("Valid measured solid inputs were not handled.");

            Near(12.5d, element.Quantities["MeasuredSurfaceAreaM2"]);
            Near(3.75d, element.Quantities["MeasuredSolidVolumeM3"]);
            Near(3.75d, element.Quantities["GrossVolumeM3"]);
            Near(3.75d, element.Quantities["NetVolumeM3"]);
        }

        private static void UnsupportedCategoryStillIgnoresVolumeProperty()
        {
            var element = new ProjectElement("D-MEASURE", ElementCategory.Door);
            element.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "not-a-number";

            if (MeasuredSolidQuantityPolicy.Apply(element))
                throw new InvalidOperationException("Unsupported-category volume input should remain unhandled.");
            False(element.Quantities.ContainsKey("MeasuredSolidVolumeM3"));
        }

        private static void MissingSourcesRetractOnlyPolicyOwnedQuantities()
        {
            var element = new ProjectElement("B-MEASURE-STALE", ElementCategory.Beam);
            element.SetQuantity("MeasuredSurfaceAreaM2", 12.5d);
            element.SetQuantity("MeasuredSolidVolumeM3", 3.75d);
            element.SetQuantity("GrossVolumeM3", 2d);
            element.SetQuantity("NetVolumeM3", 1.5d);

            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new InvalidOperationException("Removing stale policy-owned measured quantities must count as handled work.");

            False(element.Quantities.ContainsKey("MeasuredSurfaceAreaM2"));
            False(element.Quantities.ContainsKey("MeasuredSolidVolumeM3"));
            Near(2d, element.Quantities["GrossVolumeM3"]);
            Near(1.5d, element.Quantities["NetVolumeM3"]);
        }

        private static void RemovalAdvancesFreshnessWithoutNoOpTouch()
        {
            var element = new ProjectElement("B-MEASURE-FRESH", ElementCategory.Beam);
            element.SetQuantity("MeasuredSurfaceAreaM2", 12.5d);
            var beforeRemoval = element.UpdatedUtc;
            while (DateTime.UtcNow <= beforeRemoval) Thread.SpinWait(32);

            if (!MeasuredSolidQuantityPolicy.Apply(element))
                throw new InvalidOperationException("Stale measured quantity removal was not reported as handled.");
            if (element.UpdatedUtc <= beforeRemoval)
                throw new InvalidOperationException("Measured quantity removal did not advance element freshness metadata.");

            var afterRemoval = element.UpdatedUtc;
            if (MeasuredSolidQuantityPolicy.Apply(element))
                throw new InvalidOperationException("A measured-quantity true no-op must remain unhandled.");
            if (element.UpdatedUtc != afterRemoval)
                throw new InvalidOperationException("Measured-quantity true no-op changed element freshness metadata.");
        }

        private static void RegenerationFallsBackAfterMeasuredSourcesDisappear()
        {
            var project = new ProjectState("MEASURE-LIFECYCLE", "Measured lifecycle");
            var element = new ProjectElement("EW-MEASURE-LIFECYCLE", ElementCategory.Earthwork);
            element.SetProperty("AreaM2", "2");
            element.SetProperty("DepthM", "0.5");
            element.SetProperty(MeasuredSolidQuantityPolicy.SurfaceAreaProperty, "12.5");
            element.SetProperty(MeasuredSolidQuantityPolicy.VolumeProperty, "3.75");
            project.Elements.Add(element);

            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            if (engine.RegenerateDirty(project) != 1)
                throw new InvalidOperationException("Initial measured earthwork regeneration was not handled exactly once.");
            Near(12.5d, element.Quantities["MeasuredSurfaceAreaM2"]);
            Near(3.75d, element.Quantities["MeasuredSolidVolumeM3"]);
            Near(3.75d, element.Quantities["GrossVolumeM3"]);
            Near(3.75d, element.Quantities["NetVolumeM3"]);

            element.Properties.Remove(MeasuredSolidQuantityPolicy.SurfaceAreaProperty);
            element.Properties.Remove(MeasuredSolidQuantityPolicy.VolumeProperty);
            element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);

            if (engine.RegenerateDirty(project) != 1)
                throw new InvalidOperationException("Measured-source removal regeneration was not handled exactly once.");
            False(element.Quantities.ContainsKey("MeasuredSurfaceAreaM2"));
            False(element.Quantities.ContainsKey("MeasuredSolidVolumeM3"));
            Near(1d, element.Quantities["GrossVolumeM3"]);
            Near(1d, element.Quantities["NetVolumeM3"]);
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12d)
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void False(bool value)
        {
            if (value) throw new InvalidOperationException("Expected false.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class MeasuredSolidQuantityAtomicitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MeasuredSolidQuantityAtomicitySmoke.Run();
        }
    }
}
