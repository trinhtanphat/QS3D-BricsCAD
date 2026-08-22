using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphReferentialIntegritySmoke
    {
        internal static void Run()
        {
            RebuildRejectsMissingDependencyAndPreservesPriorGraph();
            ForwardReferenceWithinFullGraphIsValid();
            TopologicalSubsetStillAllowsDependencyOutsideSubset();
        }

        private static void RebuildRejectsMissingDependencyAndPreservesPriorGraph()
        {
            var graph = new DependencyGraph();
            var source = Element("A");
            var dependent = Element("B", "A");
            graph.Rebuild(new[] { source, dependent });

            var invalid = Element("C", "MISSING");
            Throws<InvalidOperationException>(() => graph.Rebuild(new[] { invalid }));

            True(graph.TryGetElement("A", out var retainedSource) && ReferenceEquals(source, retainedSource),
                "failed rebuild replaced the previous element index");
            True(graph.TryGetElement("B", out var retainedDependent) && ReferenceEquals(dependent, retainedDependent),
                "failed rebuild lost the previous dependent element");
            False(graph.TryGetElement("C", out _), "failed rebuild committed the invalid element index");
            SequenceEqual(new[] { "B" }, graph.GetDirectDependents("A"), "failed rebuild replaced the previous dependent edge");
            Equal(0, graph.GetDirectDependents("MISSING").Count, "failed rebuild committed a synthetic missing source node");
        }

        private static void ForwardReferenceWithinFullGraphIsValid()
        {
            var graph = new DependencyGraph();
            var dependent = Element("B", "A");
            var source = Element("A");

            graph.Rebuild(new[] { dependent, source });

            True(graph.TryGetElement("A", out var resolvedSource) && ReferenceEquals(source, resolvedSource),
                "forward-referenced source was not retained");
            SequenceEqual(new[] { "B" }, graph.GetDirectDependents("A"), "forward dependency edge was not built");
        }

        private static void TopologicalSubsetStillAllowsDependencyOutsideSubset()
        {
            var graph = new DependencyGraph();
            var dependent = Element("B", "A");

            var ordered = graph.TopologicalDirtyOrder(new[] { dependent });

            Equal(1, ordered.Count, "subset topological ordering dropped the supplied candidate");
            True(ReferenceEquals(dependent, ordered[0]), "subset topological ordering changed candidate identity");
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.Beam);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("DependencyGraphReferentialIntegritySmoke: " + message + ".");
        }

        private static void False(bool condition, string message) => True(!condition, message);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception("DependencyGraphReferentialIntegritySmoke: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void SequenceEqual(string[] expected, System.Collections.Generic.IReadOnlyList<string> actual, string message)
        {
            if (!expected.SequenceEqual(actual, StringComparer.OrdinalIgnoreCase))
                throw new Exception("DependencyGraphReferentialIntegritySmoke: " + message + ".");
        }
    }

    internal static class DependencyGraphReferentialIntegritySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphReferentialIntegritySmoke.Run();
    }
}
