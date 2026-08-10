using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphScaleSmoke
    {
        internal static void Run()
        {
            DeepDirtyChainOrdersWithoutProcessRecursion();
            DuplicateDirtyIdsFailClosed();
        }

        private static void DeepDirtyChainOrdersWithoutProcessRecursion()
        {
            const int depth = 8192;
            var elements = new List<ProjectElement>(depth);
            for (var index = 0; index < depth; index++)
            {
                var element = NewElement("E" + index);
                if (index + 1 < depth) element.DependsOn.Add("E" + (index + 1));
                elements.Add(element);
            }

            var order = new DependencyGraph().TopologicalDirtyOrder(elements);
            if (order.Count != depth) throw new Exception("Deep dependency ordering lost semantic elements.");
            if (!string.Equals(order[0].Id, "E" + (depth - 1), StringComparison.Ordinal) || !string.Equals(order[depth - 1].Id, "E0", StringComparison.Ordinal))
                throw new Exception("Deep dependency ordering did not preserve dependency-before-dependent semantics.");
        }

        private static void DuplicateDirtyIdsFailClosed()
        {
            var first = NewElement("DUP");
            var second = NewElement("dup");
            var threw = false;
            try { new DependencyGraph().TopologicalDirtyOrder(new[] { first, second }); }
            catch (InvalidOperationException ex) { threw = ex.Message.IndexOf("duplicate semantic element id", StringComparison.OrdinalIgnoreCase) >= 0; }
            if (!threw) throw new Exception("Dependency ordering must reject duplicate dirty semantic IDs.");
        }

        private static ProjectElement NewElement(string id) =>
            new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);
    }

    internal static class DependencyGraphScaleRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphScaleSmoke.Run();
    }
}
