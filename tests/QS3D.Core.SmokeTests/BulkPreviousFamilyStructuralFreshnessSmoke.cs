using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkPreviousFamilyStructuralFreshnessSmoke
    {
        internal static void Run()
        {
            StableLazyAssignmentMigratesInheritedDefault();
            SameIdPreviousFamilyReplacementFailsClosed();
            PreviousFamilyRemovalThenEmptyFailsClosed();
        }

        private static void StableLazyAssignmentMigratesInheritedDefault()
        {
            var project = BuildProject(out var element, out _, out _);
            var changed = new BulkEditService().AssignFamily(project, StableTargetIds(), "F1");
            if (changed != 1 ||
                !string.Equals(element.FamilyId, "F1", StringComparison.Ordinal) ||
                !element.Properties.TryGetValue("Width", out var width) ||
                !string.Equals(width, "0.8", StringComparison.Ordinal))
                throw new InvalidOperationException("Stable lazy bulk Family assignment no longer migrates inherited defaults.");
        }

        private static void SameIdPreviousFamilyReplacementFailsClosed()
        {
            var project = BuildProject(out var element, out var previousFamily, out _);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                new BulkEditService().AssignFamily(project, ReplacePreviousFamilyThenYield(project, previousFamily), "F1"));
            if (project.ChangeVersion != version + 1L)
                throw new InvalidOperationException("Direct previous-Family replacement must advance ProjectState.ChangeVersion exactly once.");
            if (!string.Equals(element.FamilyId, "F0", StringComparison.Ordinal) ||
                !element.Properties.TryGetValue("Width", out var width) ||
                !string.Equals(width, "0.4", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk Family assignment mutated the element before rejecting previous-Family replacement.");
        }

        private static void PreviousFamilyRemovalThenEmptyFailsClosed()
        {
            var project = BuildProject(out var element, out var previousFamily, out _);
            var version = project.ChangeVersion;
            Throws<InvalidOperationException>(() =>
                new BulkEditService().AssignFamily(project, RemovePreviousFamilyThenEmpty(project, previousFamily), "F1"));
            if (project.ChangeVersion != version + 1L)
                throw new InvalidOperationException("Direct previous-Family removal must advance ProjectState.ChangeVersion exactly once.");
            if (!string.Equals(element.FamilyId, "F0", StringComparison.Ordinal))
                throw new InvalidOperationException("Bulk Family assignment changed the element before rejecting previous-Family removal.");
        }

        private static IEnumerable<string> StableTargetIds()
        {
            yield return "E1";
        }

        private static IEnumerable<string> ReplacePreviousFamilyThenYield(ProjectState project, ProjectFamily previousFamily)
        {
            var index = project.Families.IndexOf(previousFamily);
            if (index < 0) throw new InvalidOperationException("Expected previous Family in project.");
            var replacement = new ProjectFamily(previousFamily.Id, previousFamily.Name, previousFamily.Category);
            replacement.Properties["Width"] = "9.9";
            project.Families[index] = replacement;
            yield return "E1";
        }

        private static IEnumerable<string> RemovePreviousFamilyThenEmpty(ProjectState project, ProjectFamily previousFamily)
        {
            if (!project.Families.Remove(previousFamily))
                throw new InvalidOperationException("Expected previous Family removal to succeed.");
            yield break;
        }

        private static ProjectState BuildProject(
            out ProjectElement element,
            out ProjectFamily previousFamily,
            out ProjectFamily targetFamily)
        {
            var project = new ProjectState("P-BULK-PREV-FAMILY-FRESHNESS", "Bulk previous Family freshness");
            previousFamily = new ProjectFamily("F0", "Previous", ElementCategory.Beam);
            previousFamily.Properties["Width"] = "0.4";
            targetFamily = new ProjectFamily("F1", "Target", ElementCategory.Beam);
            targetFamily.Properties["Width"] = "0.8";
            project.Families.Add(previousFamily);
            project.Families.Add(targetFamily);

            element = new ProjectElement("E1", ElementCategory.Beam, "F0", string.Empty, string.Empty);
            element.Properties["Width"] = "0.4";
            project.Elements.Add(element);
            return project;
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
