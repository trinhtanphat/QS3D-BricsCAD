using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticUntrackPredicateContentFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            DependencyMutationCannotBypassDependentGuard();
            SourceHandleMutationFailsBeforeRemoval();
            PropertyMutationFailsBeforeNoOp();
            QuantityMutationFailsBeforeRemoval();
            ScalarRelationMutationFailsBeforeRemoval();
            DirtyMutationFailsBeforeRemoval();
            StablePredicateStillUsesNormalDependencyGuard();
            StablePredicateStillUntracksIndependentTarget();
        }

        private static void DependencyMutationCannotBypassDependentGuard()
        {
            var project = CreateDependentProject(out var target, out var dependent);
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    if (ReferenceEquals(candidate, target)) dependent.DependsOn.Clear();
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "dependency mutation revision");
            True(project.Elements.Contains(target), "dependency mutation retained target");
            True(project.Elements.Contains(dependent), "dependency mutation retained dependent");
            Equal(0, dependent.DependsOn.Count, "dependency mutation caller side effect remains visible");
        }

        private static void SourceHandleMutationFailsBeforeRemoval()
        {
            var project = CreateIndependentProject(out var target);
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    candidate.SourceHandles.Clear();
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "source-handle mutation revision");
            True(project.Elements.Contains(target), "source-handle mutation retained target");
            Equal(0, target.SourceHandles.Count, "source-handle mutation caller side effect remains visible");
        }

        private static void PropertyMutationFailsBeforeNoOp()
        {
            var project = CreateIndependentProject(out var target);
            target.Properties["Mode"] = "Before";
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    candidate.Properties["Mode"] = "After";
                    return false;
                }));

            Equal(beforeVersion, project.ChangeVersion, "property mutation revision");
            True(project.Elements.Contains(target), "property mutation retained target");
            Equal("After", target.Properties["Mode"], "property mutation caller side effect remains visible");
        }

        private static void QuantityMutationFailsBeforeRemoval()
        {
            var project = CreateIndependentProject(out var target);
            target.Quantities["Length"] = 1d;
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    candidate.Quantities["Length"] = 2d;
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "quantity mutation revision");
            True(project.Elements.Contains(target), "quantity mutation retained target");
            Equal(2d, target.Quantities["Length"], "quantity mutation caller side effect remains visible");
        }

        private static void ScalarRelationMutationFailsBeforeRemoval()
        {
            var project = CreateIndependentProject(out var target);
            target.ZoneId = "ZONE-A";
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    candidate.ZoneId = "ZONE-B";
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "scalar relation mutation revision");
            True(project.Elements.Contains(target), "scalar relation mutation retained target");
            Equal("ZONE-B", target.ZoneId, "scalar relation mutation caller side effect remains visible");
        }

        private static void DirtyMutationFailsBeforeRemoval()
        {
            var project = CreateIndependentProject(out var target);
            var beforeVersion = project.ChangeVersion;

            ThrowsContentFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    candidate.MarkClean(ElementDirtyFlags.All);
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "dirty mutation revision");
            True(project.Elements.Contains(target), "dirty mutation retained target");
            Equal(ElementDirtyFlags.None, target.Dirty, "dirty mutation caller side effect remains visible");
        }

        private static void StablePredicateStillUsesNormalDependencyGuard()
        {
            var project = CreateDependentProject(out var target, out var dependent);
            var beforeVersion = project.ChangeVersion;

            try
            {
                SemanticUntrackService.Untrack(
                    project,
                    new[] { "A" },
                    candidate => ReferenceEquals(candidate, target));
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.StartsWith("Cannot untrack semantic element(s) while dependents remain:", StringComparison.Ordinal))
                    throw new InvalidOperationException("Stable predicate produced an unexpected dependency guard error.", ex);
                Equal(beforeVersion, project.ChangeVersion, "stable dependent predicate revision");
                True(project.Elements.Contains(target), "stable dependent predicate retained target");
                True(project.Elements.Contains(dependent), "stable dependent predicate retained dependent");
                Equal(1, dependent.DependsOn.Count, "stable dependent predicate preserved dependency");
                return;
            }

            throw new InvalidOperationException("Stable semantic untrack predicate unexpectedly bypassed the dependency guard.");
        }

        private static void StablePredicateStillUntracksIndependentTarget()
        {
            var project = CreateIndependentProject(out var target);
            var beforeVersion = project.ChangeVersion;

            var result = SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate => ReferenceEquals(candidate, target));

            Equal(1, result.Count, "stable independent result count");
            Equal(target.Id, result.RemovedElementIds[0], "stable independent removed id");
            Equal(beforeVersion + 1L, project.ChangeVersion, "stable independent revision");
            False(project.Elements.Contains(target), "stable independent target ownership");
        }

        private static ProjectState CreateIndependentProject(out ProjectElement target)
        {
            var project = new ProjectState("UNTRACK-CONTENT-FRESH", "Semantic untrack predicate content freshness");
            target = new ProjectElement("E1", ElementCategory.CustomQuantity);
            target.SourceHandles.Add("A");
            project.Elements.Add(target);
            return project;
        }

        private static ProjectState CreateDependentProject(out ProjectElement target, out ProjectElement dependent)
        {
            var project = CreateIndependentProject(out target);
            dependent = new ProjectElement("E2", ElementCategory.CustomQuantity);
            dependent.SourceHandles.Add("B");
            dependent.DependsOn.Add(target.Id);
            project.Elements.Add(dependent);
            return project;
        }

        private static void ThrowsContentFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project element content changed while evaluating semantic untrack predicate. Retry against the current project state.";
                if (string.Equals(ex.Message, expected, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected semantic untrack predicate content freshness error.", ex);
            }
            throw new InvalidOperationException("Expected semantic untrack predicate content freshness rejection.");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("SemanticUntrackPredicateContentFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException("SemanticUntrackPredicateContentFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("SemanticUntrackPredicateContentFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
