using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticUntrackPredicateStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RemovedTargetFalsePredicateFailsBeforeNoOp();
            ReplacedTargetTruePredicateFailsBeforePlanning();
            RemovedUnrelatedElementFailsBeforePlanning();
        }

        private static void RemovedTargetFalsePredicateFailsBeforeNoOp()
        {
            var project = CreateProject(out var target, out _);
            var beforeVersion = project.ChangeVersion;

            ThrowsStructuralFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    project.Elements.Remove(candidate);
                    return false;
                }));

            Equal(beforeVersion, project.ChangeVersion, "removed-target false-predicate revision");
            Equal(1, project.Elements.Count, "removed-target false-predicate caller-side effect count");
            False(project.Elements.Contains(target), "removed-target false-predicate original target ownership");
        }

        private static void ReplacedTargetTruePredicateFailsBeforePlanning()
        {
            var project = CreateProject(out var target, out _);
            var beforeVersion = project.ChangeVersion;
            ProjectElement? replacement = null;

            ThrowsStructuralFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    project.Elements.Remove(candidate);
                    replacement = new ProjectElement(candidate.Id, candidate.Category);
                    replacement.SourceHandles.Add("A");
                    project.Elements.Add(replacement);
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "replaced-target true-predicate revision");
            Equal(2, project.Elements.Count, "replaced-target true-predicate caller-side effect count");
            False(project.Elements.Contains(target), "replaced-target true-predicate original target ownership");
            True(replacement != null && project.Elements.Contains(replacement), "replaced-target true-predicate replacement ownership");
        }

        private static void RemovedUnrelatedElementFailsBeforePlanning()
        {
            var project = CreateProject(out var target, out var unrelated);
            var beforeVersion = project.ChangeVersion;

            ThrowsStructuralFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate =>
                {
                    if (ReferenceEquals(candidate, target)) project.Elements.Remove(unrelated);
                    return true;
                }));

            Equal(beforeVersion, project.ChangeVersion, "removed-unrelated revision");
            True(project.Elements.Contains(target), "removed-unrelated target ownership");
            False(project.Elements.Contains(unrelated), "removed-unrelated caller-side effect");
        }

        private static ProjectState CreateProject(out ProjectElement target, out ProjectElement unrelated)
        {
            var project = new ProjectState("UNTRACK-STRUCT-FRESH", "Semantic untrack structural freshness");
            target = new ProjectElement("E1", ElementCategory.CustomQuantity);
            target.SourceHandles.Add("A");
            unrelated = new ProjectElement("E2", ElementCategory.CustomQuantity);
            unrelated.SourceHandles.Add("B");
            project.Elements.Add(target);
            project.Elements.Add(unrelated);
            return project;
        }

        private static void ThrowsStructuralFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project element ownership changed while evaluating semantic untrack predicate. Retry against the current project state.";
                if (string.Equals(ex.Message, expected, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected semantic untrack structural freshness error.", ex);
            }
            throw new InvalidOperationException("Expected semantic untrack structural freshness rejection.");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException("SemanticUntrackPredicateStructuralFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException("SemanticUntrackPredicateStructuralFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("SemanticUntrackPredicateStructuralFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
