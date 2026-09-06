using System;
using System.Collections.Generic;
using System.Reflection;
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
            UnrelatedDuplicateFamilyDuringLazyEnumerationFailsClosed();
            UnrelatedDuplicateElementDuringLazyEnumerationFailsClosed();
            TargetDefaultsChangedDuringLazyEnumerationFailClosed();
            MalformedTargetDefaultsDuringLazyEnumerationFailClosed();
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
                "Project changed while Family assignment targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion, "removed-family caller structural revision");
            False(project.Families.Contains(family), "removed-family external removal");
            Equal(string.Empty, element.FamilyId, "removed-family FamilyId");
            False(element.Properties.ContainsKey("Material"), "removed-family inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-family dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-family timestamp");
        }

        private static void UnrelatedDuplicateFamilyDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-3", out var family, out var element);
            project.Families.Add(new ProjectFamily("F-OTHER", "Other Family", ElementCategory.Beam));
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenDuplicateUnrelatedFamily(project, element)),
                "Project changed while Family assignment targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion, "duplicate-family caller structural revision");
            Equal(3, project.Families.Count, "duplicate-family deliberate corruption count");
            Equal(string.Empty, element.FamilyId, "duplicate-family target FamilyId");
            False(element.Properties.ContainsKey("Material"), "duplicate-family inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "duplicate-family target dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "duplicate-family target timestamp");
        }

        private static void UnrelatedDuplicateElementDuringLazyEnumerationFailsClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-4", out var family, out var element);
            project.Elements.Add(new ProjectElement("E-OTHER", ElementCategory.Beam));
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenDuplicateUnrelatedElement(project, element)),
                "Project contains duplicate semantic element id: e-other");

            Equal(beforeVersion, project.ChangeVersion, "duplicate-element project revision");
            Equal(3, project.Elements.Count, "duplicate-element deliberate corruption count");
            Equal(string.Empty, element.FamilyId, "duplicate-element target FamilyId");
            False(element.Properties.ContainsKey("Material"), "duplicate-element inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "duplicate-element target dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "duplicate-element target timestamp");
        }

        private static void TargetDefaultsChangedDuringLazyEnumerationFailClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-5", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenChangeTargetMaterial(family, element)),
                "Project changed while Family assignment targets were being enumerated");

            Equal(beforeVersion + 1L, project.ChangeVersion, "target-default external mutation project revision");
            Equal("Concrete", family.Properties["Material"], "target-default current Family material");
            Equal(string.Empty, element.FamilyId, "target-default stale assignment FamilyId");
            False(element.Properties.ContainsKey("Material"), "target-default stale assignment inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "target-default stale assignment dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "target-default stale assignment timestamp");
        }

        private static void MalformedTargetDefaultsDuringLazyEnumerationFailClosed()
        {
            var project = CreateProject("P-FAMILY-STRUCT-6", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, YieldThenAddMalformedTargetDefault(family, element)),
                "Target Family contains a non-canonical property key");

            Equal(beforeVersion, project.ChangeVersion, "malformed-target-default legacy injection must not get ChangeVersion help");
            Equal("Invalid", family.Properties[" Material "], "malformed-target-default external mutation");
            Equal(string.Empty, element.FamilyId, "malformed-target-default FamilyId");
            False(element.Properties.ContainsKey("Material"), "malformed-target-default inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "malformed-target-default dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "malformed-target-default timestamp");
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

        private static IEnumerable<ProjectElement> YieldThenDuplicateUnrelatedFamily(ProjectState project, ProjectElement element)
        {
            yield return element;
            project.Families.Add(new ProjectFamily("f-other", "Other Family Duplicate", ElementCategory.Beam));
        }

        private static IEnumerable<ProjectElement> YieldThenDuplicateUnrelatedElement(ProjectState project, ProjectElement element)
        {
            yield return element;
            project.Elements.Add(new ProjectElement("e-other", ElementCategory.Beam));
        }

        private static IEnumerable<ProjectElement> YieldThenChangeTargetMaterial(ProjectFamily family, ProjectElement element)
        {
            yield return element;
            family.Properties["Material"] = "Concrete";
        }

        private static IEnumerable<ProjectElement> YieldThenAddMalformedTargetDefault(ProjectFamily family, ProjectElement element)
        {
            yield return element;
            InjectLegacyFamilyProperty(family, " Material ", "Invalid");
        }

        private static void InjectLegacyFamilyProperty(ProjectFamily family, string key, string value)
        {
            var innerField = family.Properties.GetType().GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("Legacy Family fixture could not locate the property backing dictionary.");
            var inner = innerField.GetValue(family.Properties) as Dictionary<string, string>
                ?? throw new Exception("Legacy Family fixture property backing dictionary had an unexpected type.");
            inner[key] = value;
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