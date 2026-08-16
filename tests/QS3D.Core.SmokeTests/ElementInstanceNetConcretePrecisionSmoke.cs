using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ElementInstanceNetConcretePrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SwallowedPositiveDeductionFailsClosed();
            ZeroDeductionPreservesGross();
            OrdinarySubtractionRemainsStable();
            EqualAndExcessDeductionStillClampToZero();
        }

        private static void SwallowedPositiveDeductionFailsClosed()
        {
            var element = Element();
            element.GrossConcreteM3 = 1e16d;
            element.DeductionM3 = 1d;
            Capture<OverflowException>(() => ReadNet(element));
        }

        private static void ZeroDeductionPreservesGross()
        {
            var element = Element();
            element.GrossConcreteM3 = 1e16d;
            element.DeductionM3 = 0d;
            Assert(element.NetConcreteM3.Equals(1e16d), "Zero concrete deduction must preserve gross volume.");
        }

        private static void OrdinarySubtractionRemainsStable()
        {
            var element = Element();
            element.GrossConcreteM3 = 10d;
            element.DeductionM3 = 2d;
            Assert(element.NetConcreteM3.Equals(8d), "Ordinary net concrete subtraction changed unexpectedly.");
        }

        private static void EqualAndExcessDeductionStillClampToZero()
        {
            var element = Element();
            element.GrossConcreteM3 = 10d;
            element.DeductionM3 = 10d;
            Assert(element.NetConcreteM3.Equals(0d), "Equal gross/deduction must remain zero.");

            element.DeductionM3 = 12d;
            Assert(element.NetConcreteM3.Equals(0d), "Excess deduction must preserve zero-clamp semantics.");
        }

        private static ElementInstance Element() =>
            new ElementInstance(
                "NET-CONCRETE-PRECISION",
                new FamilyDefinition("Concrete family", ElementCategory.Column),
                "L1");

        private static void ReadNet(ElementInstance element) => _ = element.NetConcreteM3;

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
