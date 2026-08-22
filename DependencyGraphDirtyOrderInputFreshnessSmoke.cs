using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphDirtyOrderInputFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            IteratorMutationIsObservedAfterEnumeration();
            StableDirtySubsetKeepsTopologicalOrder();
        }

        private static void IteratorMutationIsObservedAfterEnumeration()
        {
            var first = CleanElement("A");
            var second = CleanElement("B");
            second.DependsOn.Add(first.Id);

            var order = new DependencyGraph().TopologicalDirtyOrder(MutateAfterFirstYield(first, second));

            Equal(2, order.Count, "mutating iterator count");
            Equal(first.Id, order[0].Id, "mutating iterator dependency first");
            Equal(second.Id, order[1].Id, "mutating iterator dependent second");
        }

        private static void StableDirtySubsetKeepsTopologicalOrder()
        {
            var first = CleanElement("ROOT");
            var second = CleanElement("CHILD");
            var clean = CleanElement("CLEAN");
            second.DependsOn.Add(first.Id);
            first.MarkDirty(ElementDirtyFlags.Quantity);
            second.MarkDirty(ElementDirtyFlags.Quantity);

            var order = new DependencyGraph().TopologicalDirtyOrder(new[] { second, clean, first });

            Equal(2, order.Count, "stable dirty count");
            Equal(first.Id, order[0].Id, "stable dependency first");
            Equal(second.Id, order[1].Id, "stable dependent second");
        }

        private static IEnumerable<ProjectElement> MutateAfterFirstYield(ProjectElement first, ProjectElement second)
        {
            yield return first;
            first.MarkDirty(ElementDirtyFlags.Quantity);
            second.MarkDirty(ElementDirtyFlags.Quantity);
            yield return second;
        }

        private static ProjectElement CleanElement(string id)
        {
            var element = new ProjectElement(id, ElementCategory.CustomQuantity);
            element.MarkClean(ElementDirtyFlags.All);
            return element;
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "DependencyGraphDirtyOrderInputFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
