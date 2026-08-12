using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphProcessedInputFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            MutationAfterProcessedElementFailsAndPreservesPreviousGraph();
            EquivalentDependencyReorderAndCasingRemainAccepted();
            StableInputStillBuildsExpectedGraph();
        }

        private static void MutationAfterProcessedElementFailsAndPreservesPreviousGraph()
        {
            var graph = new DependencyGraph();
            var previousRoot = new ProjectElement("OLD", ElementCategory.Beam);
            var previousDependent = new ProjectElement("OLD-DEP", ElementCategory.Beam);
            previousDependent.DependsOn.Add(previousRoot.Id);
            graph.Rebuild(new[] { previousRoot, previousDependent });

            var a = new ProjectElement("A", ElementCategory.Beam);
            var b = new ProjectElement("B", ElementCategory.Beam);
            b.DependsOn.Add(a.Id);

            ExpectInvalidOperation(
                () => graph.Rebuild(MutateProcessedDependency(a, b)),
                "input changed");

            EqualIds(new[] { "OLD-DEP" }, graph.GetDirectDependents("OLD"));
            if (!graph.TryGetElement("OLD", out var retained) || !ReferenceEquals(retained, previousRoot))
                throw new InvalidOperationException("Failed rebuild did not preserve the previous DependencyGraph element state.");
            if (graph.TryGetElement("A", out _))
                throw new InvalidOperationException("Failed rebuild leaked staged element A into the previous DependencyGraph state.");
        }

        private static void EquivalentDependencyReorderAndCasingRemainAccepted()
        {
            var graph = new DependencyGraph();
            var dependent = new ProjectElement("D", ElementCategory.Beam);
            dependent.DependsOn.Add("A");
            dependent.DependsOn.Add("B");
            var a = new ProjectElement("A", ElementCategory.Beam);
            var b = new ProjectElement("B", ElementCategory.Beam);

            graph.Rebuild(ReorderProcessedDependencies(dependent, a, b));

            EqualIds(new[] { "D" }, graph.GetDirectDependents("A"));
            EqualIds(new[] { "D" }, graph.GetDirectDependents("B"));
            if (!string.Equals(dependent.DependsOn[0], "b", StringComparison.Ordinal) ||
                !string.Equals(dependent.DependsOn[1], "a", StringComparison.Ordinal))
                throw new InvalidOperationException("Equivalent dependency mutation control did not execute.");
        }

        private static void StableInputStillBuildsExpectedGraph()
        {
            var graph = new DependencyGraph();
            var root = new ProjectElement("ROOT", ElementCategory.Beam);
            var middle = new ProjectElement("MID", ElementCategory.Beam);
            middle.DependsOn.Add(root.Id);
            var leaf = new ProjectElement("LEAF", ElementCategory.Beam);
            leaf.DependsOn.Add(middle.Id);

            graph.Rebuild(new[] { root, middle, leaf });

            EqualIds(new[] { "MID" }, graph.GetDirectDependents("ROOT"));
            EqualIds(new[] { "MID", "LEAF" }, graph.GetDependentsTransitive("ROOT"));
        }

        private static IEnumerable<ProjectElement> MutateProcessedDependency(ProjectElement first, ProjectElement second)
        {
            yield return first;
            first.DependsOn.Add(second.Id);
            yield return second;
        }

        private static IEnumerable<ProjectElement> ReorderProcessedDependencies(ProjectElement dependent, ProjectElement a, ProjectElement b)
        {
            yield return dependent;
            dependent.DependsOn.Clear();
            dependent.DependsOn.Add("b");
            dependent.DependsOn.Add("a");
            yield return a;
            yield return b;
        }

        private static void EqualIds(IEnumerable<string> expected, IEnumerable<string> actual)
        {
            var left = expected.ToArray();
            var right = actual.ToArray();
            if (left.Length != right.Length)
                throw new InvalidOperationException("DependencyGraph result count changed unexpectedly.");
            for (var index = 0; index < left.Length; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("DependencyGraph result identity changed unexpectedly at index " + index + ".");
            }
        }

        private static void ExpectInvalidOperation(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("DependencyGraph rejected processed-input mutation for an unexpected reason: " + ex.Message, ex);
            }

            throw new InvalidOperationException("Expected processed DependencyGraph input mutation to fail closed.");
        }
    }
}
