using System;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ZoneDefinitionXmlPersistabilitySmoke
    {
        internal static void Run()
        {
            RejectsXmlInvalidIdentityAtPublicBoundary();
            FailedNameMutationPreservesPriorValue();
            SupplementaryUnicodeRoundTripsThroughQsdb();
        }

        private static void RejectsXmlInvalidIdentityAtPublicBoundary()
        {
            Throws<ArgumentException>(() => new ZoneDefinition("zone-\uD800", "Valid zone"));
            Throws<ArgumentException>(() => new ZoneDefinition("zone-valid", "Zone \uD800"));
        }

        private static void FailedNameMutationPreservesPriorValue()
        {
            var zone = new ZoneDefinition("zone-atomic", "Original zone");
            var before = zone.Name;

            Throws<ArgumentException>(() => zone.Name = "Invalid \uD800 zone");

            Require(zone.Name == before, "XML-invalid Zone name mutation changed the prior persisted value.");
        }

        private static void SupplementaryUnicodeRoundTripsThroughQsdb()
        {
            var zoneId = "zone-\U0001F9ED";
            var zoneName = "Khu \U0001F9ED";
            var project = new ProjectState("zone-xml-roundtrip", "Zone XML roundtrip");
            project.Zones.Add(new ZoneDefinition(zoneId, zoneName));

            var path = Path.Combine(Path.GetTempPath(), "qs3d-zone-xml-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                var roundTripped = loaded.FindZone(zoneId);

                Require(roundTripped != null, "Supplementary-Unicode Zone was not found after QSDB round-trip.");
                Require(roundTripped!.Id == zoneId, "Supplementary-Unicode Zone id changed across QSDB round-trip.");
                Require(roundTripped.Name == zoneName, "Supplementary-Unicode Zone name changed across QSDB round-trip.");
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Smoke cleanup must not mask the contract assertion result.
            }
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
