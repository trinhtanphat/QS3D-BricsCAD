using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactSourceStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RemovedDependentDuringSourceEnumerationFailsClosed();
            ReplacedDependentDuringSourceEnumerationFailsClosed();
            StablePlanStillIncludesDependent();
        }

        private static void RemovedDependentDuringSourceEnumerationFailsClosed()
        {
            var project = CreateProject(out var root, out var child);
            var beforeVersion = project.ChangeVersion;

            ThrowsStructuralFreshness(() => new DependencyImpactPlanner().Plan(
                project,
                RemoveAndYield(project, child, root.Id)));

            Equal(beforeVersion, project.ChangeVersion, "remove side-effect version");
            Equal(1, project.Elements.Count, "remove side-effect element count");
            True(project.Elements.Contains(root), "remove side-effect root ownership");
            False(project.Elements.Contains(child), "remove side-effect child ownership");
        }

        private static void ReplacedDependentDuringSourceEnumerationFailsClosed()
        {
            var project = CreateProject(out var root, out var child);
            var beforeVersion = project.ChangeVersion;
            ProjectElement? replacement = null;

            ThrowsStructuralFreshness(() => new DependencyImpactPlanner().Plan(
                project,
                ReplaceAndYield(project, child, root.Id, value => replacement = value)));

            Equal(beforeVersion, project.ChangeVersion, "replacement side-effect version");
            Equal(2, project.Elements.Count, "replacement side-effect element count");
            False(project.Elements.Contains(child), "replacement original ownership");
            True(replacement != null && project.Elements.Contains(replacement), "replacement new ownership");
        }

        private static void StablePlanStillIncludesDependent()
        {
            var project = CreateProject(out var root, out var child);
            var beforeVersion = project.ChangeVersion;

            var plan = new DependencyImpactPlanner().Plan(project, new[] { root.Id });

            Equal(beforeVersion, plan.SourceChangeVersion, "stable source version");
            Equal(1, plan.RootElementIds.Count, "stable root count");
            Equal(root.Id, plan.RootElementIds[0], "stable root id");
            Equal(1, plan.Entries.Count, "stable impact count");
            Equal(child.Id, plan.Entries[0].ElementId, "stable impacted id");
            Equal(1, plan.Entries[0].Depth, "stable depth");
            Equal(root.Id, plan.Entries[0].CauseElementId, "stable cause id");
            Equal(root.Id, plan.Entries[0].RootElementId, "stable root provenance");
        }

        private static ProjectState CreateProject(out ProjectElement root, out ProjectElement child)
        {
            var project = new ProjectState("DEP-IMPACT-STRUCT", "Dependency impact structural freshness");
            root = new ProjectElement("ROOT", ElementCategory.CustomQuantity);
            child = new ProjectElement("CHILD", ElementCategory.CustomQuantity);
            child.DependsOn.Add(root.Id);
            project.Elements.Add(root);
            project.Elements.Add(child);
            return project;
        }

        private static IEnumerable<string> RemoveAndYield(ProjectState project, ProjectElement removed, string rootId)
        {
            project.Elements.Remove(removed);
            yield return rootId;
        }

        private static IEnumerable<string> ReplaceAndYield(
            ProjectState project,
            ProjectElement original,
            string rootId,
            Action<ProjectElement> captureReplacement)
        {
            project.Elements.Remove(original);
            var replacement = new ProjectElement(original.Id, original.Category);
            replacement.DependsOn.Add(rootId);
            project.Elements.Add(replacement);
            captureReplacement(replacement);
            yield return rootId;
        }

        private static void ThrowsStructuralFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project element ownership changed while dependency impact was being planned; recompute the impact plan.";
                if (string.Equals(ex.Message, expected, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected dependency impact structural freshness error.", ex);
            }
            throw new InvalidOperationException("Expected dependency impact structural freshness rejection.");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("DependencyImpactSourceStructuralFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException("DependencyImpactSourceStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "DependencyImpactSourceStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
