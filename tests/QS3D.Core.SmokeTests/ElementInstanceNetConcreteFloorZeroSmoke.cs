using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceNetConcreteFloorZeroSmoke
    {
        internal static void Run()
        {
            var family = new FamilyDefinition("Concrete", ElementCategory.StructuralWall);
            var element = new ElementInstance("E1", family, "F1")
            {
                GrossConcreteM3 = 10d,
                DeductionM3 = 3d
            };

            Equal(7d, element.NetConcreteM3, "normal net concrete");

            element.DeductionM3 = 10d;
            Equal(0d, element.NetConcreteM3, "equal deduction");

            element.DeductionM3 = 12d;
            Equal(0d, element.NetConcreteM3, "over deduction floor zero");

            element.GrossConcreteM3 = double.MaxValue;
            element.DeductionM3 = double.MaxValue;
            Equal(0d, element.NetConcreteM3, "maximum finite equality");

            Throws<ArgumentOutOfRangeException>(() => element.GrossConcreteM3 = double.NaN, "gross NaN guard");
            Throws<ArgumentOutOfRangeException>(() => element.DeductionM3 = double.PositiveInfinity, "deduction infinity guard");
            Throws<ArgumentOutOfRangeException>(() => element.DeductionM3 = -1d, "deduction negative guard");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (!expected.Equals(actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
