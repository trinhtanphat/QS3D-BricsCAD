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
            var project = CreateProjectWithNullBacking();

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            AssertNullBacking(detached, "detached copy");

            var originalElement = project.Elements[0];
            var snapshot = ProjectStateSnapshot.Capture(project);
            MutateNullBacking(project);
            snapshot.Restore(project);

            if (!ReferenceEquals(originalElement, project.Elements[0]))
                throw new InvalidOperationException("rollback restore must preserve the captured ProjectElement object identity.");
            AssertNullBacking(project, "rollback restore");
        }

        private static ProjectState CreateProjectWithNullBacking()
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
            element.SourceHandles.Add(null!);
            element.DependsOn.Add(null!);
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

        private static void MutateNullBacking(ProjectState project)
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
            element.SourceHandles[0] = "AB12";
            element.DependsOn[0] = "E-HOST";
            element.Properties["NullProperty"] = "changed-property";

            var audit = project.AuditEvents[0];
            audit.Action = "changed-action";
            audit.ElementId = "changed-element";
            audit.Detail = "changed-detail";
            audit.Actor = "changed-actor";
            audit.CorrelationId = "changed-correlation";
        }

        private static void AssertNullBacking(ProjectState project, string label)
        {
            IsNull(project.DrawingPath, label + " project drawing path");
            IsNull(project.DrawingFingerprint, label + " project drawing fingerprint");
            IsNull(project.ActiveZoneId, label + " active zone id");
            IsNull(project.ActiveFloorId, label + " active floor id");
            IsNull(project.Metadata["NullMetadata"], label + " project metadata value");
            IsNull(project.Families[0].Properties["NullFamilyProperty"], label + " family property value");

            var element = project.Elements[0];
            IsNull(element.FamilyId, label + " element family id");
            IsNull(element.FloorId, label + " element floor id");
            IsNull(element.ZoneId, label + " element zone id");
            IsNull(element.DrawingFingerprint, label + " element drawing fingerprint");
            IsNull(element.SourceHandles[0], label + " source handle");
            IsNull(element.DependsOn[0], label + " dependency");
            IsNull(element.Properties["NullProperty"], label + " element property value");

            var audit = project.AuditEvents[0];
            IsNull(audit.Action, label + " audit action");
            IsNull(audit.ElementId, label + " audit element id");
            IsNull(audit.Detail, label + " audit detail");
            IsNull(audit.Actor, label + " audit actor");
            IsNull(audit.CorrelationId, label + " audit correlation id");
        }

        private static void IsNull(object? value, string label)
        {
            if (value != null)
                throw new InvalidOperationException(label + ": expected null backing state.");
        }
    }
}
