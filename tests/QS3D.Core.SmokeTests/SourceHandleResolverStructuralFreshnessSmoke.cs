using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverStructuralFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            StableLazyRootResolvesOriginalHandle();
            RemovedRootFailsClosed();
            ReboundRootFailsClosed();
            MutatingEmptyRootFailsClosed();
        }

        private static void StableLazyRootResolvesOriginalHandle()
        {
            var project = Project(out var original);
            var handles = SourceHandleResolver.Resolve(project, LazyRoot(original.Id));

            Require(handles.Count == 1 && handles[0] == "A",
                "Stable lazy Locate root did not resolve the original source handle.");
        }

        private static void RemovedRootFailsClosed()
        {
            var project = Project(out var original);
            ThrowsStructuralFreshness(() => SourceHandleResolver.Resolve(project, RemoveThenYield(project, original, original.Id)));
            Require(project.Elements.Count == 0,
                "Locate structural freshness unexpectedly rolled back caller-side element removal.");
        }

        private static void ReboundRootFailsClosed()
        {
            var project = Project(out var original);
            var replacement = new ProjectElement(original.Id, ElementCategory.CustomQuantity);
            replacement.SourceHandles.Add("B");

            ThrowsStructuralFreshness(() => SourceHandleResolver.Resolve(project, ReplaceThenYield(project, replacement, original.Id)));
            Require(project.Elements.Count == 1 && ReferenceEquals(project.Elements[0], replacement),
                "Locate structural freshness unexpectedly rolled back caller-side same-id replacement.");
            Require(replacement.SourceHandles.Count == 1 && replacement.SourceHandles[0] == "B",
                "Locate structural freshness mutated the rebound element.");
        }

        private static void MutatingEmptyRootFailsClosed()
        {
            var project = Project(out var original);
            ThrowsStructuralFreshness(() => SourceHandleResolver.Resolve(project, RemoveThenStop(project, original)));
            Require(project.Elements.Count == 0,
                "Mutating empty Locate root enumeration escaped structural freshness without preserving caller mutation.");
        }

        private static ProjectState Project(out ProjectElement element)
        {
            var project = new ProjectState("LOCATE-STRUCTURAL-FRESH", "Locate structural freshness");
            element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> LazyRoot(string elementId)
        {
            yield return elementId;
        }

        private static IEnumerable<string> RemoveThenYield(ProjectState project, ProjectElement element, string elementId)
        {
            project.Elements.Remove(element);
            yield return elementId;
        }

        private static IEnumerable<string> ReplaceThenYield(ProjectState project, ProjectElement replacement, string elementId)
        {
            project.Elements[0] = replacement;
            yield return elementId;
        }

        private static IEnumerable<string> RemoveThenStop(ProjectState project, ProjectElement element)
        {
            project.Elements.Remove(element);
            yield break;
        }

        private static void ThrowsStructuralFreshness(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                const string expected = "Project element ownership changed while materializing Locate root element ids. Retry Locate against the current project state.";
                if (!string.Equals(ex.Message, expected, StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected Locate structural freshness error.", ex);
                return;
            }
            throw new InvalidOperationException("Expected Locate structural freshness rejection.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
