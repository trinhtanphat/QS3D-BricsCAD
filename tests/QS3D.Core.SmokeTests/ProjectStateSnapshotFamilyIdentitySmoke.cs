using System;
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

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
