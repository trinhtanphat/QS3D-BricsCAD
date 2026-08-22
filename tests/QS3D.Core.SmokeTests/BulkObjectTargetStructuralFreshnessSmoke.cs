using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkObjectTargetStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SetPropertyRemovedTargetFailsClosed();
            MultiplyReplacedTargetFailsClosed();
            StableObjectTargetsStillMutate();
        }

        private static void SetPropertyRemovedTargetFailsClosed()
        {
            var project = CreateProject("P-BULK-OBJECT-STRUCT-1", out var element);
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => new BulkEditService().SetProperty(project, YieldThenRemove(project, element), "Note", "changed"),
                "Bulk edit object target enumeration target no longer belongs to the project after enumeration");

            Equal(beforeVersion, project.ChangeVersion, "removed-target project revision");
            False(project.Elements.Contains(element), "removed-target caller side effect");
            False(element.Properties.ContainsKey("Note"), "removed-target stale property mutation");
            Equal(ElementDirtyFlags.None, element.Dirty, "removed-target dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "removed-target timestamp");
        }

        private static void MultiplyReplacedTargetFailsClosed()
        {
            var project = CreateProject("P-BULK-OBJECT-STRUCT-2", out var element);
            element.Properties["Factor"] = "2";
            element.MarkClean(ElementDirtyFlags.All);
            var replacement = new ProjectElement(element.Id, element.Category);
            replacement.Properties["Factor"] = "7";
            replacement.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;
            var replacementUpdated = replacement.UpdatedUtc;

            ThrowsContaining<InvalidOperationException>(
                () => new BulkEditService().MultiplyNumericProperty(project, YieldThenReplace(project, element, replacement), "Factor", 3d),
                "Bulk numeric object target enumeration target no longer belongs to the project after enumeration");

            Equal(beforeVersion, project.ChangeVersion, "replaced-target project revision");
            False(project.Elements.Contains(element), "replaced-target original instance");
            True(project.Elements.Contains(replacement), "replaced-target replacement instance");
            Equal("2", element.Properties["Factor"], "replaced-target stale value");
            Equal("7", replacement.Properties["Factor"], "replaced-target replacement value");
            Equal(ElementDirtyFlags.None, element.Dirty, "replaced-target original dirty flags");
            Equal(ElementDirtyFlags.None, replacement.Dirty, "replaced-target replacement dirty flags");
            Equal(beforeUpdated, element.UpdatedUtc, "replaced-target original timestamp");
            Equal(replacementUpdated, replacement.UpdatedUtc, "replaced-target replacement timestamp");
        }

        private static void StableObjectTargetsStillMutate()
        {
            var project = CreateProject("P-BULK-OBJECT-STRUCT-3", out var element);
            element.Properties["Factor"] = "2";
            element.MarkClean(ElementDirtyFlags.All);
            var service = new BulkEditService();
            var beforeVersion = project.ChangeVersion;

            var changed = service.SetProperty(project, YieldStable(element), "Note", "ok");
            Equal(1, changed.Count, "stable SetProperty count");
            Equal("ok", element.Properties["Note"], "stable SetProperty value");
            Equal(beforeVersion + 1L, project.ChangeVersion, "stable SetProperty project revision");

            var afterSetVersion = project.ChangeVersion;
            changed = service.MultiplyNumericProperty(project, YieldStable(element), "Factor", 3d);
            Equal(1, changed.Count, "stable Multiply count");
            Equal("6", element.Properties["Factor"], "stable Multiply value");
            Equal(afterSetVersion + 1L, project.ChangeVersion, "stable Multiply project revision");
        }

        private static ProjectState CreateProject(string id, out ProjectElement element)
        {
            var project = new ProjectState(id, "Bulk object structural freshness");
            element = new ProjectElement("E-BULK-OBJECT", ElementCategory.Beam);
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<ProjectElement> YieldThenRemove(ProjectState project, ProjectElement element)
        {
            yield return element;
            project.Elements.Remove(element);
        }

        private static IEnumerable<ProjectElement> YieldThenReplace(ProjectState project, ProjectElement original, ProjectElement replacement)
        {
            yield return original;
            project.Elements.Remove(original);
            project.Elements.Add(replacement);
        }

        private static IEnumerable<ProjectElement> YieldStable(ProjectElement element)
        {
            yield return element;
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("BulkObjectTargetStructuralFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkObjectTargetStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkObjectTargetStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void ThrowsContaining<TException>(Action action, string expectedText) where TException : Exception
        {
            try { action(); }
            catch (TException ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("BulkObjectTargetStructuralFreshnessSmoke expected message containing '" + expectedText + "', actual='" + ex.Message + "'.");
            }
            throw new Exception("BulkObjectTargetStructuralFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
