using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotNullCollectionIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsNullZoneEntry();
            RejectsNullFamilyEntry();
            RejectsNullAuditEntry();
            PreservesCanonicalDetachedCopyIsolation();
        }

        private static void RejectsNullZoneEntry()
        {
            var project = new ProjectState("SNAP-NULL-ZONE", "Snapshot null Zone");
            project.Zones.Add(null!);
            ThrowsExact(
                () => ProjectStateSnapshot.CreateDetachedCopy(project),
                "Cannot snapshot a project containing a null zone entry at index 0.");
        }

        private static void RejectsNullFamilyEntry()
        {
            var project = new ProjectState("SNAP-NULL-FAMILY", "Snapshot null Family");
            project.Families.Add(new ProjectFamily("F1", "Family 1", ElementCategory.Beam));
            project.Families.Add(null!);
            ThrowsExact(
                () => ProjectStateSnapshot.CreateDetachedCopy(project),
                "Cannot snapshot a project containing a null family entry at index 1.");
        }

        private static void RejectsNullAuditEntry()
        {
            var project = new ProjectState("SNAP-NULL-AUDIT", "Snapshot null Audit");
            project.AuditEvents.Add(new AuditEvent { Utc = DateTime.UtcNow, Action = "test" });
            project.AuditEvents.Add(null!);
            ThrowsExact(
                () => ProjectStateSnapshot.CreateDetachedCopy(project),
                "Cannot snapshot a project containing a null audit event entry at index 1.");
        }

        private static void PreservesCanonicalDetachedCopyIsolation()
        {
            var project = new ProjectState("SNAP-VALID", "Snapshot valid");
            var zone = new ZoneDefinition("Z1", "Zone 1");
            var family = new ProjectFamily("F1", "Family 1", ElementCategory.Beam);
            family.Properties["Material"] = "Steel";
            var element = new ProjectElement("E1", ElementCategory.Beam, family.Id, string.Empty, zone.Id);
            element.Properties["Width"] = "0.3";
            project.Zones.Add(zone);
            project.Families.Add(family);
            project.Elements.Add(element);

            var copy = ProjectStateSnapshot.CreateDetachedCopy(project);
            if (ReferenceEquals(copy, project) ||
                ReferenceEquals(copy.Zones[0], zone) ||
                ReferenceEquals(copy.Families[0], family) ||
                ReferenceEquals(copy.Elements[0], element))
                throw new InvalidOperationException("Detached snapshot must not share mutable semantic objects with its source project.");
            if (!string.Equals(copy.Elements[0].FamilyId, "F1", StringComparison.Ordinal) ||
                !string.Equals(copy.Elements[0].ZoneId, "Z1", StringComparison.Ordinal) ||
                !string.Equals(copy.Families[0].Properties["Material"], "Steel", StringComparison.Ordinal))
                throw new InvalidOperationException("Detached snapshot must preserve canonical semantic content.");
        }

        private static void ThrowsExact(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, expectedMessage, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Snapshot null-entry guard returned an unexpected error message.", ex);
            }
            throw new InvalidOperationException("Snapshot null-entry guard did not reject malformed state.");
        }
    }
}
