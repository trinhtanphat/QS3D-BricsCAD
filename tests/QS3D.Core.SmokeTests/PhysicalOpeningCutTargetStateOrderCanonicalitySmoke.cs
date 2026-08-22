using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetStateOrderCanonicalitySmoke
    {
        public static void Run()
        {
            var host = new ProjectElement("HOST-1", ElementCategory.StructuralWall);
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPENING-B", "OPENING-A" });

            True(PhysicalOpeningCutTargetStateCodec.TryRead(host, out var canonical));
            Equal(2, canonical.Count);
            Equal("OPENING-A", canonical[0]);
            Equal("OPENING-B", canonical[1]);

            var persisted = host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey];
            var tokens = persisted.Split(';');
            Equal(2, tokens.Length);
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = tokens[1] + ";" + tokens[0];

            Throws<InvalidOperationException>(() => PhysicalOpeningCutTargetStateCodec.TryRead(host, out _));
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
