using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetEmptyIdSmoke
    {
        public static void Run()
        {
            NormalizeRejectsWhitespaceTarget();
            WriteFailurePreservesExistingState();
            CanonicalTargetsRemainSortedAndUnique();
        }

        private static void NormalizeRejectsWhitespaceTarget()
        {
            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "OPEN-1", "   " }));
        }

        private static void WriteFailurePreservesExistingState()
        {
            var host = new ProjectElement("WALL-1", ElementCategory.ArchitecturalWall);
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPEN-1" });
            var before = host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey];

            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPEN-2", string.Empty }));

            var after = host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey];
            if (!string.Equals(before, after, StringComparison.Ordinal))
                throw new InvalidOperationException("Failed physical opening target write changed persisted target-state.");
        }

        private static void CanonicalTargetsRemainSortedAndUnique()
        {
            var ids = PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "OPEN-B", "open-a" });
            Equal(2, ids.Count);
            Equal("open-a", ids[0]);
            Equal("OPEN-B", ids[1]);

            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Normalize(new[] { " OPEN-B ", "open-a" }));

            Throws<InvalidOperationException>(() =>
                PhysicalOpeningCutTargetStateCodec.Normalize(new[] { "OPEN-1", "open-1" }));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
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

    internal static class PhysicalOpeningCutTargetEmptyIdSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PhysicalOpeningCutTargetEmptyIdSmoke.Run();
        }
    }
}
