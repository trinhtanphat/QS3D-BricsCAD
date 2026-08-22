using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class PhysicalOpeningCutTargetStateSplitBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            MaximumTargetCountStillRoundTrips();
            OversizedPersistedTokenCountFailsClosed();
            OrdinaryTargetStateStillRoundTrips();
        }

        private static void MaximumTargetCountStillRoundTrips()
        {
            var host = new ProjectElement("HOST-MAX", ElementCategory.StructuralWall);
            var expected = Enumerable.Range(0, 4096).Select(x => "O" + x.ToString("D4")).ToArray();
            PhysicalOpeningCutTargetStateCodec.Write(host, expected);

            if (!PhysicalOpeningCutTargetStateCodec.TryRead(host, out var actual) || actual.Count != expected.Length)
                throw new InvalidOperationException("Maximum supported physical opening target count no longer round-trips.");
            for (var index = 0; index < expected.Length; index++)
                if (!string.Equals(actual[index], expected[index], StringComparison.Ordinal))
                    throw new InvalidOperationException("Physical opening target-state ordering changed at the maximum supported count.");
        }

        private static void OversizedPersistedTokenCountFailsClosed()
        {
            var host = new ProjectElement("HOST-OVER", ElementCategory.StructuralWall);
            host.Properties[PhysicalOpeningCutTargetStateCodec.OpeningIdsKey] = string.Join(";", Enumerable.Repeat("QQ==", 4097));
            try
            {
                PhysicalOpeningCutTargetStateCodec.TryRead(host, out _);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("too many physical opening targets", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Oversized target-state failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("A persisted physical opening target-state above the 4096-token limit was accepted.");
        }

        private static void OrdinaryTargetStateStillRoundTrips()
        {
            var host = new ProjectElement("HOST-TWO", ElementCategory.StructuralWall);
            PhysicalOpeningCutTargetStateCodec.Write(host, new[] { "OPEN-A", "OPEN-B" });
            if (!PhysicalOpeningCutTargetStateCodec.TryRead(host, out var ids) || ids.Count != 2 ||
                !string.Equals(ids[0], "OPEN-A", StringComparison.Ordinal) ||
                !string.Equals(ids[1], "OPEN-B", StringComparison.Ordinal))
                throw new InvalidOperationException("Ordinary physical opening target-state roundtrip changed while bounding split allocation.");
        }
    }
}
