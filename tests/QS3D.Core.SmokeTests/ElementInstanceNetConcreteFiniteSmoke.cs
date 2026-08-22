using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceNetConcreteFiniteSmoke
    {
        internal static void Run()
        {
            PreservesFiniteArithmetic();
            FloorsOverDeductionToZero();
            PreservesMaximumFiniteGross();
            PreservesMaximumFiniteDeduction();
        }

        private static void PreservesFiniteArithmetic()
        {
            var element = NewElement();
            element.GrossConcreteM3 = 1.2d;
            element.DeductionM3 = 0.1d;
            Near(1.1d, element.NetConcreteM3, 1e-12, "normal finite net");
        }

        private static void FloorsOverDeductionToZero()
        {
            var element = NewElement();
            element.GrossConcreteM3 = 1d;
            element.DeductionM3 = 2d;
            Near(0d, element.NetConcreteM3, 0d, "over-deduction floors net to zero");
        }

        private static void PreservesMaximumFiniteGross()
        {
            var element = NewElement();
            element.GrossConcreteM3 = double.MaxValue;
            element.DeductionM3 = 0d;
            Near(double.MaxValue, element.NetConcreteM3, 0d, "maximum finite gross");
        }

        private static void PreservesMaximumFiniteDeduction()
        {
            var element = NewElement();
            element.GrossConcreteM3 = double.MaxValue;
            element.DeductionM3 = double.MaxValue;
            Near(0d, element.NetConcreteM3, 0d, "equal maximum finite values");
        }

        private static ElementInstance NewElement() =>
            new ElementInstance("NET-FINITE", new FamilyDefinition("Net finite", ElementCategory.ArchitecturalWall), "Nền 0.00");

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("ElementInstanceNetConcreteFiniteSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

    }

    internal static class ElementInstanceNetConcreteFiniteSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementInstanceNetConcreteFiniteSmoke.Run();
    }
}
