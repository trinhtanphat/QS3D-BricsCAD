using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphRebuildInputFreshnessSmoke
    {
        public static void Run()
        {
            StableLazyRebuildPreservesGraphSemantics();
            ReentrantRebuildFailsWithoutOverwritingNewerGraph();
            ReentrantRebuildWithEmptyOuterInputFailsBeforeClear();
        }

        private static void StableLazyRebuildPreservesGraphSemantics()
        {
            var graph = new DependencyGraph();
            var source = Element("A");
            var dependent = Element("B", "A");

            graph.Rebuild(LazyElements(source, dependent));

            SequenceEqual(new[] { "B" }, graph.GetDirectDependents("A"));
            SequenceEqual(new[] { "B" }, graph.GetDependentsTransitive("A"));
            True(graph.TryGetElement("B", out var resolved));
            True(ReferenceEquals(dependent, resolved));
        }

        private static void ReentrantRebuildFailsWithoutOverwritingNewerGraph()
        {
            var graph = new DependencyGraph();
            var innerSource = Element("A");
            var innerDependent = Element("C", "A");
            var outerSource = Element("A");
            var outerDependent = Element("B", "A");

            ThrowsContaining<InvalidOperationException>(
                () => graph.Rebuild(RebuildThenYield(graph, new[] { innerSource, innerDependent }, outerSource, outerDependent)),
                "Dependency graph changed while rebuild elements were being enumerated");

            SequenceEqual(new[] { "C" }, graph.GetDirectDependents("A"));
            True(graph.TryGetElement("C", out var resolved));
            True(ReferenceEquals(innerDependent, resolved));
            False(graph.TryGetElement("B", out _));
        }

        private static void ReentrantRebuildWithEmptyOuterInputFailsBeforeClear()
        {
            var graph = new DependencyGraph();
            var innerSource = Element("A");
            var innerDependent = Element("C", "A");

            ThrowsContaining<InvalidOperationException>(
                () => graph.Rebuild(RebuildThenStop(graph, new[] { innerSource, innerDependent })),
                "Dependency graph changed while rebuild elements were being enumerated");

            SequenceEqual(new[] { "C" }, graph.GetDependentsTransitive("A"));
            True(graph.TryGetElement("C", out _));
        }

        private static ProjectElement Element(string id, params string[] dependsOn)
        {
            var element = new ProjectElement(id, ElementCategory.Room);
            foreach (var dependencyId in dependsOn)
                element.DependsOn.Add(dependencyId);
            return element;
        }

        private static IEnumerable<ProjectElement> LazyElements(params ProjectElement[] elements)
        {
            foreach (var element in elements)
                yield return element;
        }

        private static IEnumerable<ProjectElement> RebuildThenYield(
            DependencyGraph graph,
            IEnumerable<ProjectElement> inner,
            params ProjectElement[] outer)
        {
            graph.Rebuild(inner);
            foreach (var element in outer)
                yield return element;
        }

        private static IEnumerable<ProjectElement> RebuildThenStop(
            DependencyGraph graph,
            IEnumerable<ProjectElement> inner)
        {
            graph.Rebuild(inner);
            yield break;
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected false.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                throw new Exception("Expected [" + string.Join(", ", expected) + "] but got [" + string.Join(", ", actual) + "].");
        }

        private static void ThrowsContaining<T>(Action action, string expectedText) where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                if (ex.Message.IndexOf(expectedText, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected exception message containing '" + expectedText + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
