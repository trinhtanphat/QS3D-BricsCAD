using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyAssignInputFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StableLazyInputAssignsFamily();
            MutatingLazyInputFailsBeforeAssignment();
            MutatingEmptyInputFailsBeforeNoOp();
        }

        private static void StableLazyInputAssignsFamily()
        {
            var project = CreateProject("P-FAMILY-FRESH-1", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = ProjectFamilyService.Assign(project, family.Id, LazyElement(element));

            Equal(1, changed, "stable changed count");
            Equal(family.Id, element.FamilyId, "stable FamilyId");
            Equal("Steel", element.Properties["Material"], "stable inherited property");
            Equal(beforeVersion + 1L, project.ChangeVersion, "stable project revision");
            Equal(ElementDirtyFlags.All, element.Dirty, "stable dirty flags");
        }

        private static void MutatingLazyInputFailsBeforeAssignment()
        {
            var project = CreateProject("P-FAMILY-FRESH-2", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, TouchThenYield(project, element)),
                "Project changed while Family assignment targets were being enumerated.");

            Equal(beforeVersion + 1L, project.ChangeVersion, "mutating-yield project revision");
            Equal(string.Empty, element.FamilyId, "mutating-yield FamilyId");
            False(element.Properties.ContainsKey("Material"), "mutating-yield inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "mutating-yield dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "mutating-yield element timestamp");
        }

        private static void MutatingEmptyInputFailsBeforeNoOp()
        {
            var project = CreateProject("P-FAMILY-FRESH-3", out var family, out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => ProjectFamilyService.Assign(project, family.Id, TouchThenStop(project)),
                "Project changed while Family assignment targets were being enumerated.");

            Equal(beforeVersion + 1L, project.ChangeVersion, "mutating-empty project revision");
            Equal(string.Empty, element.FamilyId, "mutating-empty FamilyId");
            False(element.Properties.ContainsKey("Material"), "mutating-empty inherited property");
            Equal(ElementDirtyFlags.None, element.Dirty, "mutating-empty dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "mutating-empty element timestamp");
        }

        private static ProjectState CreateProject(string id, out ProjectFamily family, out ProjectElement element)
        {
            var project = new ProjectState(id, "Family assignment freshness");
            family = new ProjectFamily("FAM-1", "Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "Steel";
            element = new ProjectElement("E-1", ElementCategory.Beam);
            project.Families.Add(family);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> LazyElement(ProjectElement element)
        {
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenYield(ProjectState project, ProjectElement element)
        {
            project.Touch();
            yield return element;
        }

        private static IEnumerable<ProjectElement> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("FamilyAssignInputFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("FamilyAssignInputFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("FamilyAssignInputFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("FamilyAssignInputFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
