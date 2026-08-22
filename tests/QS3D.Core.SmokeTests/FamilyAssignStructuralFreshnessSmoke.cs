using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyAssignStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RemovedElementDuringLazyEnumerationFailsClosed();
            RemovedTargetFamilyDuringLazyEnumerationFailsClosed();
        }

        private static void RemovedElementDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-1", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenRemoveElement(project, element)),
                "Element no longer belongs to the project after Family assignment target enumeration");

            Equal(beforeVersion, project.ChangeVersion, "removed-element project revision");
            False(project.Elements.Contains(element), "removed-element external removal");
            Equal(string.Empty, element.FamilyId, "removed-element FamilyId");
            False(element.Properties.ContainsKey("Material"), "removed-element inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-element dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-element timestamp");
        }

        private static void RemovedTargetFamilyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-2", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenRemoveFamily(project, family, element)),
                "Target Family no longer belongs to the project after assignment target enumeration");

            Equal(beforeVersion, project.ChangeVersion, "removed-family project revision");
            False(project.Families.Contains(family), "removed-family external removal");
            Equal(string.Empty, element.FamilyId, "removed-family FamilyId");
            False(element.Properties.ContainsKey("Material"), "removed-family inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-family dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-family timestamp");
        }

        private static ProjectState CreateProject(string id, out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState(id, "Family structural freshness");
            family = new ProjectFamily("FAM-STRUCT", "Structural Family", ElementCategory.Beam);
            family.Properties["Material"] = "Steel";
            element = new ProjectElement("E-STRUCT", ElementCategory.Beam);
            project.Families.Add(family);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> YieldThenRemoveElement(ProjectState project, ProjectElement element)
        {
            yield return element;
            project.Elements.Remove(element);
        }

        private static IEnumerable<ProjectElement> YieldThenRemoveFamily(ProjectState project, ProjectFamily family, ProjectElement element)
        {
            yield return element;
            project.Families.Remove(family);
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("FamilyAssignStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("FamilyAssignStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("FamilyAssignStructuralFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("FamilyAssignStructuralFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
