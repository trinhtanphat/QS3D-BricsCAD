using System;
using System.Runtime.CompilerServices;
using System.Threading;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectElementMarkCleanNoOpSmoke
    {
        internal static void Run()
        {
            FirstCleanAdvancesTimestamp();
            RepeatedCleanIsTimestampNoOp();
            PartialMultiFlagCleanRemainsMutation();
            NoneAndInvalidFlagsRemainNonMutating();
        }

        private static void FirstCleanAdvancesTimestamp()
        {
            var element = NewElement();
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.MarkClean(ElementDirtyFlags.Geometry);

            Require((element.Dirty & ElementDirtyFlags.Geometry) == 0, "first clean did not clear Geometry");
            Require(element.UpdatedUtc > before, "first clean did not advance UpdatedUtc");
        }

        private static void RepeatedCleanIsTimestampNoOp()
        {
            var element = NewElement();
            element.MarkClean(ElementDirtyFlags.Geometry);
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.MarkClean(ElementDirtyFlags.Geometry);

            Equal(expectedDirty, element.Dirty, "repeated clean changed Dirty");
            Equal(before, element.UpdatedUtc, "repeated clean changed UpdatedUtc");
        }

        private static void PartialMultiFlagCleanRemainsMutation()
        {
            var element = NewElement();
            element.MarkClean(ElementDirtyFlags.Geometry);
            var before = element.UpdatedUtc;
            Thread.Sleep(20);

            element.MarkClean(ElementDirtyFlags.Geometry | ElementDirtyFlags.Properties);

            Require((element.Dirty & ElementDirtyFlags.Properties) == 0, "partial multi-flag clean did not clear remaining Properties bit");
            Require(element.UpdatedUtc > before, "partial multi-flag clean did not advance UpdatedUtc");
        }

        private static void NoneAndInvalidFlagsRemainNonMutating()
        {
            var element = NewElement();
            var expectedDirty = element.Dirty;
            var before = element.UpdatedUtc;

            element.MarkClean(ElementDirtyFlags.None);
            Equal(expectedDirty, element.Dirty, "None clean changed Dirty");
            Equal(before, element.UpdatedUtc, "None clean changed UpdatedUtc");

            Throws<ArgumentOutOfRangeException>(() => element.MarkClean((ElementDirtyFlags)16));
            Equal(expectedDirty, element.Dirty, "invalid clean changed Dirty");
            Equal(before, element.UpdatedUtc, "invalid clean changed UpdatedUtc");
        }

        private static ProjectElement NewElement() =>
            new ProjectElement("E-CLEAN-NOOP", ElementCategory.Beam);

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("ProjectElementMarkCleanNoOpSmoke: " + message + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectElementMarkCleanNoOpSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }

    internal static class ProjectElementMarkCleanNoOpSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectElementMarkCleanNoOpSmoke.Run();
    }
}
