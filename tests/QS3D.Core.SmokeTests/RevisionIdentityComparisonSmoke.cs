using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionIdentityComparisonSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            CaseOnlyReferenceChangesAreEquivalent();
            RealFamilyReferenceChangeIsReported();
            RealFloorReferenceChangeIsReported();
            RealZoneReferenceChangeIsReported();
        }

        private static void CaseOnlyReferenceChangesAreEquivalent()
        {
            var before = Snapshot("FAMILY-A", "FLOOR-A", "ZONE-A");
            var after = Snapshot("family-a", "floor-a", "zone-a");
            Equal(0, new RevisionService().Compare(before, after).Count);
        }

        private static void RealFamilyReferenceChangeIsReported()
        {
            AssertSingleField(Snapshot("FAMILY-A", "FLOOR-A", "ZONE-A"), Snapshot("FAMILY-B", "FLOOR-A", "ZONE-A"), "FamilyId");
        }

        private static void RealFloorReferenceChangeIsReported()
        {
            AssertSingleField(Snapshot("FAMILY-A", "FLOOR-A", "ZONE-A"), Snapshot("FAMILY-A", "FLOOR-B", "ZONE-A"), "FloorId");
        }

        private static void RealZoneReferenceChangeIsReported()
        {
            AssertSingleField(Snapshot("FAMILY-A", "FLOOR-A", "ZONE-A"), Snapshot("FAMILY-A", "FLOOR-A", "ZONE-B"), "ZoneId");
        }

        private static void AssertSingleField(RevisionSnapshot before, RevisionSnapshot after, string expectedField)
        {
            var deltas = new RevisionService().Compare(before, after);
            Equal(1, deltas.Count);
            Equal("Changed", deltas[0].Change);
            Equal(1, deltas[0].Fields.Count);
            Equal(expectedField, deltas[0].Fields.Single().Field);
        }

        private static RevisionSnapshot Snapshot(string familyId, string floorId, string zoneId)
        {
            var snapshot = new RevisionSnapshot { Id = "r", CreatedUtc = DateTime.UtcNow };
            snapshot.Elements.Add(new RevisionElementSnapshot
            {
                ElementId = "E1",
                Category = ElementCategory.Beam.ToString(),
                FamilyId = familyId,
                FloorId = floorId,
                ZoneId = zoneId
            });
            return snapshot;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("RevisionIdentityComparisonSmoke expected " + expected + ", got " + actual + ".");
        }
    }
}
