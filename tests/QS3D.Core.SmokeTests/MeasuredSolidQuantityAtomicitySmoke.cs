using System;
using System.Runtime.CompilerServices;
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
