using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditAtomicitySmoke
    {
        internal static void Run()
        {
            InvalidLaterElementDoesNotPartiallyMutateBatch();
            ValidBatchStillAppliesAllChanges();
            AssignFamilyRejectsDuplicateProjectIdsWithoutMutation();
            GenericSemanticReferencesFailClosed();
        }

        private static void InvalidLaterElementDoesNotPartiallyMutateBatch()
        {
            var project = new ProjectState("bulk-atomic", "Bulk Atomic");
            var first = new ProjectElement("A", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("B", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            first.Properties["Factor"] = "2";
            second.Properties["Factor"] = "not-a-number";
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var threw = false;
            try
            {
                new BulkEditService().MultiplyNumericProperty(project, new[] { first, second }, "Factor", 3d);
            }
            catch (FormatException)
            {
                threw = true;
            }

            if (!threw) throw new Exception("Expected invalid numeric bulk-edit input to fail.");
            if (!string.Equals(first.Properties["Factor"], "2", StringComparison.Ordinal))
                throw new Exception("Numeric bulk edit partially mutated an earlier element before a later validation failure.");
            if (first.Dirty != ElementDirtyFlags.None || second.Dirty != ElementDirtyFlags.None)
                throw new Exception("Failed numeric bulk edit dirtied project elements before the batch was validated.");
        }

        private static void ValidBatchStillAppliesAllChanges()
        {
            var project = new ProjectState("bulk-valid", "Bulk Valid");
            var first = new ProjectElement("A", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("B", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            first.Properties["Factor"] = "2";
            second.Properties["Factor"] = "4";
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);

            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { first, second }, "Factor", 3d);
            if (changed.Count != 2) throw new Exception("Expected both valid bulk-edit elements to change.");
            if (!string.Equals(first.Properties["Factor"], "6", StringComparison.Ordinal) ||
                !string.Equals(second.Properties["Factor"], "12", StringComparison.Ordinal))
                throw new Exception("Valid numeric bulk edit did not apply the staged values.");
        }

        private static void AssignFamilyRejectsDuplicateProjectIdsWithoutMutation()
        {
            var project = new ProjectState("bulk-family-duplicate", "Bulk Family Duplicate");
            var oldFamily = new ProjectFamily("old", "Old", ElementCategory.Room);
            oldFamily.Properties["HeightM"] = "3";
            var nextFamily = new ProjectFamily("next", "Next", ElementCategory.Room);
            nextFamily.Properties["HeightM"] = "4";
            project.Families.Add(oldFamily);
            project.Families.Add(nextFamily);

            var first = new ProjectElement("DUP", ElementCategory.Room, oldFamily.Id, string.Empty, string.Empty);
            var second = new ProjectElement("DUP", ElementCategory.Room, oldFamily.Id, string.Empty, string.Empty);
            first.Properties["HeightM"] = "3";
            second.Properties["HeightM"] = "3";
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;

            var threw = false;
            try
            {
                new BulkEditService().AssignFamily(project, new[] { "DUP" }, nextFamily.Id);
            }
            catch (InvalidOperationException ex)
            {
                var message = ex.Message ?? string.Empty;
                threw =
                    message.IndexOf("duplicate element id", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    message.IndexOf("duplicate semantic element id", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!threw) throw new Exception("AssignFamily must fail closed when the project contains duplicate semantic element IDs.");
            if (!string.Equals(first.FamilyId, oldFamily.Id, StringComparison.Ordinal) || !string.Equals(second.FamilyId, oldFamily.Id, StringComparison.Ordinal))
                throw new Exception("Rejected duplicate-ID family assignment mutated an element FamilyId.");
            if (!string.Equals(first.Properties["HeightM"], "3", StringComparison.Ordinal) || !string.Equals(second.Properties["HeightM"], "3", StringComparison.Ordinal))
                throw new Exception("Rejected duplicate-ID family assignment mutated inherited properties.");
            if (first.Dirty != ElementDirtyFlags.None || second.Dirty != ElementDirtyFlags.None)
                throw new Exception("Rejected duplicate-ID family assignment dirtied project elements.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new Exception("Rejected duplicate-ID family assignment touched project persistence state.");
        }

        private static void GenericSemanticReferencesFailClosed()
        {
            var project = new ProjectState("bulk-reference-guard", "Bulk reference guard");
            var first = new ProjectElement("A", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            var second = new ProjectElement("B", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            first.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            second.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            first.Properties["HostRefId"] = "2";
            second.Properties["HostRefId"] = "4";
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(first);
            project.Elements.Add(second);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;
            var service = new BulkEditService();

            ThrowsInvalidOperation(() => service.SetProperty(project, new[] { first, second }, ProjectFloorService.BottomLevelIdKey, "L2"));
            ThrowsInvalidOperation(() => service.MultiplyNumericProperty(project, new[] { first, second }, "HostRefId", 2d));

            if (!string.Equals(first.Properties[ProjectFloorService.BottomLevelIdKey], "L1", StringComparison.Ordinal) ||
                !string.Equals(second.Properties[ProjectFloorService.BottomLevelIdKey], "L1", StringComparison.Ordinal))
                throw new Exception("Generic bulk property edit bypassed the Level relation service.");
            if (!string.Equals(first.Properties["HostRefId"], "2", StringComparison.Ordinal) ||
                !string.Equals(second.Properties["HostRefId"], "4", StringComparison.Ordinal))
                throw new Exception("Generic numeric bulk edit mutated a semantic reference field.");
            if (first.Dirty != ElementDirtyFlags.None || second.Dirty != ElementDirtyFlags.None)
                throw new Exception("Rejected generic semantic-reference edits dirtied project elements.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeUpdated)
                throw new Exception("Rejected generic semantic-reference edits touched project persistence state.");
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try { action(); }
            catch (InvalidOperationException) { return; }
            throw new Exception("Expected generic semantic-reference edit to fail closed.");
        }
    }

    internal static class BulkEditAtomicityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkEditAtomicitySmoke.Run();
    }
}
