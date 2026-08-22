using System;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementQuantityNoOpSmoke
    {
        internal static void Run()
        {
            NewQuantityAdvancesTimestamp();
            SameValueAliasIsTimestampNoOp();
            ChangedValueStillAdvancesTimestamp();
            NonFiniteValuesRemainNonMutating();
        }

        private static void NewQuantityAdvancesTimestamp()
        {
            var element = NewElement();
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.SetQuantity("NetVolumeM3", 2.5d);

            Require(element.UpdatedUtc > before, "new quantity did not advance UpdatedUtc");
            Equal(2.5d, element.Quantities["NetVolumeM3"], "new quantity value");
        }

        private static void SameValueAliasIsTimestampNoOp()
        {
            var element = NewElement();
            element.SetQuantity("NetVolumeM3", 2.5d);
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.SetQuantity(" netvolumem3 ", 2.5d);

            Equal(before, element.UpdatedUtc, "same-value alias changed UpdatedUtc");
            Equal(1, element.Quantities.Count, "same-value alias created a second quantity key");
            Equal(2.5d, element.Quantities["NETVOLUMEM3"], "same-value alias changed the stored value");
        }

        private static void ChangedValueStillAdvancesTimestamp()
        {
            var element = NewElement();
            element.SetQuantity("NetVolumeM3", 2.5d);
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.SetQuantity("NETVOLUMEM3", 3.5d);

            Require(element.UpdatedUtc > before, "changed quantity did not advance UpdatedUtc");
            Equal(1, element.Quantities.Count, "changed alias created a second quantity key");
            Equal(3.5d, element.Quantities["NetVolumeM3"], "changed quantity value");
        }

        private static void NonFiniteValuesRemainNonMutating()
        {
            var element = NewElement();
            element.SetQuantity("NetVolumeM3", 2.5d);
            var before = element.UpdatedUtc;
            var count = element.Quantities.Count;

            Throws<ArgumentOutOfRangeException>(() => element.SetQuantity("NetVolumeM3", double.NaN));
            Throws<ArgumentOutOfRangeException>(() => element.SetQuantity("NetVolumeM3", double.PositiveInfinity));

            Equal(before, element.UpdatedUtc, "rejected non-finite quantity changed UpdatedUtc");
            Equal(count, element.Quantities.Count, "rejected non-finite quantity changed dictionary cardinality");
            Equal(2.5d, element.Quantities["NetVolumeM3"], "rejected non-finite quantity changed stored value");
        }

        private static ProjectElement NewElement() =>
            new ProjectElement("E-Q-NOOP", ElementCategory.Beam);

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("ProjectElementQuantityNoOpSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectElementQuantityNoOpSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProjectElementQuantityNoOpSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementQuantityNoOpSmoke.Run();
    }
}
