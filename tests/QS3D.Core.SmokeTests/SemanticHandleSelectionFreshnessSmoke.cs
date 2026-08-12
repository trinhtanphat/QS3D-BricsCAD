using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleSelectionFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableLazySelectionResolvesOwner();
            MutatingLazySelectionFailsBeforeOwnershipScan();
            MutatingEmptySelectionFailsBeforeNoOp();
        }

        private static void StableLazySelectionResolvesOwner()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;

            var resolved = SemanticHandleOwnershipResolver.Resolve(project, LazyHandle(" A "));

            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Stable lazy semantic handle selection did not resolve the owned element.");
            Require(project.ChangeVersion == beforeVersion,
                "Stable semantic handle resolution unexpectedly changed ProjectState.ChangeVersion.");
        }

        private static void MutatingLazySelectionFailsBeforeOwnershipScan()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;
            var beforeHandles = element.SourceHandles.Count;

            ThrowsFreshness(() => SemanticHandleOwnershipResolver.Resolve(project, TouchThenYield(project, "A")));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating lazy handle input did not preserve the caller's project revision change.");
            Require(element.SourceHandles.Count == beforeHandles && element.SourceHandles[0] == "A",
                "Freshness rejection mutated semantic source ownership.");
        }

        private static void MutatingEmptySelectionFailsBeforeNoOp()
        {
            var project = CreateProject(out var element);
            var beforeVersion = project.ChangeVersion;

            ThrowsFreshness(() => SemanticHandleOwnershipResolver.Resolve(project, TouchThenStop(project)));

            Require(project.ChangeVersion == beforeVersion + 1L,
                "Mutating empty handle input did not preserve the caller's project revision change.");
            Require(element.SourceHandles.Count == 1 && element.SourceHandles[0] == "A",
                "Mutating empty selection changed semantic source ownership.");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("HANDLE-FRESH", "Semantic handle freshness");
            element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> LazyHandle(string handle)
        {
            yield return handle;
        }

        private static IEnumerable<string> TouchThenYield(ProjectState project, string handle)
        {
            project.Touch();
            yield return handle;
        }

        private static IEnumerable<string> TouchThenStop(ProjectState project)
        {
            project.Touch();
            yield break;
        }

        private static void ThrowsFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project state changed while materializing semantic handle selection. Retry against the current project state.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected semantic handle freshness error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected semantic handle selection freshness rejection.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
