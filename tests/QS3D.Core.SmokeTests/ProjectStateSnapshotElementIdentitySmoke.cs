using System;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateSnapshotElementIdentitySmoke
    {
        public static void Run()
        {
            RestoreAtRevisionCeilingDoesNotOverflow();
            RestorePreservesCapturedElementIdentity();
            RestoreIntoDifferentSameIdProjectNeverInjectsCapturedElements();
            DetachedCopyNeverAliasesCanonicalElements();
        }

        private static void RestoreAtRevisionCeilingDoesNotOverflow()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-snapshot-revision-ceiling-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.Save(new ProjectState("snapshot-revision-ceiling", "Captured name"), path);

                var document = XDocument.Load(path, LoadOptions.None);
                var root = document.Root ?? throw new Exception("Serialized QSDB root was not found for revision-ceiling fixture.");
                root.SetAttributeValue(
                    "changeVersion",
                    (long.MaxValue - 1L).ToString(System.Globalization.CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);

                var project = store.Load(path);
                Require(project.ChangeVersion == long.MaxValue - 1L, "Revision-ceiling fixture did not restore the persisted ChangeVersion.");
                var capturedUpdatedUtc = project.UpdatedUtc;
                var rollback = ProjectStateSnapshot.Capture(project);

                project.Name = "Mutated name";
                Require(project.ChangeVersion == long.MaxValue, "Revision-ceiling mutation did not reach long.MaxValue.");

                rollback.Restore(project);

                Require(project.Name == "Captured name", "Revision-ceiling rollback did not restore the captured project name.");
                Require(project.ChangeVersion == long.MaxValue - 1L, "Revision-ceiling rollback did not restore the captured ChangeVersion exactly.");
                Require(project.UpdatedUtc == capturedUpdatedUtc, "Revision-ceiling rollback did not restore the captured UpdatedUtc exactly.");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
                try { if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); } catch { }
            }
        }

        private static void RestorePreservesCapturedElementIdentity()
        {
            var project = new ProjectState("snapshot-element-identity", "Snapshot element identity");
            var first = new ProjectElement("E1", ElementCategory.ArchitecturalWall, "F1", "L1", "Z1")
            {
                DrawingFingerprint = "DWG-A"
            };
            first.SourceHandles.Add("A1");
            first.DependsOn.Add("HOST-1");
            first.Properties["Material"] = "Before";
            first.SetQuantity("NetConcreteM3", 1.25d);
            first.MarkClean(ElementDirtyFlags.All);

            var second = new ProjectElement("E2", ElementCategory.Slab, "F2", "L2", "Z2")
            {
                DrawingFingerprint = "DWG-B"
            };
            second.SourceHandles.Add("B2");
            second.Properties["ThicknessM"] = "0.2";
            second.SetQuantity("AreaM2", 12d);
            second.MarkClean(ElementDirtyFlags.All);

            project.Elements.Add(first);
            project.Elements.Add(second);
            project.Metadata["SnapshotMarker"] = "before";
            project.Touch();

            var firstDirty = first.Dirty;
            var firstUpdatedUtc = first.UpdatedUtc;
            var secondDirty = second.Dirty;
            var secondUpdatedUtc = second.UpdatedUtc;
            var projectUpdatedUtc = project.UpdatedUtc;
            var projectChangeVersion = project.ChangeVersion;
            var rollback = ProjectStateSnapshot.Capture(project);

            first.Category = ElementCategory.Beam;
            first.FamilyId = "MUTATED-FAMILY";
            first.FloorId = "MUTATED-FLOOR";
            first.ZoneId = "MUTATED-ZONE";
            first.DrawingFingerprint = "MUTATED-DWG";
            first.SourceHandles.Clear();
            first.SourceHandles.Add("FFFF");
            first.DependsOn.Clear();
            first.DependsOn.Add("MUTATED-HOST");
            first.SetProperty("Material", "After");
            first.SetProperty("Transient", "remove-on-rollback");
            first.SetQuantity("NetConcreteM3", 99d);
            first.MarkDirty(ElementDirtyFlags.All);

            second.SetProperty("Transient", "removed-element-mutation");
            project.Elements.Remove(second);

            var added = new ProjectElement("E3", ElementCategory.Column);
            added.Properties["Transient"] = "post-capture";
            project.Elements.Insert(0, added);
            project.Metadata["SnapshotMarker"] = "after";
            project.Touch();

            rollback.Restore(project);

            Require(project.Elements.Count == 2, "Rollback did not restore the captured element count.");
            Require(ReferenceEquals(project.Elements[0], first), "Rollback replaced the first captured canonical ProjectElement reference.");
            Require(ReferenceEquals(project.Elements[1], second), "Rollback did not reinsert the removed captured ProjectElement reference.");
            Require(ReferenceEquals(project.FindElement("E1"), first), "FindElement(E1) no longer returns the pre-transaction canonical object after rollback.");
            Require(ReferenceEquals(project.FindElement("E2"), second), "FindElement(E2) no longer returns the removed pre-transaction canonical object after rollback.");
            Require(project.FindElement("E3") == null, "Rollback retained an element created after snapshot capture.");

            Require(first.Category == ElementCategory.ArchitecturalWall, "Rollback did not restore element category.");
            Require(first.FamilyId == "F1", "Rollback did not restore FamilyId.");
            Require(first.FloorId == "L1", "Rollback did not restore FloorId.");
            Require(first.ZoneId == "Z1", "Rollback did not restore ZoneId.");
            Require(first.DrawingFingerprint == "DWG-A", "Rollback did not restore element drawing fingerprint.");
            Require(first.SourceHandles.Count == 1 && first.SourceHandles[0] == "A1", "Rollback did not restore source handles.");
            Require(first.DependsOn.Count == 1 && first.DependsOn[0] == "HOST-1", "Rollback did not restore dependencies.");
            Require(first.Properties.Count == 1 && first.Properties["Material"] == "Before", "Rollback did not restore element properties exactly.");
            Require(first.Quantities.Count == 1 && first.Quantities["NetConcreteM3"].Equals(1.25d), "Rollback did not restore element quantities exactly.");
            Require(first.Dirty == firstDirty, "Rollback did not restore first element dirty flags.");
            Require(first.UpdatedUtc == firstUpdatedUtc, "Rollback did not restore first element UpdatedUtc.");

            Require(second.Properties.Count == 1 && second.Properties["ThicknessM"] == "0.2", "Rollback did not restore the removed element values.");
            Require(second.Quantities.Count == 1 && second.Quantities["AreaM2"].Equals(12d), "Rollback did not restore the removed element quantities.");
            Require(second.Dirty == secondDirty, "Rollback did not restore removed element dirty flags.");
            Require(second.UpdatedUtc == secondUpdatedUtc, "Rollback did not restore removed element UpdatedUtc.");

            Require(project.Metadata["SnapshotMarker"] == "before", "Rollback did not restore project metadata.");
            Require(project.ChangeVersion == projectChangeVersion, "Rollback did not restore project ChangeVersion.");
            Require(project.UpdatedUtc == projectUpdatedUtc, "Rollback did not restore project UpdatedUtc.");
        }

        private static void RestoreIntoDifferentSameIdProjectNeverInjectsCapturedElements()
        {
            var capturedProject = new ProjectState("snapshot-shared-id", "Captured project");
            var capturedElement = new ProjectElement("E1", ElementCategory.Beam);
            capturedElement.Properties["Name"] = "Captured";
            capturedProject.Elements.Add(capturedElement);
            var rollback = ProjectStateSnapshot.Capture(capturedProject);

            var foreignProject = new ProjectState("snapshot-shared-id", "Foreign project");
            var foreignElement = new ProjectElement("E1", ElementCategory.Column);
            foreignElement.Properties["Name"] = "Foreign";
            foreignProject.Elements.Add(foreignElement);

            rollback.Restore(foreignProject);

            var restoredElement = foreignProject.FindElement("E1") ?? throw new Exception("Foreign-target restore lost E1.");
            Require(!ReferenceEquals(restoredElement, capturedElement), "Foreign-target restore injected a canonical element reference from the captured project.");
            Require(!ReferenceEquals(restoredElement, foreignElement), "Foreign-target restore unexpectedly reused the foreign project's pre-restore element reference.");
            Require(restoredElement.Category == ElementCategory.Beam, "Foreign-target restore did not copy captured element values.");
            Require(restoredElement.Properties["Name"] == "Captured", "Foreign-target restore did not copy captured element properties.");

            restoredElement.SetProperty("Name", "Restored foreign");
            Require(capturedElement.Properties["Name"] == "Captured", "Mutating a foreign-target restored element changed the captured project's canonical element.");
        }

        private static void DetachedCopyNeverAliasesCanonicalElements()
        {
            var project = new ProjectState("snapshot-detached-identity", "Snapshot detached identity");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.Properties["Name"] = "Canonical";
            project.Elements.Add(element);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedElement = detached.FindElement("E1") ?? throw new Exception("Detached copy lost E1.");

            Require(!ReferenceEquals(detached, project), "CreateDetachedCopy returned the canonical ProjectState.");
            Require(!ReferenceEquals(detachedElement, element), "CreateDetachedCopy aliased the canonical ProjectElement.");

            detachedElement.SetProperty("Name", "Detached");
            detachedElement.SourceHandles.Add("DETACHED");
            Require(element.Properties["Name"] == "Canonical", "Mutating a detached element changed canonical properties.");
            Require(element.SourceHandles.Count == 0, "Mutating a detached element changed canonical source handles.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
