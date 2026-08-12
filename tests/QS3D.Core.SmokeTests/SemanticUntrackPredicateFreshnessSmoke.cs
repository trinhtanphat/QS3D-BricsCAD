using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticUntrackPredicateFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StablePredicateUntracksOwnedElement();
            MutatingTruePredicateFailsBeforeRemoval();
            MutatingFalsePredicateFailsBeforeNoOp();
        }

        private static void StablePredicateUntracksOwnedElement()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;

            var result = SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                candidate => ReferenceEquals(candidate, element));

            Require(result.Count == 1 && result.RemovedElementIds[0] == element.Id,
                "Stable semantic untrack predicate did not remove the owned element.");
            Require(project.Elements.Count == 0,
                "Stable semantic untrack predicate left the target in the project.");
            Require(project.ChangeVersion == beforeVersion + 1L,
                "Stable semantic untrack did not advance the project revision exactly once.");
        }

        private static void MutatingTruePredicateFailsBeforeRemoval()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                _ =>
                {
                    project.Touch();
                    return true;
                }));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating semantic untrack predicate side effect was unexpectedly rolled back.");
            Require(project.Elements.Count == 1 && ReferenceEquals(project.Elements[0], element),
                "Freshness rejection removed the semantic target.");
            Require(element.SourceHandles.Count == 1 && element.SourceHandles[0] == "A",
                "Freshness rejection changed semantic source ownership.");
        }

        private static void MutatingFalsePredicateFailsBeforeNoOp()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => SemanticUntrackService.Untrack(
                project,
                new[] { "A" },
                _ =>
                {
                    project.Touch();
                    return false;
                }));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating false predicate side effect was unexpectedly rolled back.");
            Require(project.Elements.Count == 1 && ReferenceEquals(project.Elements[0], element),
                "Mutating false predicate escaped freshness rejection through the no-op path.");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("UNTRACK-FRESH", "Semantic untrack predicate freshness");
            element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
        }

        private static void ThrowsFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project state changed while evaluating semantic untrack predicate. Retry against the current project state.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected semantic untrack predicate freshness error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected semantic untrack predicate freshness rejection.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
