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
            RejectsInvalidMutablePropertyState();
            PreservesCanonicalPropertyState();
            RejectsInvalidMutableQuantityState();
            RejectsCanonicalQuantityNameCollision();
            DetachedCopyCanonicalizesNegativeZero();
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
                root.SetAttributeValue("changeVersion", (long.MaxValue - 1L).ToString(System.Globalization.CultureInfo.InvariantCulture));
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

        private static void RejectsInvalidMutablePropertyState()
        {
            ExpectRejectedElementProperty("padded key", " WidthM ", "0.2");
            ExpectRejectedElementProperty("control key", "Width\tM", "0.2");
            ExpectRejectedElementProperty("malformed key", "Width\uD800M", "0.2");
            ExpectRejectedElementProperty("malformed value", "WidthM", "bad\uD800value");
            ExpectRejectedFamilyProperty("padded key", " WidthM ", "0.2");
            ExpectRejectedFamilyProperty("control key", "Width\nM", "0.2");
            ExpectRejectedFamilyProperty("malformed key", "Width\uD800M", "0.2");
            ExpectRejectedFamilyProperty("malformed value", "WidthM", "bad\uD800value");
            ExpectRejectedFamilyProperty("oversized key", new string('K', 121), "0.2");
            ExpectRejectedFamilyProperty("oversized value", "Description", new string('V', 1001));
        }

        private static void ExpectRejectedElementProperty(string label, string key, string value)
        {
            var project = new ProjectState("snapshot-invalid-element-property-" + label.Replace(" ", "-"), "Invalid element property fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.Properties[key] = value;
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);
            var originalDirty = element.Dirty;
            var originalUpdatedUtc = element.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;
            var originalProjectUpdatedUtc = project.UpdatedUtc;
            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " element property was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " element property was accepted by detached-copy capture.");
            Require(element.Properties.Count == 1 && element.Properties.ContainsKey(key) && string.Equals(element.Properties[key], value, StringComparison.Ordinal), "Rejected element-property validation mutated source properties.");
            Require(element.Dirty == originalDirty && element.UpdatedUtc == originalUpdatedUtc, "Rejected element-property validation changed source persistence state.");
            Require(project.ChangeVersion == originalChangeVersion && project.UpdatedUtc == originalProjectUpdatedUtc, "Rejected element-property validation changed project persistence state.");
        }

        private static void ExpectRejectedFamilyProperty(string label, string key, string value)
        {
            var project = new ProjectState("snapshot-invalid-family-property-" + label.Replace(" ", "-"), "Invalid Family property fixture");
            var family = new ProjectFamily("F1", "Family", ElementCategory.Room);
            family.Properties[key] = value;
            project.Families.Add(family);
            var originalChangeVersion = project.ChangeVersion;
            var originalProjectUpdatedUtc = project.UpdatedUtc;
            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " Family property was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " Family property was accepted by detached-copy capture.");
            Require(family.Properties.Count == 1 && family.Properties.ContainsKey(key) && string.Equals(family.Properties[key], value, StringComparison.Ordinal), "Rejected Family-property validation mutated source properties.");
            Require(project.ChangeVersion == originalChangeVersion && project.UpdatedUtc == originalProjectUpdatedUtc, "Rejected Family-property validation changed project persistence state.");
        }

        private static void PreservesCanonicalPropertyState()
        {
            var project = new ProjectState("snapshot-property-unicode", "Canonical property fixture");
            var family = new ProjectFamily("F1", "Family", ElementCategory.Room);
            family.Properties["Description"] = "Family-\U0001F680\tvalue";
            project.Families.Add(family);
            var element = new ProjectElement("E1", ElementCategory.Room, "F1", string.Empty, string.Empty);
            element.SetProperty("Label", "Element-\U0001F680\nvalue");
            element.MarkClean(ElementDirtyFlags.All);
            var dirty = element.Dirty;
            var updatedUtc = element.UpdatedUtc;
            project.Elements.Add(element);

            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedFamily = detached.FindFamily("F1") ?? throw new Exception("Detached property fixture lost F1.");
            var detachedElement = detached.FindElement("E1") ?? throw new Exception("Detached property fixture lost E1.");
            Require(detachedFamily.Properties.Count == 1 && detachedFamily.Properties["Description"] == "Family-\U0001F680\tvalue", "Detached snapshot changed canonical Family property Unicode/control-preserving value semantics.");
            Require(detachedElement.Properties.Count == 1 && detachedElement.Properties["Label"] == "Element-\U0001F680\nvalue", "Detached snapshot changed canonical element property Unicode/control-preserving value semantics.");
            Require(detachedElement.Dirty == dirty && detachedElement.UpdatedUtc == updatedUtc, "Canonical element property cloning changed captured persistence state.");
        }

        private static void RejectsInvalidMutableQuantityState()
        {
            ExpectRejectedQuantity("padded name", " AreaM2 ", 1d);
            ExpectRejectedQuantity("negative", "AreaM2", -1d);
            ExpectRejectedQuantity("NaN", "AreaM2", double.NaN);
            ExpectRejectedQuantity("positive infinity", "AreaM2", double.PositiveInfinity);
            ExpectRejectedQuantity("control-character name", "Area\tM2", 1d);
            ExpectRejectedQuantity("malformed UTF-16 name", "Area\uD800M2", 1d);
        }

        private static void ExpectRejectedQuantity(string label, string name, double value)
        {
            var project = new ProjectState("snapshot-invalid-quantity-" + label.Replace(" ", "-"), "Invalid quantity fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.Quantities[name] = value;
            project.Elements.Add(element);
            var originalDirty = element.Dirty;
            var originalUpdatedUtc = element.UpdatedUtc;
            var originalChangeVersion = project.ChangeVersion;
            var originalProjectUpdatedUtc = project.UpdatedUtc;
            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), label + " quantity was accepted by snapshot capture.");
            ExpectInvalidOperation(() => ProjectStateSnapshot.CreateDetachedCopy(project), label + " quantity was accepted by detached-copy capture.");
            Require(element.Quantities.Count == 1 && element.Quantities.ContainsKey(name), "Rejected snapshot quantity validation mutated the source quantity dictionary.");
            Require(element.Dirty == originalDirty, "Rejected snapshot quantity validation changed source dirty flags.");
            Require(element.UpdatedUtc == originalUpdatedUtc, "Rejected snapshot quantity validation changed source UpdatedUtc.");
            Require(project.ChangeVersion == originalChangeVersion, "Rejected snapshot quantity validation changed project ChangeVersion.");
            Require(project.UpdatedUtc == originalProjectUpdatedUtc, "Rejected snapshot quantity validation changed project UpdatedUtc.");
        }

        private static void RejectsCanonicalQuantityNameCollision()
        {
            var project = new ProjectState("snapshot-quantity-collision", "Quantity collision fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.Quantities["AreaM2"] = 1d;
            element.Quantities[" AreaM2 "] = 2d;
            project.Elements.Add(element);
            ExpectInvalidOperation(() => ProjectStateSnapshot.Capture(project), "Snapshot accepted quantity names that collapse to one canonical identity.");
            Require(element.Quantities.Count == 2, "Canonical-collision rejection mutated source quantities.");
        }

        private static void DetachedCopyCanonicalizesNegativeZero()
        {
            var project = new ProjectState("snapshot-negative-zero", "Negative zero fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.Quantities["AreaM2"] = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL));
            element.MarkClean(ElementDirtyFlags.All);
            var dirty = element.Dirty;
            var updatedUtc = element.UpdatedUtc;
            project.Elements.Add(element);
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var detachedElement = detached.FindElement("E1") ?? throw new Exception("Detached negative-zero fixture lost E1.");
            Require(detachedElement.Quantities.Count == 1 && detachedElement.Quantities.ContainsKey("AreaM2"), "Detached snapshot changed canonical quantity identity.");
            var copied = detachedElement.Quantities["AreaM2"];
            Require(copied == 0d, "Detached snapshot changed zero quantity magnitude.");
            Require(BitConverter.DoubleToInt64Bits(copied) == 0L, "Detached snapshot bypassed canonical positive-zero normalization.");
            Require(detachedElement.Dirty == dirty, "Canonical quantity cloning changed captured dirty flags.");
            Require(detachedElement.UpdatedUtc == updatedUtc, "Canonical quantity cloning changed captured UpdatedUtc.");
        }

        private static void RestorePreservesCapturedElementIdentity()
        {
            var project = new ProjectState("snapshot-element-identity", "Snapshot element identity");
            var first = new ProjectElement("E1", ElementCategory.ArchitecturalWall, "F1", "L1", "Z1") { DrawingFingerprint = "DWG-A" };
            first.SourceHandles.Add("A1"); first.DependsOn.Add("HOST-1"); first.Properties["Material"] = "Before"; first.SetQuantity("NetConcreteM3", 1.25d); first.MarkClean(ElementDirtyFlags.All);
            var second = new ProjectElement("E2", ElementCategory.Slab, "F2", "L2", "Z2") { DrawingFingerprint = "DWG-B" };
            second.SourceHandles.Add("B2"); second.Properties["ThicknessM"] = "0.2"; second.SetQuantity("AreaM2", 12d); second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first); project.Elements.Add(second); project.Metadata["SnapshotMarker"] = "before"; project.Touch();
            var firstDirty = first.Dirty; var firstUpdatedUtc = first.UpdatedUtc; var secondDirty = second.Dirty; var secondUpdatedUtc = second.UpdatedUtc;
            var projectUpdatedUtc = project.UpdatedUtc; var projectChangeVersion = project.ChangeVersion; var rollback = ProjectStateSnapshot.Capture(project);
            first.Category = ElementCategory.Beam; first.FamilyId = "MUTATED-FAMILY"; first.FloorId = "MUTATED-FLOOR"; first.ZoneId = "MUTATED-ZONE"; first.DrawingFingerprint = "MUTATED-DWG";
            first.SourceHandles.Clear(); first.SourceHandles.Add("FFFF"); first.DependsOn.Clear(); first.DependsOn.Add("MUTATED-HOST"); first.SetProperty("Material", "After"); first.SetProperty("Transient", "remove-on-rollback"); first.SetQuantity("NetConcreteM3", 99d); first.MarkDirty(ElementDirtyFlags.All);
            second.SetProperty("Transient", "removed-element-mutation"); project.Elements.Remove(second);
            var added = new ProjectElement("E3", ElementCategory.Column); added.Properties["Transient"] = "post-capture"; project.Elements.Insert(0, added); project.Metadata["SnapshotMarker"] = "after"; project.Touch();
            rollback.Restore(project);
            Require(project.Elements.Count == 2, "Rollback did not restore the captured element count.");
            Require(ReferenceEquals(project.Elements[0], first) && ReferenceEquals(project.Elements[1], second), "Rollback did not preserve captured canonical element references.");
            Require(ReferenceEquals(project.FindElement("E1"), first) && ReferenceEquals(project.FindElement("E2"), second) && project.FindElement("E3") == null, "Rollback element identity/content mismatch.");
            Require(first.Category == ElementCategory.ArchitecturalWall && first.FamilyId == "F1" && first.FloorId == "L1" && first.ZoneId == "Z1" && first.DrawingFingerprint == "DWG-A", "Rollback did not restore element scalar state.");
            Require(first.SourceHandles.Count == 1 && first.SourceHandles[0] == "A1" && first.DependsOn.Count == 1 && first.DependsOn[0] == "HOST-1", "Rollback did not restore element relations.");
            Require(first.Properties.Count == 1 && first.Properties["Material"] == "Before" && first.Quantities.Count == 1 && first.Quantities["NetConcreteM3"].Equals(1.25d), "Rollback did not restore first element payload.");
            Require(first.Dirty == firstDirty && first.UpdatedUtc == firstUpdatedUtc, "Rollback did not restore first element persistence state.");
            Require(second.Properties.Count == 1 && second.Properties["ThicknessM"] == "0.2" && second.Quantities.Count == 1 && second.Quantities["AreaM2"].Equals(12d), "Rollback did not restore removed element payload.");
            Require(second.Dirty == secondDirty && second.UpdatedUtc == secondUpdatedUtc, "Rollback did not restore removed element persistence state.");
            Require(project.Metadata["SnapshotMarker"] == "before" && project.ChangeVersion == projectChangeVersion && project.UpdatedUtc == projectUpdatedUtc, "Rollback did not restore project persistence state.");
        }

        private static void RestoreIntoDifferentSameIdProjectNeverInjectsCapturedElements()
        {
            var capturedProject = new ProjectState("snapshot-shared-id", "Captured project"); var capturedElement = new ProjectElement("E1", ElementCategory.Beam); capturedElement.Properties["Name"] = "Captured"; capturedProject.Elements.Add(capturedElement); var rollback = ProjectStateSnapshot.Capture(capturedProject);
            var foreignProject = new ProjectState("snapshot-shared-id", "Foreign project"); var foreignElement = new ProjectElement("E1", ElementCategory.Column); foreignElement.Properties["Name"] = "Foreign"; foreignProject.Elements.Add(foreignElement); rollback.Restore(foreignProject);
            var restoredElement = foreignProject.FindElement("E1") ?? throw new Exception("Foreign-target restore lost E1.");
            Require(!ReferenceEquals(restoredElement, capturedElement) && !ReferenceEquals(restoredElement, foreignElement), "Foreign-target restore reused an existing canonical element reference.");
            Require(restoredElement.Category == ElementCategory.Beam && restoredElement.Properties["Name"] == "Captured", "Foreign-target restore did not copy captured values.");
            restoredElement.SetProperty("Name", "Restored foreign"); Require(capturedElement.Properties["Name"] == "Captured", "Foreign restored mutation leaked into captured project.");
        }

        private static void DetachedCopyNeverAliasesCanonicalElements()
        {
            var project = new ProjectState("snapshot-detached-identity", "Snapshot detached identity"); var element = new ProjectElement("E1", ElementCategory.Room); element.Properties["Name"] = "Canonical"; project.Elements.Add(element);
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project); var detachedElement = detached.FindElement("E1") ?? throw new Exception("Detached copy lost E1.");
            Require(!ReferenceEquals(detached, project) && !ReferenceEquals(detachedElement, element), "CreateDetachedCopy aliased canonical state.");
            detachedElement.SetProperty("Name", "Detached"); detachedElement.SourceHandles.Add("DETACHED");
            Require(element.Properties["Name"] == "Canonical" && element.SourceHandles.Count == 0, "Detached mutation leaked into canonical element.");
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try { action(); } catch (InvalidOperationException) { return; }
            throw new Exception(message);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
    }
}
