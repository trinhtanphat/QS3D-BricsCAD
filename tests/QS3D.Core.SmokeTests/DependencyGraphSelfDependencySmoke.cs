using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphSelfDependencySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RebuildRejectsSelfDependency();
            DirtyOrderingRejectsSelfDependency();
            NormalDependencyStillRebuildsAndTraverses();
        }

        private static void RebuildRejectsSelfDependency()
        {
            var element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.DependsOn.Add("e1");

            ThrowsSelfDependency(() => new DependencyGraph().Rebuild(new[] { element }));
        }

        private static void DirtyOrderingRejectsSelfDependency()
        {
            var element = new ProjectElement("E2", ElementCategory.CustomQuantity);
            element.DependsOn.Add("E2");

            ThrowsSelfDependency(() => new DependencyGraph().TopologicalDirtyOrder(new[] { element }));
        }

        private static void NormalDependencyStillRebuildsAndTraverses()
        {
            var root = new ProjectElement("ROOT", ElementCategory.CustomQuantity);
            var child = new ProjectElement("CHILD", ElementCategory.CustomQuantity);
            child.DependsOn.Add(root.Id);

            var graph = new DependencyGraph();
            graph.Rebuild(new[] { root, child });

            var direct = graph.GetDirectDependents(root.Id);
            Equal(1, direct.Count, "direct dependent count");
            Equal(child.Id, direct[0], "direct dependent id");

            var transitive = graph.GetDependentsTransitive(root.Id);
            Equal(1, transitive.Count, "transitive dependent count");
            Equal(child.Id, transitive[0], "transitive dependent id");
        }

        private static void ThrowsSelfDependency(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("depends on itself", StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected DependencyGraph self-dependency error.", ex);
            }
            throw new InvalidOperationException("Expected DependencyGraph self-dependency rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "DependencyGraphSelfDependencySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
