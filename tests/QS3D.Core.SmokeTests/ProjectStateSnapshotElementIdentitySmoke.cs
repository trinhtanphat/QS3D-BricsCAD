using System;
using System.Collections.Generic;
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
            InjectLegacyFamilyProperty(family, key, value);
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
            var project = new ProjectState("snapshot-element-identity", "Captured element identity fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.SetProperty("Name", "Captured");
            element.SetQuantity("AreaM2", 10d);
            project.Elements.Add(element);
            var snapshot = ProjectStateSnapshot.Capture(project);
            element.SetProperty("Name", "Mutated");
            element.SetQuantity("AreaM2", 20d);
            snapshot.Restore(project);
            var restored = project.FindElement("E1") ?? throw new Exception("Restored project lost E1.");
            Require(ReferenceEquals(element, restored), "Snapshot restore replaced the captured ProjectElement object instead of preserving identity.");
            Require(restored.Properties["Name"] == "Captured", "Snapshot restore did not restore the captured element property state.");
            Require(restored.Quantities["AreaM2"] == 10d, "Snapshot restore did not restore the captured element quantity state.");
        }

        private static void RestoreIntoDifferentSameIdProjectNeverInjectsCapturedElements()
        {
            var capturedProject = new ProjectState("snapshot-same-project-id", "Captured project");
            var capturedElement = new ProjectElement("E1", ElementCategory.Room);
            capturedElement.SetProperty("Name", "Captured");
            capturedProject.Elements.Add(capturedElement);
            var snapshot = ProjectStateSnapshot.Capture(capturedProject);

            var targetProject = new ProjectState("snapshot-same-project-id", "Target project");
            var targetElement = new ProjectElement("E1", ElementCategory.Room);
            targetElement.SetProperty("Name", "Target");
            targetProject.Elements.Add(targetElement);

            snapshot.Restore(targetProject);
            var restored = targetProject.FindElement("E1") ?? throw new Exception("Same-id target project lost E1.");
            Require(!ReferenceEquals(capturedElement, restored), "Snapshot restore injected a captured ProjectElement object into a different ProjectState instance.");
            Require(!ReferenceEquals(targetElement, restored), "Snapshot restore into a different same-id project must materialize detached element state rather than preserving the target instance.");
            Require(restored.Properties["Name"] == "Captured", "Snapshot restore into a different same-id project did not restore captured state.");
        }

        private static void DetachedCopyNeverAliasesCanonicalElements()
        {
            var project = new ProjectState("snapshot-detached-alias", "Detached alias fixture");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.SetProperty("Name", "Captured");
            project.Elements.Add(element);
            var detached = ProjectStateSnapshot.CreateDetachedCopy(project);
            var copied = detached.FindElement("E1") ?? throw new Exception("Detached copy lost E1.");
            Require(!ReferenceEquals(element, copied), "Detached snapshot copy aliased the source ProjectElement object.");
            Require(copied.Properties["Name"] == "Captured", "Detached snapshot copy changed element semantic state.");
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField(
                "_inner",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Legacy Family fixture could not locate the property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Legacy Family fixture property backing dictionary had an unexpected type.");
            inner[key] = value;
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}