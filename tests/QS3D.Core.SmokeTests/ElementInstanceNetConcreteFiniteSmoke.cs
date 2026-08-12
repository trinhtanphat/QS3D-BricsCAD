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
            PreservesNegativeFiniteArithmetic();
            RejectsPositiveOverflow();
            RejectsNegativeOverflow();
        }

        private static void PreservesFiniteArithmetic()
        {
            var element = NewElement();
            element.GrossConcreteM3 = 1.2d;
            element.DeductionM3 = 0.1d;
            Near(1.1d, element.NetConcreteM3, 1e-12, "normal finite net");
        }

        private static void PreservesNegativeFiniteArithmetic()
        {
            var element = NewElement();
            element.GrossConcreteM3 = 1d;
            element.DeductionM3 = 2d;
            Near(-1d, element.NetConcreteM3, 0d, "negative finite net");
        }

        private static void RejectsPositiveOverflow()
        {
            var element = NewElement();
            element.GrossConcreteM3 = double.MaxValue;
            element.DeductionM3 = -double.MaxValue;
            Throws<OverflowException>(() => _ = element.NetConcreteM3, "positive overflow");
        }

        private static void RejectsNegativeOverflow()
        {
            var element = NewElement();
            element.GrossConcreteM3 = -double.MaxValue;
            element.DeductionM3 = double.MaxValue;
            Throws<OverflowException>(() => _ = element.NetConcreteM3, "negative overflow");
        }

        private static ElementInstance NewElement() =>
            new ElementInstance("NET-FINITE", new FamilyDefinition("Net finite", ElementCategory.ArchitecturalWall), "Nền 0.00");

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("ElementInstanceNetConcreteFiniteSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ElementInstanceNetConcreteFiniteSmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }

    internal static class ElementInstanceNetConcreteFiniteSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ElementInstanceNetConcreteFiniteSmoke.Run();
    }
}
