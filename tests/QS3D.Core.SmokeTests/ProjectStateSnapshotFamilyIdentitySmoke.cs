using System;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotFamilyIdentitySmoke
    {
        public static void Run()
        {
            RestorePreservesCapturedFamilyIdentity();
            DetachedCopyNeverAliasesCanonicalFamilies();
            ForeignTargetRestoreNeverAliasesCapturedFamilies();
            RejectsNonCanonicalElementRelationIdentities();
            PreservesRepairableDuplicateRelations();
            PreservesCanonicalUnicodeElementRelations();
            RejectsInvalidDirectAuditHistory();
            PreservesCanonicalUnicodeAuditHistory();
        }

        private static void RestorePreservesCapturedFamilyIdentity()
        {
            var project = new ProjectState("snapshot-family-identity", "Snapshot family identity");
            var first = new ProjectFamily("F1", "Before", ElementCategory.ArchitecturalWall);
            first.Properties["Material"] = "Concrete";
            var second = new ProjectFamily("F2", "Second", ElementCategory.Slab);
            second.Properties["ThicknessM"] = "0.2";
            project.Families.Add(first);
            project.Families.Add(second);
            project.Touch();

            var propertyChangedCount = 0;
            first.PropertyChanged += (_, __) => propertyChangedCount++;
            var projectUpdatedUtc = project.UpdatedUtc;
            var projectChangeVersion = project.ChangeVersion;
            var rollback = ProjectStateSnapshot.Capture(project);

            first.Name = "After";
            first.Category = ElementCategory.Beam;
            first.Properties.Clear();
            first.Properties["Transient"] = "after";

            second.Name = "Removed-mutated";
            second.Properties["Transient"] = "removed-after-capture";
            project.Families.Remove(second);

            var added = new ProjectFamily("F3", "Added", ElementCategory.Column);
            added.Properties["Transient"] = "post-capture";
            project.Families.Insert(0, added);
            project.Touch();

            propertyChangedCount = 0;
            rollback.Restore(project);

            Require(project.Families.Count == 2, "Rollback did not restore the captured family count.");
            Require(ReferenceEquals(project.Families[0], first), "Rollback replaced the first captured canonical ProjectFamily reference.");
            Require(ReferenceEquals(project.Families[1], second), "Rollback did not reinsert the removed captured ProjectFamily reference.");
            Require(ReferenceEquals(project.FindFamily("F1"), first), "FindFamily(F1) no longer returns the pre-transaction canonical object after rollback.");
            Require(ReferenceEquals(project.FindFamily("F2"), second), "FindFamily(F2) no longer returns the removed pre-transaction canonical object after rollback.");
            Require(project.FindFamily("F3") == null, "Rollback retained a family created after snapshot capture.");

            Require(first.Name == "Before", "Rollback did not restore the first family name.");
            Require(first.Category == ElementCategory.ArchitecturalWall, "Rollback did not restore the first family category.");
            Require(first.Properties.Count == 1 && first.Properties["Material"] == "Concrete", "Rollback did not restore the first family properties exactly.");
            Require(second.Name == "Second", "Rollback did not restore the removed family name.");
            Require(second.Category == ElementCategory.Slab, "Rollback did not restore the removed family category.");
            Require(second.Properties.Count == 1 && second.Properties["ThicknessM"] == "0.2", "Rollback did not restore the removed family properties exactly.");
            Require(project.ChangeVersion == projectChangeVersion, "Rollback did not restore project ChangeVersion.");
            Require(project.UpdatedUtc == projectUpdatedUtc, "Rollback did not restore project UpdatedUtc.");
            Require(propertyChangedCount >= 2, "Rollback did not notify existing ProjectFamily subscribers while restoring changed Name/Category.");

            var afterRollbackEvents = propertyChangedCount;
            first.Name = "AfterRollback";
            Require(propertyChangedCount == afterRollbackEvents + 1, "The pre-transaction ProjectFamily subscription was detached from the canonical family after rollback.");
        }

        private static void DetachedCopyNeverAliasesCanonicalFamilies()
        {
            var project = new ProjectState("snapshot-family-detached", "Snapshot family detached");
            var family = new ProjectFamily("F1", "Canonical", ElementCategory.Room);
            family.Properties["Material"] = "CanonicalMaterial";
            project.Families.Add(family);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedFamily = detached.FindFamily("F1") ?? throw new Exception("Detached copy lost F1.");

            Require(!ReferenceEquals(detached, project), "CreateDetachedCopy returned the canonical ProjectState.");
            Require(!ReferenceEquals(detachedFamily, family), "CreateDetachedCopy aliased the canonical ProjectFamily.");

            detachedFamily.Name = "Detached";
            detachedFamily.Category = ElementCategory.Beam;
            detachedFamily.Properties["Material"] = "DetachedMaterial";
            Require(family.Name == "Canonical", "Mutating a detached family changed the canonical name.");
            Require(family.Category == ElementCategory.Room, "Mutating a detached family changed the canonical category.");
            Require(family.Properties["Material"] == "CanonicalMaterial", "Mutating a detached family changed canonical properties.");
        }

        private static void ForeignTargetRestoreNeverAliasesCapturedFamilies()
        {
            var source = new ProjectState("snapshot-family-foreign", "Source");
            var captured = new ProjectFamily("F1", "Source Family", ElementCategory.ArchitecturalWall);
            captured.Properties["Material"] = "SourceMaterial";
            source.Families.Add(captured);
            var rollback = ProjectStateSnapshot.Capture(source);

            var target = new ProjectState("snapshot-family-foreign", "Target");
            target.Families.Add(new ProjectFamily("OLD", "Old", ElementCategory.Beam));
            rollback.Restore(target);

            var restored = target.FindFamily("F1") ?? throw new Exception("Foreign target restore lost F1.");
            Require(!ReferenceEquals(restored, captured), "Restoring into a foreign same-id ProjectState aliased the captured canonical family.");
            restored.Name = "Target Mutation";
            restored.Properties["Material"] = "TargetMaterial";
            Require(captured.Name == "Source Family", "Foreign target family mutation changed the captured source family name.");
            Require(captured.Properties["Material"] == "SourceMaterial", "Foreign target family mutation changed captured source family properties.");
        }

        private static void RejectsNonCanonicalElementRelationIdentities()
        {
            ExpectRejectedRelation(true, " A1 ", "padded source handle");
            ExpectRejectedRelation(true, "   ", "blank source handle");
            ExpectRejectedRelation(true, "A\t1", "control-bearing source handle");
            ExpectRejectedRelation(true, "A\uD8001", "malformed source handle");

            ExpectRejectedRelation(false, " HOST ", "padded dependency");
            ExpectRejectedRelation(false, "\t", "blank dependency");
            ExpectRejectedRelation(false, "HOST\n1", "control-bearing dependency");
            ExpectRejectedRelation(false, "HOST\uD800", "malformed dependency");
        }

        private static void ExpectRejectedRelation(bool sourceHandle, string value, string label)
        {
            var project = NewRelationProject(label);
            var element = project.Elements[0];
            var values = sourceHandle ? element.SourceHandles : element.DependsOn;
            values.Add(value);
            var originalDirty = element.Dirty;
            var originalUpdatedUtc = element.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;
            var originalProjectUpdatedUtc = project.UpdatedUtc;

            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " was accepted by detached copy.");

            Require(values.Count == 1 && string.Equals(values[0], value, StringComparison.Ordinal), label + " rejection mutated relation source state.");
            Require(element.Dirty == originalDirty && element.UpdatedUtc == originalUpdatedUtc, label + " rejection changed element persistence state.");
            Require(project.ChangeVersion == originalChangeVersion && project.UpdatedUtc == originalProjectUpdatedUtc, label + " rejection changed project persistence state.");
        }

        private static void PreservesRepairableDuplicateRelations()
        {
            var project = NewRelationProject("repairable-duplicates");
            var element = project.Elements[0];
            element.SourceHandles.Add("A1");
            element.SourceHandles.Add("a1");
            element.DependsOn.Add("HOST");
            element.DependsOn.Add("host");

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copy = detached.FindElement("E1") ?? throw new Exception("Detached snapshot lost the repairable duplicate fixture element.");
            Require(copy.SourceHandles.Count == 2 && copy.SourceHandles[0] == "A1" && copy.SourceHandles[1] == "a1", "Detached snapshot changed repairable duplicate source handles.");
            Require(copy.DependsOn.Count == 2 && copy.DependsOn[0] == "HOST" && copy.DependsOn[1] == "host", "Detached snapshot changed repairable duplicate dependencies.");
        }

        private static void PreservesCanonicalUnicodeElementRelations()
        {
            var project = NewRelationProject("canonical-unicode");
            var element = project.Elements[0];
            const string handle = "HANDLE-\U0001F680";
            const string dependency = "HOST-\U0001F680";
            element.SourceHandles.Add(handle);
            element.DependsOn.Add(dependency);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copy = detached.FindElement("E1") ?? throw new Exception("Detached snapshot lost the canonical relation fixture element.");
            Require(copy.SourceHandles.Count == 1 && string.Equals(copy.SourceHandles[0], handle, StringComparison.Ordinal), "Detached snapshot changed canonical source-handle text.");
            Require(copy.DependsOn.Count == 1 && string.Equals(copy.DependsOn[0], dependency, StringComparison.Ordinal), "Detached snapshot changed canonical dependency text.");
        }

        private static void RejectsInvalidDirectAuditHistory()
        {
            ExpectRejectedAudit(
                new AuditEvent { Utc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local), Action = "Changed" },
                "non-UTC audit timestamp");
            ExpectRejectedAudit(
                new AuditEvent { Utc = DateTime.UtcNow, Action = " Changed " },
                "padded audit action");
            ExpectRejectedAudit(
                new AuditEvent { Utc = DateTime.UtcNow, Action = "Changed", ElementId = " E1 " },
                "padded audit element id");
            ExpectRejectedAudit(
                new AuditEvent { Utc = DateTime.UtcNow, Action = "Changed", Detail = "bad\uD800" },
                "XML-invalid audit detail");
            ExpectRejectedAudit(
                new AuditEvent { Utc = DateTime.UtcNow, Action = "Changed", CorrelationId = " C1 " },
                "padded audit correlation id");
        }

        private static void ExpectRejectedAudit(AuditEvent audit, string label)
        {
            var project = new ProjectState("snapshot-audit-invalid", "Snapshot audit invalid");
            project.AuditEvents.Add(audit);
            var originalUpdatedUtc = project.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;

            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " was accepted by detached copy.");

            Require(project.AuditEvents.Count == 1 && ReferenceEquals(project.AuditEvents[0], audit), label + " rejection mutated source audit history.");
            Require(project.UpdatedUtc == originalUpdatedUtc && project.ChangeVersion == originalChangeVersion, label + " rejection changed project persistence state.");
        }

        private static void PreservesCanonicalUnicodeAuditHistory()
        {
            var project = new ProjectState("snapshot-audit-unicode", "Snapshot audit unicode");
            var audit = new AuditEvent
            {
                Utc = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                Action = "Review-\U0001F680",
                ElementId = "E-\U0001F680",
                Detail = "Chi tiết \U0001F680",
                Actor = "QS-\U0001F680",
                CorrelationId = "C-\U0001F680"
            };
            project.AuditEvents.Add(audit);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            Require(detached.AuditEvents.Count == 1, "Detached snapshot lost canonical audit history.");
            var copy = detached.AuditEvents[0];
            Require(!ReferenceEquals(copy, audit), "Detached snapshot aliased the mutable source AuditEvent.");
            Require(copy.Utc == audit.Utc, "Detached snapshot changed canonical audit UTC timestamp.");
            Require(string.Equals(copy.Action, audit.Action, StringComparison.Ordinal), "Detached snapshot changed canonical audit action.");
            Require(string.Equals(copy.ElementId, audit.ElementId, StringComparison.Ordinal), "Detached snapshot changed canonical audit element id.");
            Require(string.Equals(copy.Detail, audit.Detail, StringComparison.Ordinal), "Detached snapshot changed canonical audit detail.");
            Require(string.Equals(copy.Actor, audit.Actor, StringComparison.Ordinal), "Detached snapshot changed canonical audit actor.");
            Require(string.Equals(copy.CorrelationId, audit.CorrelationId, StringComparison.Ordinal), "Detached snapshot changed canonical audit correlation id.");
        }

        private static ProjectState NewRelationProject(string label)
        {
            var project = new ProjectState("snapshot-relation-" + label.Replace(" ", "-"), "Snapshot relation fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            return project;
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception(message);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
