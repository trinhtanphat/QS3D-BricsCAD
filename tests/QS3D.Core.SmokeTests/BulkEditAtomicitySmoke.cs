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

            var threw = false;
            try
            {
                new BulkEditService().AssignFamily(project, new[] { "DUP" }, nextFamily.Id);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    ex.Message.IndexOf("element id", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!threw) throw new Exception("AssignFamily must fail closed when the project contains duplicate semantic element IDs.");
            if (!string.Equals(first.FamilyId, oldFamily.Id, StringComparison.Ordinal) || !string.Equals(second.FamilyId, oldFamily.Id, StringComparison.Ordinal))
                throw new Exception("Rejected duplicate-ID family assignment mutated an element FamilyId.");
            if (!string.Equals(first.Properties["HeightM"], "3", StringComparison.Ordinal) || !string.Equals(second.Properties["HeightM"], "3", StringComparison.Ordinal))
                throw new Exception("Rejected duplicate-ID family assignment mutated inherited properties.");
            if (first.Dirty != ElementDirtyFlags.None || second.Dirty != ElementDirtyFlags.None)
                throw new Exception("Rejected duplicate-ID family assignment dirtied project elements.");
        }
    }

    internal static class BulkEditAtomicityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => BulkEditAtomicitySmoke.Run();
    }
}
