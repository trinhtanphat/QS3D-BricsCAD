using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphDirectDependentsSmoke
    {
        public static void Run()
        {
            DirectLookupIsDeterministicAndNonTransitive();
            LookupNormalizesSourceId();
            MissingSourceIsEmpty();
        }

        private static void DirectLookupIsDeterministicAndNonTransitive()
        {
            var root = Element("ROOT");
            var z = Element("Z-CHILD", "ROOT");
            var a = Element("A-CHILD", "ROOT", "ROOT");
            var grandchild = Element("GRANDCHILD", "A-CHILD");
            var graph = new DependencyGraph();
            graph.Rebuild(new[] { grandchild, z, root, a });

            var direct = graph.GetDirectDependents("ROOT").ToArray();
            if (direct.Length != 2 || direct[0] != "A-CHILD" || direct[1] != "Z-CHILD")
                throw new Exception("Direct dependents must be unique and deterministically ordered.");
            if (direct.Contains("GRANDCHILD"))
                throw new Exception("Direct dependents lookup must not include transitive descendants.");
        }

        private static void LookupNormalizesSourceId()
        {
            var root = Element("Root");
            var child = Element("Child", " root ");
            var graph = new DependencyGraph();
            graph.Rebuild(new[] { root, child });
            var direct = graph.GetDirectDependents(" ROOT ");
            if (direct.Count != 1 || !string.Equals(direct[0], "Child", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Direct dependency lookup must normalize source IDs case-insensitively.");
        }

        private static void MissingSourceIsEmpty()
        {
            var graph = new DependencyGraph();
            graph.Rebuild(new[] { Element("ROOT") });
            if (graph.GetDirectDependents("missing").Count != 0 || graph.GetDirectDependents(" ").Count != 0)
                throw new Exception("Missing/blank direct dependency lookups must be empty.");
        }

        private static ProjectElement Element(string id, params string[] dependsOn)
        {
            var element = new ProjectElement(id, ElementCategory.Wall, null, null, null);
            foreach (var dependency in dependsOn) element.DependsOn.Add(dependency);
            return element;
        }
    }
}
