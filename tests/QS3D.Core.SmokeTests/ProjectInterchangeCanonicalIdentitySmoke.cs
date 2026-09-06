using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeCanonicalIdentitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedProjectId();
            RejectsPaddedRelationId();
            RejectsPaddedDependency();
            RejectsPaddedSourceHandle();
            RejectsPaddedPropertyKey();
            RejectsTimestampWithoutOffset();
            RejectsTimestampWithExplicitOffset();
            AcceptsCanonicalUtcDeterministically();
        }

        private static void RejectsPaddedProjectId()
        {
            var json = Json().Replace("\"id\":\"P-CANON\"", "\"id\":\" P-CANON \"");
            Reject(json);
        }

        private static void RejectsPaddedRelationId()
        {
            var json = Json().Replace("\"familyId\":\"FAM-1\"", "\"familyId\":\" FAM-1 \"");
            Reject(json);
        }

        private static void RejectsPaddedDependency()
        {
            var json = Json().Replace("\"dependencies\": [\"E-ROOT\"]", "\"dependencies\": [\" E-ROOT \"]");
            Reject(json);
        }

        private static void RejectsPaddedSourceHandle()
        {
            var json = Json().Replace("\"sourceHandles\": [\"1A2B\"]", "\"sourceHandles\": [\" 1A2B \"]");
            Reject(json);
        }

        private static void RejectsPaddedPropertyKey()
        {
            var json = Json().Replace("\"Mark\":\"B-01\"", "\" Mark \":\"B-01\"");
            Reject(json);
        }

        private static void RejectsTimestampWithoutOffset()
        {
            var json = Json().Replace("2026-08-10T10:11:12.0000000Z", "2026-08-10T10:11:12.0000000");
            Reject(json);
        }

        private static void RejectsTimestampWithExplicitOffset()
        {
            var json = Json().Replace("2026-08-10T10:11:12.0000000Z", "2026-08-10T17:11:12.0000000+07:00");
            Reject(json);
        }

        private static void AcceptsCanonicalUtcDeterministically()
        {
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(Json());
            var expected = new DateTime(2026, 8, 10, 10, 11, 12, DateTimeKind.Utc);
            if (!snapshot.Project.UpdatedUtc.HasValue || snapshot.Project.UpdatedUtc.Value != expected || snapshot.Project.UpdatedUtc.Value.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("ProjectInterchangeCanonicalIdentitySmoke: canonical UTC timestamp did not round-trip deterministically.");
        }

        private static void Reject(string json)
        {
            try { ProjectInterchangeValidatedSnapshotReader.Read(json); }
            catch (InvalidDataException) { return; }
            throw new InvalidOperationException("ProjectInterchangeCanonicalIdentitySmoke: non-canonical snapshot identity was accepted.");
        }

        private static string Json()
        {
            var project = new ProjectState("P-CANON", "Canonical")
            {
                UpdatedUtc = new DateTime(2026, 8, 10, 10, 11, 12, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z-1", "Zone"));
            project.Floors.Add(new FloorDefinition("F-1", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM-1", "Family", ElementCategory.Beam));

            var root = new ProjectElement("E-ROOT", ElementCategory.Beam, "FAM-1", "F-1", "Z-1");
            root.SourceHandles.Add("100");
            root.SetProperty("Mark", "ROOT");
            project.Elements.Add(root);

            var child = new ProjectElement("E-CHILD", ElementCategory.Beam, "FAM-1", "F-1", "Z-1");
            child.SourceHandles.Add("1A2B");
            child.DependsOn.Add(root.Id);
            child.SetProperty("Mark", "B-01");
            project.Elements.Add(child);
            project.UpdatedUtc = new DateTime(2026, 8, 10, 10, 11, 12, DateTimeKind.Utc);
            return ProjectInterchangeJsonExporter.Build(project);
        }
    }
}
