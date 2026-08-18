using System;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetStateCanonicalReadSmoke
    {
        internal static void Run()
        {
            WriterRoundtripRemainsValid();
            RejectsPaddedEncodedToken();
            RejectsPaddedDecodedId();
            RejectsWhitespaceInsideBase64();
        }

        private static void WriterRoundtripRemainsValid()
        {
            var host = NewHost();
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPEN-B", "open-a" });
            if (!PhysicalOpeningCutTargetStateCodec.TryRead(host, out var ids))
                throw new Exception("Expected canonical physical opening target-state to be present.");
            Equal(2, ids.Count);
            Equal("open-a", ids[0]);
            Equal("OPEN-B", ids[1]);
        }

        private static void RejectsPaddedEncodedToken()
        {
            var host = NewHost();
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPEN-1" });
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] =
                " " + host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey];
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.TryRead(host, out _));
        }

        private static void RejectsPaddedDecodedId()
        {
            var host = NewHost();
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] =
                Convert.ToBase64String(Encoding.UTF8.GetBytes(" OPEN-1 "));
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.TryRead(host, out _));
        }

        private static void RejectsWhitespaceInsideBase64()
        {
            var host = NewHost();
            var canonical = Convert.ToBase64String(Encoding.UTF8.GetBytes("OPEN-1"));
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] =
                canonical.Insert(2, " ");
            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.TryRead(host, out _));
        }

        private static ProjectElement NewHost() =>
            new ProjectElement("WALL-1", ElementCategory.ArchitecturalWall);

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class PhysicalOpeningCutTargetStateCanonicalReadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => PhysicalOpeningCutTargetStateCanonicalReadSmoke.Run();
    }
}
