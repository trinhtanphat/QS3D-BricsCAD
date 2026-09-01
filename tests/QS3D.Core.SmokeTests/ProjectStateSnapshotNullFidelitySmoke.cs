using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotNullFidelitySmoke
    {
        internal static void Run()
        {
            var project = CreateProjectWithCanonicalAndNullBacking();
            AssertExpectedBacking(project, "source fixture");

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            AssertExpectedBacking(detached, "detached copy");

            var originalElement = project.Elements[0];
            var snapshot = ProjectStateSnapshot.Capture(project);
            MutateBacking(project);
            snapshot.Restore(project);

            if (!ReferenceEquals(originalElement, project.Elements[0]))
                throw new InvalidOperationException("rollback restore must preserve the captured ProjectElement object identity.");
            AssertExpectedBacking(project, "rollback restore");
        }

        private static ProjectState CreateProjectWithCanonicalAndNullBacking()
        {
            var project = new ProjectState("P-SNAPSHOT-NULL", "Snapshot Null Fidelity")
            {
                DrawingPath = null!,
                DrawingFingerprint = null!,
                ActiveZoneId = null!,
                ActiveFloorId = null!
            };
            project.Metadata["NullMetadata"] = null!;

            var family = new ProjectFamily("F-1", "Family", ElementCategory.Beam);
            family.Properties["NullFamilyProperty"] = null!;
            project.Families.Add(family);

            var element = new ProjectElement("E-1", ElementCategory.Beam)
            {
                FamilyId = null!,
                FloorId = null!,
                ZoneId = null!,
                DrawingFingerprint = null!
            };
            element.SourceHandles.Add("AB12");
            element.DependsOn.Add("E-BASE");
            element.Properties["NullProperty"] = null!;
            project.Elements.Add(element);

            project.AuditEvents.Add(new AuditEvent
            {
                Utc = DateTime.UtcNow,
                Action = null!,
                ElementId = null!,
                Detail = null!,
                Actor = null!,
                CorrelationId = null!
            });

            return project;
        }

        private static void MutateBacking(ProjectState project)
        {
            project.DrawingPath = "changed-path";
            project.DrawingFingerprint = "changed-fingerprint";
            project.ActiveZoneId = "changed-zone";
            project.ActiveFloorId = "changed-floor";
            project.Metadata["NullMetadata"] = "changed-metadata";
            project.Families[0].Properties["NullFamilyProperty"] = "changed-family-property";

            var element = project.Elements[0];
            element.FamilyId = "changed-family";
            element.FloorId = "changed-floor";
            element.ZoneId = "changed-zone";
            element.DrawingFingerprint = "changed-element-fingerprint";
            element.SourceHandles[0] = "CD34";
            element.DependsOn[0] = "E-HOST";
            element.Properties["NullProperty"] = "changed-property";

            var audit = project.AuditEvents[0];
            audit.Action = "changed-action";
            audit.ElementId = "changed-element";
            audit.Detail = "changed-detail";
            audit.Actor = "changed-actor";
            audit.CorrelationId = "changed-correlation";
        }

        private static void AssertExpectedBacking(ProjectState project, string label)
        {
            IsEmpty(project.DrawingPath, label + " project drawing path");
            IsEmpty(project.DrawingFingerprint, label + " project drawing fingerprint");
            IsEmpty(project.ActiveZoneId, label + " active zone id");
            IsEmpty(project.ActiveFloorId, label + " active floor id");
            IsEmpty(project.Metadata["NullMetadata"], label + " project metadata value");
            IsNull(project.Families[0].Properties["NullFamilyProperty"], label + " family property value");

            var element = project.Elements[0];
            IsEmpty(element.FamilyId, label + " element family id");
            IsEmpty(element.FloorId, label + " element floor id");
            IsEmpty(element.ZoneId, label + " element zone id");
            IsEmpty(element.DrawingFingerprint, label + " element drawing fingerprint");
            IsEqual("AB12", element.SourceHandles[0], label + " source handle");
            IsEqual("E-BASE", element.DependsOn[0], label + " dependency");
            IsNull(element.Properties["NullProperty"], label + " element property value");

            var audit = project.AuditEvents[0];
            IsNull(audit.Action, label + " audit action");
            IsNull(audit.ElementId, label + " audit element id");
            IsNull(audit.Detail, label + " audit detail");
            IsNull(audit.Actor, label + " audit actor");
            IsNull(audit.CorrelationId, label + " audit correlation id");
        }

        private static void IsEmpty(string? value, string label)
        {
            if (!string.Equals(value, string.Empty, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected canonical empty-string backing state.");
        }

        private static void IsEqual(string expected, string? value, string label)
        {
            if (!string.Equals(value, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(label + ": expected canonical relation identity '" + expected + "'.");
        }

        private static void IsNull(object? value, string label)
        {
            if (value != null)
                throw new InvalidOperationException(label + ": expected null backing state.");
        }
    }
}
