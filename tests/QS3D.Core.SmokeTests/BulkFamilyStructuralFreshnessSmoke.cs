using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkFamilyStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RemovedTargetFamilyDuringLazyEnumerationFailsClosed();
            ReplacedTargetFamilyDuringLazyEnumerationFailsClosed();
            StableTargetFamilyStillAssigns();
        }

        private static void RemovedTargetFamilyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-BULK-FAMILY-STRUCT-1", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => new BulkEditService().AssignFamily(project, YieldThenRemoveFamily(project, family, element.Id), family.Id),
                "Bulk Family target-id enumeration changed the project while targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion, "removed-family project revision");
            False(project.Families.Contains(family), "removed-family caller side effect");
            Equal(string.Empty, element.FamilyId, "removed-family FamilyId");
            Equal(0, element.Properties.Count, "removed-family property count");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-family dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-family timestamp");
        }

        private static void ReplacedTargetFamilyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-BULK-FAMILY-STRUCT-2", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => new BulkEditService().AssignFamily(project, YieldThenReplaceFamily(project, family, element.Id), family.Id),
                "Bulk Family target-id enumeration changed the project while targets were being enumerated");

            Equal(beforeVersion + 2L, project.ChangeVersion, "replaced-family project revision");
            False(project.Families.Contains(family), "replaced-family original instance");
            Equal(string.Empty, element.FamilyId, "replaced-family FamilyId");
            Equal(0, element.Properties.Count, "replaced-family property count");
            Equal(ElementDirtyFlags.None, element.Dirty, "replaced-family dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "replaced-family timestamp");
        }

        private static void StableTargetFamilyStillAssigns()
        {
            var project = CreateProject("P-BULK-FAMILY-STRUCT-3", out var family, out var element);
            family.Properties["WidthM"] = "0.4";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().AssignFamily(project, YieldStable(element.Id), family.Id);

            Equal(1, changed, "stable assignment count");
            Equal(family.Id, element.FamilyId, "stable FamilyId");
            Equal("0.4", element.Properties["WidthM"], "stable inherited property");
            Equal(beforeVersion + 1L, project.ChangeVersion, "stable project revision");
        }

        private static ProjectState CreateProject(string id, out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState(id, "Bulk Family structural freshness");
            family = new ProjectFamily("F-BULK-STRUCT", "Bulk Structural Family", ElementCategory.Beam);
            element = new ProjectElement("E-BULK-STRUCT", ElementCategory.Beam);
            project.Families.Add(family);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> YieldThenRemoveFamily(ProjectState project, ProjectFamily family, string elementId)
        {
            yield return elementId;
            project.Families.Remove(family);
        }

        private static IEnumerable<string> YieldThenReplaceFamily(ProjectState project, ProjectFamily family, string elementId)
        {
            yield return elementId;
            project.Families.Remove(family);
            project.Families.Add(new ProjectFamily(family.Id, "Replacement Family", family.Category));
        }

        private static IEnumerable<string> YieldStable(string elementId)
        {
            yield return elementId;
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkFamilyStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkFamilyStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("BulkFamilyStructuralFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("BulkFamilyStructuralFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
